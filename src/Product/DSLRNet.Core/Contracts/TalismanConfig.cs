
namespace DSLRNet.Core.Contracts;

public partial class TalismanConfig : ParamBase<TalismanConfig>
{
    public string Effect { get; set; } = string.Empty;
    public string ShortEffect { get; set; } = string.Empty;
    public int RefSpEffect { get; set; }
    public short NoStackingGroupID { get; set; }
    public string NamePrefix { get; set; } = string.Empty;  
}