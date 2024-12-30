
namespace DSLRNet.Core.Contracts;

public partial class SpEffectConfig : ParamBase<SpEffectConfig>
{
    public float Value { get; set; }
    public int SpEffectPower { get; set; }
    public string Prefix { get; set; } = string.Empty;
    public string Interfix { get; set; } = string.Empty;
    public string Suffix { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public int SpEffectType { get; set; }
    public int TalismanIcon { get; set; }
    public int TalismanSortID { get; set; }
    public int OverrideStacking { get; set; }
    public int Stacks { get; set; }
    public int InformationOnly { get; set; }
}