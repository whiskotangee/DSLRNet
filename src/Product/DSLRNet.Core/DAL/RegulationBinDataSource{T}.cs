namespace DSLRNet.Core.Data;

using DSLRNet.Core.DAL;
using System.Collections.Concurrent;

public class RegulationBinDataSource<T>(
    DataSourceConfig paramSource,
    RandomProvider random,
    RegulationBinBank regulationBinReader) : BaseDataSource<T>(random)
    where T : ParamBase<T>, ICloneable<T>, new()
{
    private PARAM? readParam = null;
    private bool pocoCreated = false;
    private static SemaphoreSlim semaphore = new(1);
    private Dictionary<int, string> namesMapping = [];

    public async override Task<IEnumerable<T>> LoadDataAsync()
    {
        if (readParam == null)
        {
            this.readParam = regulationBinReader.GetParam(paramSource.Name);
        }

        var names = File.ReadAllLines($"Assets\\Data\\PARAM\\ER\\Names\\{paramSource.Name}.txt");
        this.namesMapping = names.ToDictionary(s => Convert.ToInt32(s.Substring(0, s.IndexOf(" "))), d => d.Substring(d.IndexOf(" ")).Trim().ToString());

        ConcurrentBag<T> loadedValues = [];
        await Parallel.ForEachAsync(
            this.readParam.Rows,
            new ParallelOptions() { MaxDegreeOfParallelism = 1000 },
            async (row, c) =>
            {
                loadedValues.Add(await this.CreateFromPARAMAsync(row));
            });

        return [.. ApplyFilters(loadedValues).OrderBy(d => d.ID)];
    }

    private async Task<T> CreateFromPARAMAsync(PARAM.Row row)
    {
        if (!pocoCreated)
        {
            await semaphore.WaitAsync();

            if (!pocoCreated)
            { 
                PocoGenerator.GenerateClass(typeof(T).Name, row);
                pocoCreated = true;
            }

            semaphore.Release();
        }

        T newObject = new()
        {
            ID = row.ID
        };

        newObject.GenericParam.Name = this.namesMapping.TryGetValue(row.ID, out string? value) ? value : row.Name;

        foreach (var cell in row.Cells)
        {
            newObject.SetValue(cell.Def.InternalName, cell.Value);
        }

        return newObject;
    }

    protected IEnumerable<T> ApplyFilters(IEnumerable<T> data)
    {
        if (paramSource.Filters != null)
        {
            var countBefore = data.Count();

            IEnumerable<T> filteredData = data;
            foreach (var filter in paramSource.Filters)
            {
                var valueString = filter.Value.ToString();

                if (valueString == null)
                {
                    throw new ArgumentNullException(nameof(filter.Value), $"Filter {filter.Operator} on param {paramSource.Name}");
                }

                switch (filter.Operator)
                {
                    case FilterOperator.GreaterThan:
                        filteredData = filteredData.Where(d => d.GetValue<int>(filter.Field) > Convert.ToDouble(filter.Value));
                        break;
                    case FilterOperator.LessThan:
                        filteredData = filteredData.Where(d => d.GetValue<int>(filter.Field) < Convert.ToInt32(filter.Value));
                        break;
                    case FilterOperator.StartsWith:
                        filteredData = filteredData.Where(d => d.GetValue<string>(filter.Field).ToString().StartsWith(valueString));
                        break;
                    case FilterOperator.EndsWith:
                        filteredData = filteredData.Where(d => d.GetValue<string>(filter.Field).ToString().EndsWith(valueString));
                        break;
                    case FilterOperator.NotEqual:
                        filteredData = filteredData.Where(d => !d.GetValue<string>(filter.Field).Equals(valueString, StringComparison.OrdinalIgnoreCase));
                        break;
                    case FilterOperator.NotInRange:
                        var range = valueString.Split("..");
                        filteredData = filteredData.Where(d => !Enumerable.Range(Convert.ToInt32(range[0]), Convert.ToInt32(range[1]) - Convert.ToInt32(range[0])).ToList().Contains(d.GetValue<int>(filter.Field)));
                        break;
                }
            }

            return filteredData.ToList();
        }

        return data;
    }
}
