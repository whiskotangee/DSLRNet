namespace DSLRNet.Core.Config;

using IniParser.Model;
using IniParser;
using System.Collections.Generic;

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

    public static Settings? CreateFromSettingsIni()
    {
        FileIniDataParser parser = new();
        IniData data;

        if (!File.Exists("Settings.User.ini"))
        {
            File.Copy("Settings.ini", "Settings.User.ini");
        }

        data = parser.ReadFile("Settings.User.ini");

        Settings settings = new()
        {
            DeployPath = data["Settings"]["DeployPath"],
            OrderedModPaths = [.. data["Settings"]["OrderedModPaths"].Split(",")],
            RandomSeed = int.TryParse(data["Settings"]["RandomSeed"], out int result) ? result : new Random().Next(),
            GamePath = data["Settings"]["GamePath"],
            MessageFileNames = [.. data["Settings"]["MessageFileNames"].Split(",")],
            RestrictSmithingStoneCost = bool.Parse(data["Settings"]["RestrictSmithingStoneCost"]),
            ItemLotGeneratorSettings = new ItemLotGeneratorSettings
            {
                ItemLotsPerBaseMapLot = int.Parse(data["Settings.ItemLotGeneratorSettings"]["ItemLotsPerBaseMapLot"]),
                ItemLotsPerBaseEnemyLot = int.Parse(data["Settings.ItemLotGeneratorSettings"]["ItemLotsPerBaseEnemyLot"]),
                ItemLotsPerBossLot = int.Parse(data["Settings.ItemLotGeneratorSettings"]["ItemLotsPerBossLot"]),
                LootPerItemLot_Enemy = int.Parse(data["Settings.ItemLotGeneratorSettings"]["LootPerItemLot_Enemy"]),
                LootPerItemLot_Map = int.Parse(data["Settings.ItemLotGeneratorSettings"]["LootPerItemLot_Map"]),
                LootPerItemLot_Bosses = int.Parse(data["Settings.ItemLotGeneratorSettings"]["LootPerItemLot_Bosses"]),
                ChestLootScannerSettings = new ScannerSettings
                {
                    Enabled = bool.Parse(data["Settings.ItemLotGeneratorSettings.ChestLootScannerSettings"]["Enabled"]),
                    ApplyPercent = int.Parse(data["Settings.ItemLotGeneratorSettings.ChestLootScannerSettings"]["ApplyPercent"])
                },
                MapLootScannerSettings = new ScannerSettings
                {
                    Enabled = bool.Parse(data["Settings.ItemLotGeneratorSettings.MapLootScannerSettings"]["Enabled"]),
                    ApplyPercent = int.Parse(data["Settings.ItemLotGeneratorSettings.MapLootScannerSettings"]["ApplyPercent"])
                },
                EnemyLootScannerSettings = new ScannerSettings
                {
                    Enabled = bool.Parse(data["Settings.ItemLotGeneratorSettings.EnemyLootScannerSettings"]["Enabled"]),
                    ApplyPercent = int.Parse(data["Settings.ItemLotGeneratorSettings.EnemyLootScannerSettings"]["ApplyPercent"])
                },
                ChaosLootEnabled = bool.Parse(data["Settings.ItemLotGeneratorSettings"]["ChaosLootEnabled"]),
                GlobalDropChance = int.Parse(data["Settings.ItemLotGeneratorSettings"]["GlobalDropChance"]),
                AllLootGauranteed = bool.Parse(data["Settings.ItemLotGeneratorSettings"]["AllLootGauranteed"])
            },
            ArmorGeneratorSettings = new ArmorGeneratorSettings
            {
                CutRateDescriptionTemplate = data["Settings.ArmorGeneratorSettings"]["CutRateDescriptionTemplate"],
                ResistParamBuffCount = int.Parse(data["Settings.ArmorGeneratorSettings"]["ResistParamBuffCount"]),
                CutRateParamBuffCount = int.Parse(data["Settings.ArmorGeneratorSettings"]["CutRateParamBuffCount"])
            },
            WeaponGeneratorSettings = new WeaponGeneratorSettings
            {
                UniqueNameChance = int.Parse(data["Settings.WeaponGeneratorSettings"]["UniqueNameChance"]),
                UniqueWeaponMultiplier = float.Parse(data["Settings.WeaponGeneratorSettings"]["UniqueWeaponMultiplier"]),
                UniqueItemNameColor = data["Settings.WeaponGeneratorSettings"]["UniqueItemNameColor"],
                SplitDamageTypeChance = int.Parse(data["Settings.WeaponGeneratorSettings"]["SplitDamageTypeChance"]),
                DamageIncreasesStaminaThreshold = int.Parse(data["Settings.WeaponGeneratorSettings"]["DamageIncreasesStaminaThreshold"]),
                CritChanceRange = new IntValueRange(
                    int.Parse(data["Settings.WeaponGeneratorSettings.CritChanceRange"]["Min"]),
                    int.Parse(data["Settings.WeaponGeneratorSettings.CritChanceRange"]["Max"])
                ),
                PrimaryBaseScalingRange = new IntValueRange(
                    int.Parse(data["Settings.WeaponGeneratorSettings.PrimaryBaseScalingRange"]["Min"]),
                    int.Parse(data["Settings.WeaponGeneratorSettings.PrimaryBaseScalingRange"]["Max"])
                ),
                SecondaryBaseScalingRange = new IntValueRange(
                    int.Parse(data["Settings.WeaponGeneratorSettings.SecondaryBaseScalingRange"]["Min"]),
                    int.Parse(data["Settings.WeaponGeneratorSettings.SecondaryBaseScalingRange"]["Max"])
                ),
                OtherBaseScalingRange = new IntValueRange(
                    int.Parse(data["Settings.WeaponGeneratorSettings.OtherBaseScalingRange"]["Min"]),
                    int.Parse(data["Settings.WeaponGeneratorSettings.OtherBaseScalingRange"]["Max"])
                )
            },
            IconBuilderSettings = new IconBuilderSettings
            {
                RegenerateIconSheets = bool.Parse(data["Settings.IconBuilderSettings"]["RegenerateIconSheets"]),
                IconSheetSettings = new IconSheetSettings
                {
                    GoalIconsPerSheet = int.Parse(data["Settings.IconBuilderSettings.IconSheetSettings"]["GoalIconsPerSheet"]),
                    StartAt = int.Parse(data["Settings.IconBuilderSettings.IconSheetSettings"]["StartAt"]),
                    IconDimensions = new IconDimensions
                    {
                        IconSize = int.Parse(data["Settings.IconBuilderSettings.IconSheetSettings.IconDimensions"]["IconSize"]),
                        Padding = int.Parse(data["Settings.IconBuilderSettings.IconSheetSettings.IconDimensions"]["Padding"])
                    },
                    Rarities =
                    [
                        new RarityIconDetails
                        {
                            Name = data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity1"]["Name"],
                            RarityIds = data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity1"]["RarityIds"].Split(',').Select(int.Parse).ToList(),
                            BackgroundImageName = data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity1"]["BackgroundImageName"]
                        },
                        new RarityIconDetails
                        {
                            Name = data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity2"]["Name"],
                            RarityIds = data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity2"]["RarityIds"].Split(',').Select(int.Parse).ToList(),
                            BackgroundImageName = data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity2"]["BackgroundImageName"]
                        },
                        new RarityIconDetails
                        {
                            Name = data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity3"]["Name"],
                            RarityIds = data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity3"]["RarityIds"].Split(',').Select(int.Parse).ToList(),
                            BackgroundImageName = data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity3"]["BackgroundImageName"]
                        },
                        new RarityIconDetails
                        {
                            Name = data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity4"]["Name"],
                            RarityIds = data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity4"]["RarityIds"].Split(',').Select(int.Parse).ToList(),
                            BackgroundImageName = data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity4"]["BackgroundImageName"]
                        },
                        new RarityIconDetails
                        {   
                            Name = data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity5"]["Name"],
                            RarityIds = data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity5"]["RarityIds"].Split(',').Select(int.Parse).ToList(),
                            BackgroundImageName = data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity5"]["BackgroundImageName"]
                        }
                    ]
                }
            }
        };

        return settings;
    }
}

