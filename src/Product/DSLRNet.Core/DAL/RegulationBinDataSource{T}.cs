namespace DSLRNet.Core.DAL;

using Org.BouncyCastle.Utilities.IO.Pem;
using System.Collections.Concurrent;

public class RegulationBinDataSource<T>(
    DataSourceConfig paramSource,
    RandomProvider random,
    RegulationBinBank regulationBinReader,
    LocalizedNameSource nameSource,
    FileSourceHandler fileSourceHandler) : BaseDataSource<T>(random)
    where T : ParamBase<T>, ICloneable<T>, new()
{
    private PARAM? readParam = null;
    private bool pocoCreated = false;
    private static readonly SemaphoreSlim semaphore = new(1);
    private Dictionary<int, string> namesMapping = [];

    public async override Task<IEnumerable<T>> LoadDataAsync()
    {
        if (readParam == null)
        {
            this.readParam = regulationBinReader.GetParam(paramSource.Name);
        }

        string[] names = File.ReadAllLines(PathHelper.FullyQualifyAppDomainPath($"Assets","Data","PARAM","ER","Names",$"{paramSource.Name}.txt"));
        this.namesMapping = names.ToDictionary(s => Convert.ToInt32(s[..s.IndexOf(' ')]), d => d[d.IndexOf(' ')..].Trim().ToString());

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

        // msg files based name
        // name baked into regulation bin
        // definition name
        if (typeof(T) == typeof(NpcParam) && nameSource.TryGetNameFromMessageFiles(paramSource.Name, (int)row.Cells.First(c => c.Def.InternalName == "nameId").Value, out string? name))
        {
            newObject.GenericParam.Name = name;
        }
        else if (typeof(T) == typeof(EquipParamCustomWeapon) && nameSource.TryGetNameFromMessageFiles(DataSourceNames.EquipParamWeapon, (int)row.Cells.First(d => d.Def.InternalName == "baseWepId").Value, out name))
        {
            newObject.GenericParam.Name = name;
        }   
        else if (nameSource.TryGetNameFromMessageFiles(paramSource.Name, row.ID, out name))
        {
            newObject.GenericParam.Name = name;
        }
        else if (!string.IsNullOrEmpty(row.Name))
        {
            newObject.GenericParam.Name = row.Name;
        }
        else if (this.namesMapping.TryGetValue(row.ID, out string? value))
        {
            newObject.GenericParam.Name = value;
        }
        else
        {
            newObject.GenericParam.Name = string.Empty;
        }

        foreach (PARAM.Cell? cell in row.Cells)
        {
            newObject.SetValue(cell.Def.InternalName, cell.Value);
        }

        return newObject;
    }

    protected IEnumerable<T> ApplyFilters(IEnumerable<T> data)
    {
        var filteredData = data;

        if (paramSource.Filters != null)
        {
            int countBefore = data.Count();

            foreach (Filter filter in paramSource.Filters)
            {
                string valueString = filter.Value.ToString() ?? throw new ArgumentNullException(nameof(filter.Value), $"Filter {filter.Operator} on param {paramSource.Name}");
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
                        string[] range = valueString.Split("..");
                        filteredData = filteredData.Where(d => !Enumerable.Range(Convert.ToInt32(range[0]), Convert.ToInt32(range[1]) - Convert.ToInt32(range[0])).ToList().Contains(d.GetValue<int>(filter.Field)));
                        break;
                    case FilterOperator.InRange:
                        string[] inRange = valueString.Split("..");
                        filteredData = filteredData.Where(d => Enumerable.Range(Convert.ToInt32(inRange[0]), Convert.ToInt32(inRange[1]) - Convert.ToInt32(inRange[0])).ToList().Contains(d.GetValue<int>(filter.Field)));
                        break;
                }
            }
        }

        return filteredData;
    }
}
