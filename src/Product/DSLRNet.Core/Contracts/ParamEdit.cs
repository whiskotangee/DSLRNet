
namespace DSLRNet.Core.Contracts;

using DSLRNet.Core.Common;

public enum ParamOperation { Create, TextOnly }

public class ParamEdit
{
    public ParamOperation Operation { get; set; }

    public ParamNames ParamName { get; set; }

    public LootFMG? MessageText { get; set; }

    public required GenericParam ParamObject { get; set; }
}
