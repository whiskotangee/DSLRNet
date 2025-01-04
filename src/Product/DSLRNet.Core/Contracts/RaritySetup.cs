
namespace DSLRNet.Core.Contracts;

public partial class RaritySetup : ParamBase<RaritySetup>
{
    public string Name { get; set; } = string.Empty;
    public int WeaponDmgAddMin { get; set; }
    public int WeaponDmgAddMax { get; set; }
    public float ArmorCutRateAddMin { get; set; }
    public float ArmorCutRateAddMax { get; set; }
    public int StatReqAddMin { get; set; }
    public int StatReqAddMax { get; set; }
    public float ShieldGuardRateMultMin { get; set; }
    public float ShieldGuardRateMultMax { get; set; }
    public int SpEffectPowerMin { get; set; }
    public int SpEffectPowerMax { get; set; }
    public float SpEffectChance0 { get; set; }
    public float SpEffectChance1 { get; set; }
    public float SpEffectChance2 { get; set; }
    public float SpEffectChance3 { get; set; }
    public int SelectionWeight { get; set; }
    public int LootDropChance { get; set; }
    public float WeightMultMin { get; set; }
    public float WeightMultMax { get; set; }
    public string ColorHex { get; set; } = string.Empty;
    public int SellValueMin { get; set; }
    public int SellValueMax { get; set; }
    public int RarityParamValue { get; set; }
    public int ScalingMin { get; set; }
    public int ScalingMax { get; set; }
    public float ArmorCutRateMaxPerc { get; set; }
    public float ArmorResistMinMult { get; set; }
    public float ArmorResistMaxMult { get; set; }
}