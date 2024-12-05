namespace DSLRNet.Core.Config;
public class Configuration
{
    public Settings Settings { get; set; }
    public ItemlotsConfig Itemlots { get; set; }
    public DSLRDescTextConfig DSLRDescText { get; set; }
    public LootParamConfig LootParam { get; set; }
}

public class ScannerSettings
{
    public bool Enabled { get; set; }

    public int ApplyPercent { get; set; }
}

public class ItemLotGeneratorSettings
{
    public int ItemLotsPerBaseMapLot { get; set; }

    public int LootPerItemLot_Enemy { get; set; }

    public int LootPerItemLot_Map { get; set; }

    public ScannerSettings ChestLootScannerSettings { get; set; }

    public ScannerSettings MapLootScannerSettings { get; set; }

    public ScannerSettings EnemyLootScannerSettings { get; set; }

    public bool ChaosLootEnabled { get; set; }

    public string UniqueItemColor { get; set; }

    public float GlobalDropChance { get; set; }

    public bool AllLootGauranteed { get; set; }
}

public class Settings
{
    public string DeployPath { get; set; }
    public string DSMSPortablePath { get; set; }

    public ItemLotGeneratorSettings ItemLotGeneratorSettings { get; set; }

    public int RandomSeed { get; set; }

    public List<string> MessageSourcePaths { get; set; } = [];

    public string GamePath { get; set; }

    public List<string> MessageFileNames { get; set; }

    public List<DataSourceConfig> DataSourceConfigs { get; set; }
}

public enum DataSourceType { CSV, RegulationBin }

public enum DataSourceNames
{
    EquipParamWeapon,
    EquipParamAccessory,
    EquipParamProtector,
    EquipParamGem,
    NpcParam,
    SpEffectParam,
    ItemLotParam_enemy,
    ItemLotParam_map,
    RaritySetup,
    ItemLotBase,
    DamageTypeSetup,
    TalismanConfig,
    SpEffectConfig
}

public class DataSourceConfig
{
    public DataSourceNames Name { get; set; }

    public DataSourceType SourceType { get; set; }

    public string SourcePath { get; set; }
}

public class ItemlotsConfig
{
    public List<Category> Categories { get; set; }
}

public class Category
{
    public string ParamCategory { get; set; }

    public string NpcParamCategory { get; set; }
}

public class DSLRDescTextConfig
{
    public string Effect { get; set; }
    public string NoStacking { get; set; }
}

public class LootParamConfig
{
    public string WeaponOriginParamBase { get; set; }
    public List<string> WeaponBehSpeffects { get; set; }
    public SpeffectsConfig Speffects { get; set; }
    public string SpeffectsStackCategory { get; set; }
    public List<string> SpeffectMsg { get; set; }
    public WeaponSpecialMotionCategoriesConfig WeaponSpecialMotionCategories { get; set; }
    public List<string> WeaponsDamageParam { get; set; }
    public string WeaponsThrowDamageParam { get; set; }
    public string WeaponsHitVfx { get; set; }
    public WeaponsCanCastParamConfig WeaponsCanCastParam { get; set; }
    public List<string> WeaponsScaling { get; set; }
    public string WeaponsStaminaRate { get; set; }
    public List<string> WeaponsVfxParam { get; set; }
    public List<string> WeaponsVfxDummyParam { get; set; }
    public List<int> WeaponsVfxDummies { get; set; }
    public List<string> ArmorParam { get; set; }
    public List<string> ArmorDefenseParams { get; set; }
    public List<string> ArmorResistParams { get; set; }
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
}

public class RarityIconMappingConfig
{
    public List<IconSheetParameters> IconSheets { get; set; }
}

public class IconSheetParameters
{
    public byte[] GeneratedBytes { get; set; }

    public RarityIconMapping IconMappings { get; set; }

    public string Name { get; set; }
}

public class RarityIconMapping
{
    public List<int> RarityIds { get; set; }

    public List<IconMapping> IconReplacements { get; set; }
}

public class IconMapping
{
    public int OriginalIconId { get; set; }

    public int NewIconId { get; set; }

    public string SourceIconPath { get; set; }

    public int TileX { get; set; }

    public int TileY { get; set; }
}