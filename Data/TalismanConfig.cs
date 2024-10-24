
namespace DSLRNet.Data;

public partial class TalismanConfig
{
    public int ID { get; set; }
    public string Effect { get; set; }
    public string ShortEffect { get; set; }
    public int RefSpEffect { get; set; }
    public int NoStackingGroupID { get; set; }

    public string NamePrefix { get; set; }
}