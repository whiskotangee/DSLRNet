namespace DSLRNet.Core.Config;


public class ItemLotGeneratorSettings
{
    public int ItemLotsPerBaseMapLot { get; set; }

    public int ItemLotsPerBaseEnemyLot { get; set; }

    public int ItemLotsPerBossLot { get; set; }

    public int LootPerItemLot_Enemy { get; set; }

    public int LootPerItemLot_Map { get; set; }

    public int LootPerItemLot_Bosses { get; set; }

    public ScannerSettings ChestLootScannerSettings { get; set; } = new ScannerSettings();

    public ScannerSettings MapLootScannerSettings { get; set; } = new ScannerSettings();

    public ScannerSettings EnemyLootScannerSettings { get; set; } = new ScannerSettings();

    public bool ChaosLootEnabled { get; set; }

    public int GlobalDropChance { get; set; }

    public bool AllLootGauranteed { get; set; }
}



public class ScannerSettings
{
    public bool Enabled { get; set; }

    public int ApplyPercent { get; set; }
}
