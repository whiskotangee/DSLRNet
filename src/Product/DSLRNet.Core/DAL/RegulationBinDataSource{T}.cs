namespace DSLRNet.Core.Data;

using DSLRNet.Core.DAL;
using System.Collections.Concurrent;

public class RegulationBinDataSource<T>(
    DataSourceConfig paramSource,
    RandomProvider random,
    RegulationBinReader regulationBinReader) : BaseDataSource<T>(random)
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
        this.namesMapping = names.ToDictionary(s => Convert.ToInt32(s.Substring(0, s.IndexOf(" "))), d => d.Substring(d.IndexOf(" ")).ToString());

        ConcurrentBag<T> loadedValues = [];
        await Parallel.ForEachAsync(
            this.readParam.Rows,
            new ParallelOptions() { MaxDegreeOfParallelism = 1000 },
            async (row, c) =>
            {
                loadedValues.Add(await this.CreateFromPARAMAsync(row));
            });

        return [.. ApplyFilters(loadedValues)];
    }

    private async Task<T> CreateFromPARAMAsync(PARAM.Row row)
    {
        if (false && !pocoCreated)
        {
            await semaphore.WaitAsync();

            if (!pocoCreated)
            { 
                PocoGenerator.GenerateClass(typeof(T).Name, row);
                pocoCreated = true;
            }
        }

        T newObject = new();

        newObject.GenericParam.ID = row.ID;
        newObject.GenericParam.Name = this.namesMapping.TryGetValue(row.ID, out string? value) ? value : row.Name;

        foreach (var cell in row.Cells)
        {
            newObject.GenericParam.SetValue(cell.Def.InternalName, cell.Value);
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
                switch (filter.Operator)
                {
                    case FilterOperator.GreaterThan:
                        filteredData = filteredData.Where(d => d.GenericParam.GetValue<int>(filter.Field) > Convert.ToDouble(filter.Value));
                        break;
                    case FilterOperator.LessThan:
                        filteredData = filteredData.Where(d => d.GenericParam.GetValue<int>(filter.Field) < Convert.ToInt32(filter.Value));
                        break;
                    case FilterOperator.StartsWith:
                        filteredData = filteredData.Where(d => d.GenericParam.GetValue<string>(filter.Field).ToString().StartsWith(filter.Value.ToString()));
                        break;
                    case FilterOperator.EndsWith:
                        filteredData = filteredData.Where(d => d.GenericParam.GetValue<string>(filter.Field).ToString().EndsWith(filter.Value.ToString()));
                        break;
                    case FilterOperator.NotEqual:
                        filteredData = filteredData.Where(d => !d.GenericParam.GetValue<string>(filter.Field).Equals(filter.Value.ToString(), StringComparison.OrdinalIgnoreCase));
                        break;
                    case FilterOperator.NotInRange:
                        var range = filter.Value.ToString().Split("..");
                        filteredData = filteredData.Where(d => !Enumerable.Range(Convert.ToInt32(range[0]), Convert.ToInt32(range[1]) - Convert.ToInt32(range[0])).ToList().Contains(d.GenericParam.GetValue<int>(filter.Field)));
                        break;
                }
            }

            Console.WriteLine($"Applied filters removed {countBefore - filteredData.Count()} rows");

            return filteredData.ToList();
        }

        return data;
    }
}
