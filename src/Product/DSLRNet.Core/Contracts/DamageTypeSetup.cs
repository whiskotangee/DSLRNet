
namespace DSLRNet.Core.Contracts;

public partial class DamageTypeSetup : ParamBase<DamageTypeSetup>
{
    public int ID { get; set; }
    public string Name { get; set; }
    public string DamageElement { get; set; }
    public string PriName { get; set; }
    public string SecName { get; set; }
    public string Param { get; set; }
    public string ShieldParam { get; set; }
    public int SpEffect { get; set; }
    public int OnHitSpEffect { get; set; }
    public byte HitEffectCategory { get; set; }
    public int PriWeight { get; set; }
    public int SecWeight { get; set; }
    public float OverallMultiplier { get; set; }
    public int Message { get; set; }
    public string EffectDescription { get; set; }
    public int NoSecondEffect { get; set; }
    public float CriticalMultAddition { get; set; }
    public int VFXSpEffectID { get; set; }
}