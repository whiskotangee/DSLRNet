namespace DSLRNet.Core.Config;

using IniParser.Model;
using IniParser;
using System.Collections.Generic;
using DSLRNet.Core.Extensions;

public class Settings
{
    public void ValidatePaths()
    {
        if (string.IsNullOrEmpty(DeployPath))
        {
            throw new DirectoryNotFoundException($"Deploy path must be specified");
        }

        if (!Directory.Exists(DeployPath))
        {
            Directory.CreateDirectory(DeployPath);
        }

        if (OrderedModPaths.Count > 0 && OrderedModPaths.Any(x => !string.IsNullOrWhiteSpace(x) && !Directory.Exists(x)))
        {
            throw new DirectoryNotFoundException($"One or more mod paths do not exist.");
        }

        if (!Directory.Exists(GamePath))
        {
            throw new DirectoryNotFoundException($"Game path {GamePath} does not exist or is not specified.");
        }
    }

    public string DeployPath { get; set; } = string.Empty;

    public List<string> OrderedModPaths { get; set; } = [];

    public ItemLotGeneratorSettings ItemLotGeneratorSettings { get; set; } = new ItemLotGeneratorSettings();

    public int RandomSeed { get; set; }

    public string GamePath { get; set; } = string.Empty;

    public List<string> MessageFileNames { get; set; } = [];

    public bool RestrictSmithingStoneCost { get; set; }

    public ArmorGeneratorSettings ArmorGeneratorSettings { get; set; } = new ArmorGeneratorSettings();

    public WeaponGeneratorSettings WeaponGeneratorSettings { get; set; } = new WeaponGeneratorSettings();

    public IconBuilderSettings IconBuilderSettings { get; set; } = new IconBuilderSettings() { IconSheetSettings  = new IconSheetSettings() };

