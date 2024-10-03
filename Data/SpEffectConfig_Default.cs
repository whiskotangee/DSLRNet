
namespace DSLRNet.Data;

public class SpEffectConfig
{
    public int ID { get; set; }
    public int Value { get; set; }
    public int SpEffectPower { get; set; }
    public string Prefix { get; set; }
    public string Interfix { get; set; }
    public string Suffix { get; set; }
    public string Description { get; set; }
    public string ShortDescription { get; set; }
    public int SpEffectType { get; set; }
    public int TalismanIcon { get; set; }
    public int TalismanSortID { get; set; }
    public int OverrideStacking { get; set; }
    public int Stacks { get; set; }
    public int InformationOnly { get; set; }
}