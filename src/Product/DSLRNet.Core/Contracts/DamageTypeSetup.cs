
namespace DSLRNet.Core.Contracts;

public partial class DamageTypeSetup : ParamBase<DamageTypeSetup>
{
    public string Name { get; set; } = string.Empty;
    public string DamageElement { get; set; } = string.Empty;
    public string PriName { get; set; } = string.Empty;
    public string SecName { get; set; } = string.Empty;
    public string Param { get; set; } = string.Empty;
    public string ShieldParam { get; set; } = string.Empty;
    public int SpEffect { get; set; }
    public int OnHitSpEffect { get; set; }
    public byte HitEffectCategory { get; set; }
    public int PriWeight { get; set; }
    public int SecWeight { get; set; }
    public float OverallMultiplier { get; set; }
    public int Message { get; set; }
    public string EffectDescription { get; set; } = string.Empty;
    public int NoSecondEffect { get; set; }
    public float CriticalMultAddition { get; set; }
    public int VFXSpEffectID { get; set; }
}