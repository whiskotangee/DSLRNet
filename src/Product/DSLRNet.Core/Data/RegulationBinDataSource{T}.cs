namespace DSLRNet.Core.Data;

public class RegulationBinDataSource<T>(DataSourceConfig paramSource) : IDataSource<T> where T : new()
{
    public IEnumerable<T> LoadAll()
    {
        throw new NotImplementedException();
    }

    public void ResetLoadedData()
    {
        throw new NotImplementedException();
    }
}
