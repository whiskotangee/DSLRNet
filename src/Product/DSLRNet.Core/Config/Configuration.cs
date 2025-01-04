namespace DSLRNet.Core.Config;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public class Configuration
{
    public ItemlotsConfig Itemlots { get; set; } = new ItemlotsConfig();
    public DSLRDescTextConfig DSLRDescText { get; set; } = new DSLRDescTextConfig();
    public LootParamConfig LootParam { get; set; } = new LootParamConfig();

    public List<DataSourceConfig> DataSourceConfigs { get; set; } = [];

    public ScannerConfig ScannerConfig { get; set; } = new ScannerConfig();

    public AshOfWarConfig AshOfWarConfig { get; set; } = new AshOfWarConfig();
}


public enum DataSourceType { CSV, RegulationBin }

public enum DataSourceNames
{
    // actual params
    EquipParamWeapon,
    EquipParamCustomWeapon,
    EquipParamAccessory,
    EquipParamProtector,
    EquipMtrlSetParam,
    EquipParamGem,
    NpcParam,
    SpEffectParamNew,
    SpEffectParam,
    ItemLotParam_enemy,
    ItemLotParam_map,
    GameAreaParam,
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

    public string SourcePath { get; set; } = string.Empty;

    public List<Filter> Filters { get; set; } = [];
}

public class ItemlotsConfig
{
    public List<Category> Categories { get; set; } = [];
}

public class Category
{
    public string ParamCategory { get; set; } = string.Empty;

    public string NpcParamCategory { get; set; } = string.Empty;
}

public class DSLRDescTextConfig
{
    public string Effect { get; set; } = string.Empty;
    public string NoStacking { get; set; } = string.Empty;
}

public class LootParamConfig
{
    public List<string> WeaponBehSpeffects { get; set; } = [];
    public SpeffectsConfig Speffects { get; set; } = new SpeffectsConfig();
    public List<string> SpeffectMsg { get; set; } = [];
    public WeaponSpecialMotionCategoriesConfig WeaponSpecialMotionCategories { get; set; } = new WeaponSpecialMotionCategoriesConfig();
    public List<string> WeaponsVfxParam { get; set; } = [];
    public List<string> WeaponsVfxDummyParam { get; set; } = [];
    public List<int> WeaponsVfxDummies { get; set; } = [];
}

public class SpeffectsConfig
{
    public List<string> EquipParamWeapon { get; set; } = [];
    public List<string> EquipParamProtector { get; set; } = [];
    public List<string> EquipParamAccessory { get; set; } = [];
}

public class WeaponSpecialMotionCategoriesConfig
{
    public List<int> Shields { get; set; } = [];
    public List<int> StaffsSeals { get; set; } = [];
    public List<int> BowsCrossbows { get; set; } = [];
}

public class RarityIconMappingConfig
{
    public List<IconSheetParameters> IconSheets { get; set; } = [];
}

public class IconSheetParameters
{
    [JsonIgnore]
    public byte[] GeneratedBytes { get; set; } = [];

    public RarityIconMapping IconMappings { get; set; } = new RarityIconMapping();

    public string Name { get; set; } = string.Empty;
}

public class RarityIconMapping
{
    public List<int> RarityIds { get; set; } = [];

    public List<IconMapping> IconReplacements { get; set; } = [];
}

public class IconMapping
{
    public int OriginalIconId { get; set; }

    public ushort NewIconId { get; set; }

    public string SourceIconPath { get; set; } = string.Empty;

    public int TileX { get; set; }

    public int TileY { get; set; }

    [JsonIgnore]
    public Image<Rgba32>? ConvertedIcon { get; set; }
}

public class ScannerConfig
{
    public List<int> AreaScalingSpEffectIds { get; set; } = [];
}

public class CommonBossEventConfig
{
    public long EventId { get; set; }
    public string EventName { get; set; } = string.Empty;

    public int BankId { get; set; }

    public int InstructionId { get; set; }

    public int EventIdIndex { get; set; } = -1;

    public HashSet<int> EntityIdIndexes { get; set; } = [];

    public HashSet<int> FlagIndexes { get; set; } = [];

    public HashSet<int> ItemLotIdIndexes { get; set; } = [];

    public int AcquisitionFlagIndex { get; set; } = -1;

    public long HardCodedEntityId { get; set; }

    public HashSet<long> HardCodedFlags { get; set; } = [];

    public HashSet<long> HardCodedItemLots { get; set; } = [];
}
    
