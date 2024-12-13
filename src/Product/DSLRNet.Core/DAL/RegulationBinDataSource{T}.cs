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
    private static SemaphoreSlim semaphore = new SemaphoreSlim(1);

    public async override Task<IEnumerable<T>> LoadDataAsync()
    {
        if (readParam == null)
        {
            this.readParam = regulationBinReader.GetParam(paramSource.Name);
        }

        ConcurrentBag<T> loadedValues = [];
        await Parallel.ForEachAsync(
            this.readParam.Rows,
            new ParallelOptions() { MaxDegreeOfParallelism = 1000 },
            async (row, c) =>
            {
                loadedValues.Add(await this.CreateFromPARAMAsync(row));
            });

        return [.. loadedValues];
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
        newObject.GenericParam.Name = row.Name;

        foreach (var cell in row.Cells)
        {
            newObject.GenericParam.SetValue(cell.Def.InternalName, cell.Value);
        }

        return newObject;
    }
}
