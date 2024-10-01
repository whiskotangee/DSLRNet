namespace DSLRNet.Config;

using Newtonsoft.Json;

public class Configuration
{
    public Settings Settings { get; set; }
    public AreaScalingConfig AreaScaling { get; set; }
    public ItemlotsConfig Itemlots { get; set; }
    public ParamNamesConfig ParamNames { get; set; }
    public UpgradesConfig Upgrades { get; set; }
    public DSLRDescTextConfig DSLRDescText { get; set; }
    public LootParamConfig LootParam { get; set; }
    public FlagsConfig Flags { get; set; }
    public Dictionary<string, List<string>> ItemlotParams { get; set; }
    public Dictionary<string, ItemlotBaseConfig> ItemlotBase { get; set; }
    public Dictionary<string, List<int>> FMGTypes { get; set; }
}

public class Settings
{
    public string DeployPath { get; set; }
    public string DSMSPortablePath { get; set; }

    public int LootPerItemLot { get; set; }

    public bool ChaosLootEnabled { get; set; }

    public double GlobalDropChance { get; set; }

    public bool AllLootGauranteed { get; set; }

    public string OverrideModLocation { get; set; }

    public string GamePath { get; set; }

    public List<string> MessageFileNames { get; set; }
}

public class AreaScalingConfig
{
    public List<int> SpeffectIds { get; set; }
    public string SpeffectParamName { get; set; }
    public List<string> ParamHp { get; set; }
    public List<string> ParamAttack { get; set; }
    public List<string> ParamDefence { get; set; }
}

public class ItemlotsConfig
{
    public List<string> ParamCategories { get; set; }
    public List<string> NpcParamCategories { get; set; }
    public ItemlotEditingArrayConfig ItemlotEditingArray { get; set; }
}

public class ItemlotEditingArrayConfig
{
    public List<string> ItemlotParams { get; set; }
    public string Luck { get; set; }
}

public class ParamNamesConfig
{
    public string NpcParam { get; set; }
    public string EquipMaterialSet { get; set; }
    public string ReinforceWeapon { get; set; }
}

public class UpgradesConfig
{
    public List<List<int>> IdStartEnd { get; set; }
    public string QuantityParam { get; set; }
    public string WeaponReinforceType { get; set; }
    public List<string> WeaponDamageRateParams { get; set; }
    public string WeaponBaseDamageParam { get; set; }
    public double WeaponBaseDamageMultiplier { get; set; }
}

public class DSLRDescTextConfig
{
    public string Effect { get; set; }
    public string NoStacking { get; set; }
}

public class OriginParamCount
{
    public string Name { get; set; }

    public int Count { get; set; }
}

public class LootParamConfig
{
    public string SellValueParam { get; set; }
    public string RarityParam { get; set; }
    public string WeaponOriginParamBase { get; set; }
    public string NoAffinityChange { get; set; }
    public List<string> WeaponBehSpeffects { get; set; }
    public SpeffectsConfig Speffects { get; set; }
    public string SpeffectsStackCategory { get; set; }
    public List<string> SpeffectMsg { get; set; }
    public WeaponSpecialMotionCategoriesConfig WeaponSpecialMotionCategories { get; set; }
    public List<string> WeaponsDamageParam { get; set; }
    public List<string> WeaponsGuardRateParam { get; set; }
    public List<string> WeaponsRealName { get; set; }
    public List<string> WeaponsTitleName { get; set; }
    public string WeaponsWepMotionCategory { get; set; }
    public string WeaponsThrowDamageParam { get; set; }
    public string WeaponsHitVfx { get; set; }
    public WeaponsCanCastParamConfig WeaponsCanCastParam { get; set; }
    public List<string> WeaponsScaling { get; set; }
    public List<string> WeaponsVfxSlots { get; set; }
    public List<string> WeaponsStatReq { get; set; }
    public string WeaponsStaminaRate { get; set; }
    public List<string> WeaponsVfxParam { get; set; }
    public List<string> WeaponsVfxDummyParam { get; set; }
    public List<int> WeaponsVfxDummies { get; set; }
    public string WeaponReinforceMaterialId { get; set; }
    public string WeaponReinforceTypeId { get; set; }
    public List<string> ArmorParam { get; set; }
    public List<string> ArmorRealName { get; set; }
    public List<string> ArmorDefenseParams { get; set; }
    public List<string> ArmorResistParams { get; set; }
    public string TalismansAccessoryGroupParam { get; set; }
    public List<string> OtherMandatoryParams { get; set; }
}

public class SpeffectsConfig
{
    public List<string> EquipParamWeapon { get; set; }
    public List<string> EquipParamProtector { get; set; }
    public List<string> EquipParamAccessory { get; set; }
}

public class WeaponSpecialMotionCategoriesConfig
{
    public List<int> Shields { get; set; }
    public List<int> StaffsSeals { get; set; }
    public List<int> BowsCrossbows { get; set; }
}

public class WeaponsCanCastParamConfig
{
    public string Sorcery { get; set; }
    public string Miracles { get; set; }
    public string Pyromancies { get; set; }
}

public class FlagsConfig
{
    public List<int> SaveableAcquisitionFlags { get; set; }
    public int ItemAcquisitionOffset { get; set; }
}

public class ItemlotBaseConfig
{
    public List<string> Header { get; set; }
    public List<object> Default { get; set; }
}
