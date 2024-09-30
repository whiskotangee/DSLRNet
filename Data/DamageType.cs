namespace DSLRNet.Data;

public class DamageType
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
    public int HitEffectCategory { get; set; }
    public int PriWeight { get; set; }
    public int SecWeight { get; set; }
    public double OverallMultiplier { get; set; }
    public string Message { get; set; }
    public string EffectDescription { get; set; }
    public bool NoSecondEffect { get; set; }
    public double CriticalMultAddition { get; set; }
    public int VFXSpEffectID { get; set; }
}
