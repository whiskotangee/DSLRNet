namespace DSLRNet.Core.Data;

public class CsvDataSource<T>(DataSourceConfig paramSource, RandomNumberGetter random) : BaseDataSource<T>(random)
    where T : class, ICloneable, new()
{
    public override IEnumerable<T> LoadData()
    {
        return Csv.LoadCsv<T>(paramSource.SourcePath).ToList();
    }
}
