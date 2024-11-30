
namespace DSLRNet.Core.Data;

using DSLRNet.Core;
using DSLRNet.Core.Contracts;

public enum ParamOperation { Create, MassEdit, TextOnly }

public class ParamEdit
{
    public ParamOperation Operation { get; set; }

    public ParamNames ParamName { get; set; }

    public LootFMG? MessageText { get; set; }

    public string MassEditString { get; set; }

    public GenericDictionary ParamObject { get; set; }
}
