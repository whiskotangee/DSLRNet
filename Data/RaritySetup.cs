
namespace DSLRNet.Data;

public class RaritySetup
{
    public int ID { get; set; }
    public string Name { get; set; }
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
    public float SpEffect0Chance { get; set; }
    public float SpEffect1Chance { get; set; }
    public float SpEffect2Chance { get; set; }
    public float SpEffect3Chance { get; set; }
    public int SelectionWeight { get; set; }
    public int LootDropChance { get; set; }
    public float WeightMultMin { get; set; }
    public float WeightMultMax { get; set; }
    public string ColorHex { get; set; }
    public int SellValueMin { get; set; }
    public int SellValueMax { get; set; }
    public int RarityParamValue { get; set; }
    public int ScalingMin { get; set; }
    public int ScalingMax { get; set; }
    public float ArmorCutRateMaxPerc { get; set; }
    public float ArmorResistMinMult { get; set; }
    public float ArmorResistMaxMult { get; set; }
}