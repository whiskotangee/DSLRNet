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
            ItemLotsPerBaseMapLot = itemLotSection.ContainsKey("ItemLotsPerBaseMapLot") ? int.Parse(itemLotSection["ItemLotsPerBaseMapLot"]) : 1;
            ItemLotsPerBaseEnemyLot = itemLotSection.ContainsKey("ItemLotsPerBaseEnemyLot") ? int.Parse(itemLotSection["ItemLotsPerBaseEnemyLot"]) : 1;
            ItemLotsPerBossLot = itemLotSection.ContainsKey("ItemLotsPerBossLot") ? int.Parse(itemLotSection["ItemLotsPerBossLot"]) : 2;
            LootPerItemLot_Enemy = itemLotSection.ContainsKey("LootPerItemLot_Enemy") ? int.Parse(itemLotSection["LootPerItemLot_Enemy"]) : 4;
            LootPerItemLot_Map = itemLotSection.ContainsKey("LootPerItemLot_Map") ? int.Parse(itemLotSection["LootPerItemLot_Map"]) : 1;
            LootPerItemLot_Bosses = itemLotSection.ContainsKey("LootPerItemLot_Bosses") ? int.Parse(itemLotSection["LootPerItemLot_Bosses"]) : 1;

            ChestLootScannerSettings = new ScannerSettings();
            ChestLootScannerSettings.Initialize(data, section);

            MapLootScannerSettings = new ScannerSettings();
            MapLootScannerSettings.Initialize(data, section);

            EnemyLootScannerSettings = new ScannerSettings();
            EnemyLootScannerSettings.Initialize(data, section);

            ChaosLootEnabled = itemLotSection.ContainsKey("ChaosLootEnabled") && bool.Parse(itemLotSection["ChaosLootEnabled"]);
            GlobalDropChance = itemLotSection.ContainsKey("GlobalDropChance") ? int.Parse(itemLotSection["GlobalDropChance"]) : 4;
            AllLootGauranteed = itemLotSection.ContainsKey("AllLootGauranteed") && bool.Parse(itemLotSection["AllLootGauranteed"]);
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
            Enabled = scannerSection.ContainsKey("Enabled") && bool.Parse(scannerSection["Enabled"]);
            ApplyPercent = scannerSection.ContainsKey("ApplyPercent") ? int.Parse(scannerSection["ApplyPercent"]) : 0;
        }
    }
}
