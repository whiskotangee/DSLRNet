namespace DSLRNet.Data;

public class RarityConfig
{
    public int ID { get; set; }
    public string Name { get; set; }
    public int WeaponDmgAddMin { get; set; }
    public int WeaponDmgAddMax { get; set; }
    public double ArmorCutRateAddMin { get; set; }
    public double ArmorCutRateAddMax { get; set; }
    public int StatReqAddMin { get; set; }
    public int StatReqAddMax { get; set; }
    public double ShieldGuardRateMultMin { get; set; }
    public double ShieldGuardRateMultMax { get; set; }
    public int SpEffectPowerMin { get; set; }
    public int SpEffectPowerMax { get; set; }
    public double SpEffect0Chance { get; set; }
    public double SpEffect1Chance { get; set; }
    public double SpEffect2Chance { get; set; }
    public double SpEffect3Chance { get; set; }
    public int SelectionWeight { get; set; }
    public int LootDropChance { get; set; }
    public double WeightMultMin { get; set; }
    public double WeightMultMax { get; set; }
    public string ColorHex { get; set; }
    public int SellValueMin { get; set; }
    public int SellValueMax { get; set; }
    public int RarityParamValue { get; set; }
    public int ScalingMin { get; set; }
    public int ScalingMax { get; set; }
    public double ArmorCutRateMaxPerc { get; set; }
    public double ArmorResistMinMult { get; set; }
    public double ArmorResistMaxMult { get; set; }
}
