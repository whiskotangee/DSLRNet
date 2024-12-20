namespace DSLRNet.Core.Config;

using ImageMagick;
using IniParser.Model;
using IniParser;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

public class Settings
{
    public string DeployPath { get; set; }

    public List<string> OrderedModPaths { get; set; }

    public ItemLotGeneratorSettings ItemLotGeneratorSettings { get; set; }

    public int RandomSeed { get; set; }

    public string GamePath { get; set; }

    public List<string> MessageFileNames { get; set; }

    public ArmorGeneratorSettings ArmorGeneratorSettings { get; set; }

    public WeaponGeneratorSettings WeaponGeneratorSettings { get; set; }

    public IconBuilderSettings IconBuilderSettings { get; set; }

    public static Settings? CreateFromSettingsIni()
    {
        var parser = new FileIniDataParser();
        IniData data = parser.ReadFile("Settings.ini");

        var settings = new Settings
        {
            DeployPath = data["Settings"]["DeployPath"],
            OrderedModPaths = [.. data["Settings"]["OrderedModPaths"].Split(",")],
            RandomSeed = int.Parse(data["Settings"]["RandomSeed"]),
            GamePath = data["Settings"]["GamePath"],
            MessageFileNames = [.. data["Settings"]["MessageFileNames"].Split(",")],
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
                GlobalDropChance = float.Parse(data["Settings.ItemLotGeneratorSettings"]["GlobalDropChance"]),
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
                UniqueNameChance = float.Parse(data["Settings.WeaponGeneratorSettings"]["UniqueNameChance"]),
                UniqueWeaponMultiplier = float.Parse(data["Settings.WeaponGeneratorSettings"]["UniqueWeaponMultiplier"]),
                UniqueItemNameColor = data["Settings.WeaponGeneratorSettings"]["UniqueItemNameColor"],
                SplitDamageTypeChance = float.Parse(data["Settings.WeaponGeneratorSettings"]["SplitDamageTypeChance"]),
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
                GenerateHiDefIcons = bool.Parse(data["Settings.IconBuilderSettings"]["GenerateHiDefIcons"]),
                IconSourcePath = data["Settings.IconBuilderSettings"]["IconSourcePath"],
                IconSheetSettings = new IconSheetSettings
                {
                    GoalIconsPerSheet = int.Parse(data["Settings.IconBuilderSettings.IconSheetSettings"]["GoalIconsPerSheet"]),
                    IconDimensions = new IconDimensions
                    {
                        IconSize = int.Parse(data["Settings.IconBuilderSettings.IconSheetSettings.IconDimensions"]["IconSize"]),
                        Padding = int.Parse(data["Settings.IconBuilderSettings.IconSheetSettings.IconDimensions"]["Padding"])
                    },
                    Rarities = 
                    [
                        new RarityIconDetails
                        {
                            RarityIds = data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity1"]["RarityIds"].Split(',').Select(int.Parse).ToList(),
                            BackgroundImageName = data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity1"]["BackgroundImageName"]
                        },
                        new RarityIconDetails
                        {
                            RarityIds = data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity2"]["RarityIds"].Split(',').Select(int.Parse).ToList(),
                            BackgroundImageName = data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity2"]["BackgroundImageName"]
                        },
                        new RarityIconDetails
                        {
                            RarityIds = data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity3"]["RarityIds"].Split(',').Select(int.Parse).ToList(),
                            BackgroundImageName = data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity3"]["BackgroundImageName"]
                        },
                        new RarityIconDetails
                        {
                            RarityIds = data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity4"]["RarityIds"].Split(',').Select(int.Parse).ToList(),
                            BackgroundImageName = data["Settings.IconBuilderSettings.IconSheetSettings.Rarities.Rarity4"]["BackgroundImageName"]
                        },
                        new RarityIconDetails
                        {
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

