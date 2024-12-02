namespace DSLRNet.Core.Data;

public class RegulationBinDataSource<T>(DataSourceConfig paramSource, RandomProvider random) : BaseDataSource<T>(random)
    where T : class, ICloneable<T>, new()
{
    public override IEnumerable<T> LoadData()
    {
        throw new NotImplementedException();
    }
}
