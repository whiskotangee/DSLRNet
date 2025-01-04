namespace DSLRNet.Models;
using DSLRNet.Core.Config;

public class ItemLotGeneratorSettingsWrapper : BaseModel<ItemLotGeneratorSettings>
{
    private readonly ItemLotGeneratorSettings _settings;

    public ItemLotGeneratorSettingsWrapper(ItemLotGeneratorSettings settings)
    {
        _settings = settings;
        OriginalObject = _settings;
        ChestLootScannerSettings = new(_settings.ChestLootScannerSettings);
        MapLootScannerSettings = new(_settings.MapLootScannerSettings);
        EnemyLootScannerSettings = new(_settings.EnemyLootScannerSettings);
    }

    public int ItemLotsPerBaseMapLot
    {
        get => _settings.ItemLotsPerBaseMapLot;
        set
        {
            if (_settings.ItemLotsPerBaseMapLot != value)
            {
                _settings.ItemLotsPerBaseMapLot = value;
                OnPropertyChanged();
            }
        }
    }

    public int ItemLotsPerBaseEnemyLot
    {
        get => _settings.ItemLotsPerBaseEnemyLot;
        set
        {
            if (_settings.ItemLotsPerBaseEnemyLot != value)
            {
                _settings.ItemLotsPerBaseEnemyLot = value;
                OnPropertyChanged();
            }
        }
    }

    public int ItemLotsPerBossLot
    {
        get => _settings.ItemLotsPerBossLot;
        set
        {
            if (_settings.ItemLotsPerBossLot != value)
            {
                _settings.ItemLotsPerBossLot = value;
                OnPropertyChanged();
            }
        }
    }

    public int LootPerItemLot_Enemy
    {
        get => _settings.LootPerItemLot_Enemy;
        set
        {
            if (_settings.LootPerItemLot_Enemy != value)
            {
                _settings.LootPerItemLot_Enemy = value;
                OnPropertyChanged();
            }
        }
    }

    public int LootPerItemLot_Map
    {
        get => _settings.LootPerItemLot_Map;
        set
        {
            if (_settings.LootPerItemLot_Map != value)
            {
                _settings.LootPerItemLot_Map = value;
                OnPropertyChanged();
            }
        }
    }

    public int LootPerItemLot_Bosses
    {
        get => _settings.LootPerItemLot_Bosses;
        set
        {
            if (_settings.LootPerItemLot_Bosses != value)
            {
                _settings.LootPerItemLot_Bosses = value;
                OnPropertyChanged();
            }
        }
    }

    public ScannerSettingsWrapper ChestLootScannerSettings { get; } 

    public ScannerSettingsWrapper MapLootScannerSettings { get; }

    public ScannerSettingsWrapper EnemyLootScannerSettings { get; }

    public bool ChaosLootEnabled
    {
        get => _settings.ChaosLootEnabled;
        set
        {
            if (_settings.ChaosLootEnabled != value)
            {
                _settings.ChaosLootEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    public int GlobalDropChance
    {
        get => _settings.GlobalDropChance;
        set
        {
            if (_settings.GlobalDropChance != value)
            {
                _settings.GlobalDropChance = value;
                OnPropertyChanged();
            }
        }
    }

    public bool AllLootGauranteed
    {
        get => _settings.AllLootGauranteed;
        set
        {
            if (_settings.AllLootGauranteed != value)
            {
                _settings.AllLootGauranteed = value;
                OnPropertyChanged();
            }
        }
    }
}
