namespace DSLRNet.Core.Contracts.Params;

public class ReinforceParamWeapon : ParamBase<ReinforceParamWeapon>
{
    public string Name { get { return this.GetValue<string>("Name"); } set { this.SetValue("Name", value); } }
    public float physicsAtkRate { get { return this.GetValue<float>("physicsAtkRate"); } set { this.SetValue("physicsAtkRate", value); } }
    public float magicAtkRate { get { return this.GetValue<float>("magicAtkRate"); } set { this.SetValue("magicAtkRate", value); } }
    public float fireAtkRate { get { return this.GetValue<float>("fireAtkRate"); } set { this.SetValue("fireAtkRate", value); } }
    public float thunderAtkRate { get { return this.GetValue<float>("thunderAtkRate"); } set { this.SetValue("thunderAtkRate", value); } }
    public float staminaAtkRate { get { return this.GetValue<float>("staminaAtkRate"); } set { this.SetValue("staminaAtkRate", value); } }
    public float saWeaponAtkRate { get { return this.GetValue<float>("saWeaponAtkRate"); } set { this.SetValue("saWeaponAtkRate", value); } }
    public float saDurabilityRate { get { return this.GetValue<float>("saDurabilityRate"); } set { this.SetValue("saDurabilityRate", value); } }
    public float correctStrengthRate { get { return this.GetValue<float>("correctStrengthRate"); } set { this.SetValue("correctStrengthRate", value); } }
    public float correctAgilityRate { get { return this.GetValue<float>("correctAgilityRate"); } set { this.SetValue("correctAgilityRate", value); } }
    public float correctMagicRate { get { return this.GetValue<float>("correctMagicRate"); } set { this.SetValue("correctMagicRate", value); } }
    public float correctFaithRate { get { return this.GetValue<float>("correctFaithRate"); } set { this.SetValue("correctFaithRate", value); } }
    public float physicsGuardCutRate { get { return this.GetValue<float>("physicsGuardCutRate"); } set { this.SetValue("physicsGuardCutRate", value); } }
    public float magicGuardCutRate { get { return this.GetValue<float>("magicGuardCutRate"); } set { this.SetValue("magicGuardCutRate", value); } }
    public float fireGuardCutRate { get { return this.GetValue<float>("fireGuardCutRate"); } set { this.SetValue("fireGuardCutRate", value); } }
    public float thunderGuardCutRate { get { return this.GetValue<float>("thunderGuardCutRate"); } set { this.SetValue("thunderGuardCutRate", value); } }
    public float poisonGuardResistRate { get { return this.GetValue<float>("poisonGuardResistRate"); } set { this.SetValue("poisonGuardResistRate", value); } }
    public float diseaseGuardResistRate { get { return this.GetValue<float>("diseaseGuardResistRate"); } set { this.SetValue("diseaseGuardResistRate", value); } }
    public float bloodGuardResistRate { get { return this.GetValue<float>("bloodGuardResistRate"); } set { this.SetValue("bloodGuardResistRate", value); } }
    public float curseGuardResistRate { get { return this.GetValue<float>("curseGuardResistRate"); } set { this.SetValue("curseGuardResistRate", value); } }
    public float staminaGuardDefRate { get { return this.GetValue<float>("staminaGuardDefRate"); } set { this.SetValue("staminaGuardDefRate", value); } }
    public byte spEffectId1 { get { return this.GetValue<byte>("spEffectId1"); } set { this.SetValue("spEffectId1", value); } }
    public byte spEffectId2 { get { return this.GetValue<byte>("spEffectId2"); } set { this.SetValue("spEffectId2", value); } }
    public byte spEffectId3 { get { return this.GetValue<byte>("spEffectId3"); } set { this.SetValue("spEffectId3", value); } }
    public byte residentSpEffectId1 { get { return this.GetValue<byte>("residentSpEffectId1"); } set { this.SetValue("residentSpEffectId1", value); } }
    public byte residentSpEffectId2 { get { return this.GetValue<byte>("residentSpEffectId2"); } set { this.SetValue("residentSpEffectId2", value); } }
    public byte residentSpEffectId3 { get { return this.GetValue<byte>("residentSpEffectId3"); } set { this.SetValue("residentSpEffectId3", value); } }
    public byte materialSetId { get { return this.GetValue<byte>("materialSetId"); } set { this.SetValue("materialSetId", value); } }
    public byte maxReinforceLevel { get { return this.GetValue<byte>("maxReinforceLevel"); } set { this.SetValue("maxReinforceLevel", value); } }
    public float darkAtkRate { get { return this.GetValue<float>("darkAtkRate"); } set { this.SetValue("darkAtkRate", value); } }
    public float darkGuardCutRate { get { return this.GetValue<float>("darkGuardCutRate"); } set { this.SetValue("darkGuardCutRate", value); } }
    public float correctLuckRate { get { return this.GetValue<float>("correctLuckRate"); } set { this.SetValue("correctLuckRate", value); } }
    public float freezeGuardDefRate { get { return this.GetValue<float>("freezeGuardDefRate"); } set { this.SetValue("freezeGuardDefRate", value); } }
    public float reinforcePriceRate { get { return this.GetValue<float>("reinforcePriceRate"); } set { this.SetValue("reinforcePriceRate", value); } }
    public float baseChangePriceRate { get { return this.GetValue<float>("baseChangePriceRate"); } set { this.SetValue("baseChangePriceRate", value); } }
    public sbyte enableGemRank { get { return this.GetValue<sbyte>("enableGemRank"); } set { this.SetValue("enableGemRank", value); } }
    public byte[] pad2 { get { return this.GetValue<byte[]>("pad2"); } set { this.SetValue("pad2", value); } }
    public float sleepGuardDefRate { get { return this.GetValue<float>("sleepGuardDefRate"); } set { this.SetValue("sleepGuardDefRate", value); } }
    public float madnessGuardDefRate { get { return this.GetValue<float>("madnessGuardDefRate"); } set { this.SetValue("madnessGuardDefRate", value); } }
    public float baseAtkRate { get { return this.GetValue<float>("baseAtkRate"); } set { this.SetValue("baseAtkRate", value); } }
}
