
namespace DSLRNet.Core.Contracts;

using DSLRNet.Core.Common;

public enum ParamOperation { Create, MassEdit, TextOnly }

public class ParamEdit
{
    public ParamOperation Operation { get; set; }

    public ParamNames ParamName { get; set; }

    public LootFMG? MessageText { get; set; }

    public string MassEditString { get; set; }

    public GenericParam ParamObject { get; set; }
}
