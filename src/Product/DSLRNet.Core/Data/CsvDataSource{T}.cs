namespace DSLRNet.Core.Data;

public class CsvDataSource<T>(DataSourceConfig paramSource, RandomProvider random, Csv csv) : BaseDataSource<T>(random)
    where T : class, ICloneable<T>, new()
{
    public override IEnumerable<T> LoadData()
    {
        return csv.LoadCsv<T>(paramSource.SourcePath).ToList();
    }
}
