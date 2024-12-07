namespace DSLRNet.Core.Data;

public class DataSourceFactory
{
    public static IDataSource<T> CreateDataSource<T>(DataSourceConfig paramSource, RandomProvider random, Csv csv)
        where T : class, ICloneable<T>, new()
    {
        return paramSource.SourceType switch
        {
            DataSourceType.CSV => new CsvDataSource<T>(paramSource, random, csv),
            DataSourceType.RegulationBin => new RegulationBinDataSource<T>(paramSource, random),
            _ => throw new ArgumentException("Invalid source type"),
        };
    }
}
