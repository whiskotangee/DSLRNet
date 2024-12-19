namespace DSLRNet.Core.Config;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public class Configuration
{
    public ItemlotsConfig Itemlots { get; set; }
    public DSLRDescTextConfig DSLRDescText { get; set; }
    public LootParamConfig LootParam { get; set; }

    public List<DataSourceConfig> DataSourceConfigs { get; set; }

    public AutoScalingSettings ScannerAutoScalingSettings { get; set; }

    public AshOfWarConfig AshOfWarConfig { get; set; }
}


public enum DataSourceType { CSV, RegulationBin }

public enum DataSourceNames
{
    // actual params
    EquipParamWeapon,
    EquipParamCustomWeapon,
    EquipParamAccessory,
    EquipParamProtector,
    EquipParamGem,
    NpcParam,
    SpEffectParamNew,
    SpEffectParam,
    ItemLotParam_enemy,
    ItemLotParam_map,
    // config setups
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

    public List<Filter> Filters { get; set; }
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
    public List<string> WeaponBehSpeffects { get; set; }
    public SpeffectsConfig Speffects { get; set; }
    public List<string> SpeffectMsg { get; set; }
    public WeaponSpecialMotionCategoriesConfig WeaponSpecialMotionCategories { get; set; }
    public List<string> WeaponsVfxParam { get; set; }
    public List<string> WeaponsVfxDummyParam { get; set; }
    public List<int> WeaponsVfxDummies { get; set; }
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

public class RarityIconMappingConfig
{
    public List<IconSheetParameters> IconSheets { get; set; }
}

public class IconSheetParameters
{
    [JsonIgnore]
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

    public ushort NewIconId { get; set; }

    public string SourceIconPath { get; set; }

    public int TileX { get; set; }

    public int TileY { get; set; }

    [JsonIgnore]
    public Image<Rgba32>? ConvertedIcon { get; set; }
}

public class AutoScalingSettings
{
    public List<int> AreaScalingSpEffectIds { get; set; } = [];
}
