namespace DSLRNet.Core.Data;

public class DataSourceFactory
{
    public static IDataSource<T> CreateDataSource<T>(DataSourceConfig paramSource) 
        where T : new()
    {
        return paramSource.SourceType switch
        {
            DataSourceType.CSV => new CsvDataSource<T>(paramSource),
            DataSourceType.RegulationBin => new RegulationBinDataSource<T>(paramSource),
            _ => throw new ArgumentException("Invalid source type"),
        };
    }
}