    public void SaveSettings(string path)
    {
        IniData data = new();
        data["Settings"]["DeployPath"] = DeployPath;
        data["Settings"]["OrderedModPaths"] = string.Join(",", OrderedModPaths);
        data["Settings"]["RandomSeed"] = RandomSeed.ToString();
        data["Settings"]["GamePath"] = GamePath;
        data["Settings"]["MessageFileNames"] = string.Join(",", MessageFileNames);
        data["Settings"]["RestrictSmithingStoneCost"] = RestrictSmithingStoneCost.ToString();
        data["Settings.ItemLotGeneratorSettings"]["ItemLotsPerBaseMapLot"] = ItemLotGeneratorSettings.ItemLotsPerBaseMapLot.ToString();
        data["Settings.ItemLotGeneratorSettings"]["ItemLotsPerBaseEnemyLot"] = ItemLotGeneratorSettings.ItemLotsPerBaseEnemyLot.ToString();
        data["Settings.ItemLotGeneratorSettings"]["ItemLotsPerBossLot"] = ItemLotGeneratorSettings.ItemLotsPerBossLot.ToString();
        data["Settings.ItemLotGeneratorSettings"]["LootPerItemLot_Enemy"] = ItemLotGeneratorSettings.LootPerItemLot_Enemy.ToString();
        data["Settings.ItemLotGeneratorSettings"]["LootPerItemLot_Map"] = ItemLotGeneratorSettings.LootPerItemLot_Map.ToString();
        data["Settings.ItemLotGeneratorSettings"]["LootPerItemLot_Bosses"] = ItemLotGeneratorSettings.LootPerItemLot_Bosses.ToString();
        data["Settings.ItemLotGeneratorSettings.ChestLootScannerSettings"]["Enabled"] = ItemLotGeneratorSettings.ChestLootScannerSettings.Enabled.ToString();
        data["Settings.ItemLotGeneratorSettings.ChestLootScannerSettings"]["ApplyPercent"] = ItemLotGeneratorSettings.ChestLootScannerSettings.ApplyPercent.ToString();
        data["Settings.ItemLotGeneratorSettings.MapLootScannerSettings"]["Enabled"] = ItemLotGeneratorSettings.MapLootScannerSettings.Enabled.ToString();
        data["Settings.ItemLotGeneratorSettings.MapLootScannerSettings"]["ApplyPercent"] = ItemLotGeneratorSettings.MapLootScannerSettings.ApplyPercent.ToString();
        data["Settings.ItemLotGeneratorSettings.EnemyLootScannerSettings"]["Enabled"] = ItemLotGeneratorSettings.EnemyLootScannerSettings.Enabled.ToString();
        data["Settings.ItemLotGeneratorSettings.EnemyLootScannerSettings"]["ApplyPercent"] = ItemLotGeneratorSettings.EnemyLootScannerSettings.ApplyPercent.ToString();
        data["Settings.ItemLotGeneratorSettings"]["ChaosLootEnabled"] = ItemLotGeneratorSettings.ChaosLootEnabled.ToString();
        data["Settings.ItemLotGeneratorSettings"]["GlobalDropChance"] = ItemLotGeneratorSettings.GlobalDropChance.ToString();
        data["Settings.ItemLotGeneratorSettings"]["AllLootGauranteed"] = ItemLotGeneratorSettings.AllLootGauranteed.ToString();
        data["Settings.ArmorGeneratorSettings"]["CutRateDescriptionTemplate"] = ArmorGeneratorSettings.CutRateDescriptionTemplate;
        data["Settings.ArmorGeneratorSettings"]["ResistParamBuffCount"] = ArmorGeneratorSettings.ResistParamBuffCount.ToString();
        data["Settings.ArmorGeneratorSettings"]["CutRateParamBuffCount"] = ArmorGeneratorSettings.CutRateParamBuffCount.ToString();
        data["Settings.WeaponGeneratorSettings"]["UniqueNameChance"] = WeaponGeneratorSettings.UniqueNameChance.ToString();
        data["Settings.WeaponGeneratorSettings"]["UniqueWeaponMultiplier"] = WeaponGeneratorSettings.UniqueWeaponMultiplier.ToString();
        data["Settings.WeaponGeneratorSettings"]["UniqueItemNameColor"] = WeaponGeneratorSettings.UniqueItemNameColor;
        data["Settings.WeaponGeneratorSettings"]["SplitDamageTypeChance"] = WeaponGeneratorSettings.SplitDamageTypeChance.ToString();
        data["Settings.WeaponGeneratorSettings"]["DamageIncreasesStaminaThreshold"] = WeaponGeneratorSettings.DamageIncreasesStaminaThreshold.ToString();
        data["Settings.WeaponGeneratorSettings.CritChanceRange"]["Min"] = WeaponGeneratorSettings.CritChanceRange.Min.ToString();
        data["Settings.WeaponGeneratorSettings.CritChanceRange"]["Max"] = WeaponGeneratorSettings.CritChanceRange.Max.ToString();
        data["Settings.WeaponGeneratorSettings.PrimaryBaseScalingRange"]["Min"] = WeaponGeneratorSettings.PrimaryBaseScalingRange.Min.ToString();
        data["Settings.WeaponGeneratorSettings.PrimaryBaseScalingRange"]["Max"] = WeaponGeneratorSettings.PrimaryBaseScalingRange.Max.ToString();
        data["Settings.WeaponGeneratorSettings.SecondaryBaseScalingRange"]["Min"] = WeaponGeneratorSettings.SecondaryBaseScalingRange.Min.ToString();
        data["Settings.WeaponGeneratorSettings.SecondaryBaseScalingRange"]["Max"] = WeaponGeneratorSettings.SecondaryBaseScalingRange.Max.ToString();
        data["Settings.WeaponGeneratorSettings.OtherBaseScalingRange"]["Min"] = WeaponGeneratorSettings.OtherBaseScalingRange.Min.ToString();
        data["Settings.WeaponGeneratorSettings.OtherBaseScalingRange"]["Max"] = WeaponGeneratorSettings.OtherBaseScalingRange.Max.ToString();
        data["Settings.IconBuilderSettings"]["RegenerateIconSheets"] = IconBuilderSettings.RegenerateIconSheets.ToString();
        data["Settings.IconBuilderSettings.IconSheetSettings"]["StartAt"] = IconBuilderSettings.IconSheetSettings.StartAt.ToString();
        data["Settings.IconBuilderSettings.IconSheetSettings"]["GoalIconsPerSheet"] = IconBuilderSettings.IconSheetSettings.GoalIconsPerSheet.ToString();
        data["Settings.IconBuilderSettings.IconSheetSettings.IconDimensions"]["IconSize"] = IconBuilderSettings.IconSheetSettings.IconDimensions.IconSize.ToString();
        data["Settings.IconBuilderSettings.IconSheetSettings.IconDimensions"]["Padding"] = IconBuilderSettings.IconSheetSettings.IconDimensions.Padding.ToString();
        data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity1"]["RarityIds"] = string.Join(",", IconBuilderSettings.IconSheetSettings.Rarities[0].RarityIds);
        data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity1"]["BackgroundImageName"] = IconBuilderSettings.IconSheetSettings.Rarities[0].BackgroundImageName;
        data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity2"]["RarityIds"] = string.Join(",", IconBuilderSettings.IconSheetSettings.Rarities[1].RarityIds);
        data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity2"]["BackgroundImageName"] = IconBuilderSettings.IconSheetSettings.Rarities[1].BackgroundImageName;
        data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity3"]["RarityIds"] = string.Join(",", IconBuilderSettings.IconSheetSettings.Rarities[2].RarityIds);
        data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity3"]["BackgroundImageName"] = IconBuilderSettings.IconSheetSettings.Rarities[2].BackgroundImageName;
        data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity4"]["RarityIds"] = string.Join(",", IconBuilderSettings.IconSheetSettings.Rarities[3].RarityIds);
        data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity4"]["BackgroundImageName"] = IconBuilderSettings.IconSheetSettings.Rarities[3].BackgroundImageName;
        data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity5"]["RarityIds"] = string.Join(",", IconBuilderSettings.IconSheetSettings.Rarities[4].RarityIds);
        data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity5"]["BackgroundImageName"] = IconBuilderSettings.IconSheetSettings.Rarities[4].BackgroundImageName;

        File.WriteAllLines(path, data.ToString().Split("\n"));
    }

