namespace DSLRNet.Core.DAL;

using System.Threading;

public class CsvDataSource<T>(DataSourceConfig paramSource, RandomProvider random, Csv csv) : BaseDataSource<T>(random)
    where T : class, ICloneable<T>, new()
{
    public override Task<IEnumerable<T>> LoadDataAsync()
    {       
        List<T> list = csv.LoadCsv<T>(paramSource.SourcePath);
        return Task.FromResult(list.AsEnumerable());
    }
}
