namespace DSLRNet.Core.Data;

public class CsvDataSource<T>(DataSourceConfig paramSource) : IDataSource<T> 
    where T : new()
{
    private List<T>? cachedData;

    public IEnumerable<T> LoadAll()
    {
        if (this.cachedData != null)
        {
            return this.cachedData;
        }

        ResetLoadedData();

        return this.cachedData;
    }

    public void ResetLoadedData()
    {
        this.cachedData = Csv.LoadCsv<T>(paramSource.SourcePath);
    }
}
