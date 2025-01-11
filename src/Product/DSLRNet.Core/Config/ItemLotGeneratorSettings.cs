namespace DSLRNet.Core.Config;

using IniParser.Model;
using System;

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

    public void Initialize(IniData data)
    {
        var section = "Settings.ItemLotGeneratorSettings";
        if (data.Sections.ContainsSection(section))
        {
            var itemLotSection = data[section];
            ItemLotsPerBaseMapLot = itemLotSection.ContainsKey("ItemLotsPerBaseMapLot") && int.TryParse(itemLotSection["ItemLotsPerBaseMapLot"], out int val) ? val : 1;
            ItemLotsPerBaseEnemyLot = itemLotSection.ContainsKey("ItemLotsPerBaseEnemyLot") && int.TryParse(itemLotSection["ItemLotsPerBaseEnemyLot"], out val) ? val : 1;
            ItemLotsPerBossLot = itemLotSection.ContainsKey("ItemLotsPerBossLot") && int.TryParse(itemLotSection["ItemLotsPerBossLot"], out val) ? val : 2;
            LootPerItemLot_Enemy = itemLotSection.ContainsKey("LootPerItemLot_Enemy") && int.TryParse(itemLotSection["LootPerItemLot_Enemy"], out val) ? val : 4;
            LootPerItemLot_Map = itemLotSection.ContainsKey("LootPerItemLot_Map") && int.TryParse(itemLotSection["LootPerItemLot_Map"], out val) ? val : 1;
            LootPerItemLot_Bosses = itemLotSection.ContainsKey("LootPerItemLot_Bosses") && int.TryParse(itemLotSection["LootPerItemLot_Bosses"], out val) ? val : 1;

            ChestLootScannerSettings = new ScannerSettings();
            ChestLootScannerSettings.Initialize(data, section);

            MapLootScannerSettings = new ScannerSettings();
            MapLootScannerSettings.Initialize(data, section);

            EnemyLootScannerSettings = new ScannerSettings();
            EnemyLootScannerSettings.Initialize(data, section);

            ChaosLootEnabled = itemLotSection.ContainsKey("ChaosLootEnabled") && bool.TryParse(itemLotSection["ChaosLootEnabled"], out bool boolVal) && boolVal;
            GlobalDropChance = itemLotSection.ContainsKey("GlobalDropChance") && int.TryParse(itemLotSection["GlobalDropChance"], out val) ? val : 4;
            AllLootGauranteed = itemLotSection.ContainsKey("AllLootGauranteed") && bool.TryParse(itemLotSection["AllLootGauranteed"], out boolVal) && boolVal;
        }
    }
}

public class ScannerSettings
{
    public bool Enabled { get; set; }

    public int ApplyPercent { get; set; }

    public void Initialize(IniData data, string parentSection)
    {
        var section = $"{parentSection}.ChestLootScannerSettings";
        if (data.Sections.ContainsSection(section))
        {
            var scannerSection = data[section];
            Enabled = scannerSection.ContainsKey("Enabled") && bool.TryParse(scannerSection["Enabled"], out var boolVal) && boolVal;
            ApplyPercent = scannerSection.ContainsKey("ApplyPercent") && int.TryParse(scannerSection["ApplyPercent"], out var val) ? val : 0;
        }
    }
}
