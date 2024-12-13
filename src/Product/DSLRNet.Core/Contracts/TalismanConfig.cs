
namespace DSLRNet.Core.Contracts;

public partial class TalismanConfig : ParamBase<TalismanConfig>
{
    public int ID { get; set; }
    public string Effect { get; set; }
    public string ShortEffect { get; set; }
    public int RefSpEffect { get; set; }
    public short NoStackingGroupID { get; set; }
    public string NamePrefix { get; set; }
}