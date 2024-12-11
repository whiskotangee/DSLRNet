namespace DSLRNet.Core.Data;

using DSLRNet.Core.DAL;

public class RegulationBinDataSource<T>(
    DataSourceConfig paramSource, 
    RegulationBinReader regulationBinReader,
    RandomProvider random) : BaseDataSource<T>(random)
    where T : ParamBase<T>, ICloneable<T>, new()
{
    private PARAM? readParam = null;

    public override IEnumerable<T> LoadData()
    {
        if (readParam == null)
        {
            this.readParam = regulationBinReader.GetParam(paramSource.Name);
        }

        return this.readParam.Rows.Select(this.CreateFromPARAM).ToList();
    }

    private T CreateFromPARAM(PARAM.Row row)
    {
        T newObject = new();

        newObject.GenericParam.ID = row.ID;
        newObject.GenericParam.Name = row.Name;

        foreach (var cell in row.Cells)
        {
            if (cell.Value as byte[] != null)
            {
                newObject.GenericParam.SetValue(cell.Def.InternalName, $"[{string.Join("|", cell.Value)}]");
            }
            else
            {
                newObject.GenericParam.SetValue(cell.Def.InternalName, cell.Value);
            }
            
        }

        return newObject;
    }
}
