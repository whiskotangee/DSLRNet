namespace DSLRNet.Core.Data;

public class RegulationBinDataSource<T>(DataSourceConfig paramSource, RandomProvider random) : BaseDataSource<T>(random)
    where T : class, ICloneable<T>, new()
{
    public override IEnumerable<T> LoadData()
    {
        // TODO: Remove reliance on CSV files by loading directly from regulation bin
        // This will require grabbing the PARAMDEF files from DSMS in order to read them
        throw new NotImplementedException();
    }
}