    public static Settings CreateFromSettingsIni()
    {
        string settingsFilePath = PathHelper.FullyQualifyAppDomainPath("Settings.ini");
        string userSettingsFilePath = PathHelper.FullyQualifyAppDomainPath("Settings.User.ini");

        FileIniDataParser parser = new();
        IniData data;

        if (!File.Exists(userSettingsFilePath))
        {
            File.Copy(settingsFilePath, userSettingsFilePath, true);
        }

        data = parser.ReadFile(userSettingsFilePath);

        Settings settings = new();
        settings.Initialize(data);
        return settings;
    }

    public void Initialize(IniData data)
    {
        var section = data["Settings"];
        DeployPath = section.ContainsKey("DeployPath") ? section["DeployPath"] : string.Empty;
        OrderedModPaths = section.ContainsKey("OrderedModPaths") ? [.. section["OrderedModPaths"].Split(',')] : [];
        RandomSeed = section.ContainsKey("RandomSeed") && int.TryParse(section["RandomSeed"], out int result) ? result : new Random().Next();
        GamePath = section.ContainsKey("GamePath") ? section["GamePath"] : string.Empty;
        MessageFileNames = section.ContainsKey("MessageFileNames") ? [.. section["MessageFileNames"].Split(',')] : [];
        RestrictSmithingStoneCost = section.ContainsKey("RestrictSmithingStoneCost") && bool.Parse(section["RestrictSmithingStoneCost"]);

        ItemLotGeneratorSettings = new ItemLotGeneratorSettings();
        ItemLotGeneratorSettings.Initialize(data);

        ArmorGeneratorSettings = new ArmorGeneratorSettings();
        ArmorGeneratorSettings.Initialize(data);

        WeaponGeneratorSettings = new WeaponGeneratorSettings();
        WeaponGeneratorSettings.Initialize(data);

        IconBuilderSettings = new IconBuilderSettings();
        IconBuilderSettings.Initialize(data);
    }
}

