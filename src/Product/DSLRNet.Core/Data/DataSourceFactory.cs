namespace DSLRNet.Core.Data;

public class DataSourceFactory
{
    public static IDataSource<T> CreateDataSource<T>(DataSourceConfig paramSource, RandomProvider random) 
        where T : class, ICloneable, new()
    {
        return paramSource.SourceType switch
        {
            DataSourceType.CSV => new CsvDataSource<T>(paramSource, random),
            DataSourceType.RegulationBin => new RegulationBinDataSource<T>(paramSource, random),
            _ => throw new ArgumentException("Invalid source type"),
        };
    }
}
