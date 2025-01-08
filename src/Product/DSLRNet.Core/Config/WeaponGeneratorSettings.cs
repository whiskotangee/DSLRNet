namespace DSLRNet.Core.Config;

using IniParser.Model;
using Newtonsoft.Json.Linq;
using PaintDotNet;
using System;

public enum WeaponTypes { Normal, Shields, StaffsSeals, BowsCrossbows }

public class WeaponGeneratorSettings
{
    public int UniqueNameChance { get; set; }

    public float UniqueWeaponMultiplier { get; set; }

    public string UniqueItemNameColor { get; set; } = string.Empty;

    public int SplitDamageTypeChance { get; set; }

    public int DamageIncreasesStaminaThreshold { get; set; }

    public IntValueRange CritChanceRange { get; set; } = new IntValueRange(5, 20);

    public IntValueRange PrimaryBaseScalingRange { get; set; } = new IntValueRange(50, 75);

    public IntValueRange SecondaryBaseScalingRange { get; set; } = new IntValueRange(30, 50);

    public IntValueRange OtherBaseScalingRange { get; set; } = new IntValueRange(5, 25);

    public void Initialize(IniData data)
    {
        var section = "Settings.WeaponGeneratorSettings";
        if (data.Sections.ContainsSection(section))
        {
            var weaponSection = data[section];
            UniqueNameChance = weaponSection.ContainsKey("UniqueNameChance") && int.TryParse(weaponSection["UniqueNameChance"], out var uniqueNameChance) ? uniqueNameChance : 4;
            UniqueWeaponMultiplier = weaponSection.ContainsKey("UniqueWeaponMultiplier") && float.TryParse(weaponSection["UniqueWeaponMultiplier"], NumberStyles.Float, CultureInfo.InvariantCulture, out var uniqueWeaponMultiplier) ? uniqueWeaponMultiplier: 1.2f;
            UniqueItemNameColor = weaponSection.ContainsKey("UniqueItemNameColor") ? weaponSection["UniqueItemNameColor"] : "ffa3c5";
            SplitDamageTypeChance = weaponSection.ContainsKey("SplitDamageTypeChance") && int.TryParse(weaponSection["SplitDamageTypeChance"], out var splitDamageTypeChance) ? splitDamageTypeChance : 70;
            DamageIncreasesStaminaThreshold = weaponSection.ContainsKey("DamageIncreasesStaminaThreshold") && int.TryParse(weaponSection["DamageIncreasesStaminaThreshold"], out var damageIncreasesStaminaThreshold) ? damageIncreasesStaminaThreshold : 170;

            var critChanceRangeSection = $"{section}.CritChanceRange";
            if (data.Sections.ContainsSection(critChanceRangeSection))
            {
                CritChanceRange = new IntValueRange(
                    int.TryParse(data[critChanceRangeSection]["Min"], out var minCritChance) ? minCritChance : 5,
                    int.TryParse(data[critChanceRangeSection]["Max"], out var maxCritChance) ? maxCritChance : 20
                );
            }

            var primaryScalingRangeSection = $"{section}.PrimaryBaseScalingRange";
            if (data.Sections.ContainsSection(primaryScalingRangeSection))
            {
                PrimaryBaseScalingRange = new IntValueRange(
                    int.TryParse(data[primaryScalingRangeSection]["Min"], out var minPrimaryBaseScaling) ? minPrimaryBaseScaling : 65,
                    int.TryParse(data[primaryScalingRangeSection]["Max"], out var maxPrimaryBaseScaling) ? maxPrimaryBaseScaling : 105
                );
            }

            var secondaryScalingRangeSection = $"{section}.SecondaryBaseScalingRange";
            if (data.Sections.ContainsSection(secondaryScalingRangeSection))
            {
                SecondaryBaseScalingRange = new IntValueRange(
                    int.TryParse(data[secondaryScalingRangeSection]["Min"], out var minSecondaryBaseScaling) ? minSecondaryBaseScaling : 45,
                    int.TryParse(data[secondaryScalingRangeSection]["Max"], out var maxSecondaryBaseScaling) ? maxSecondaryBaseScaling : 65
                );
            }

            var otherScalingRangeSection = $"{section}.OtherBaseScalingRange";
            if (data.Sections.ContainsSection(otherScalingRangeSection))
            {
                OtherBaseScalingRange = new IntValueRange(
                    int.TryParse(data[otherScalingRangeSection]["Min"], out var minOtherBaseScaling) ? minOtherBaseScaling : 10,
                    int.TryParse(data[otherScalingRangeSection]["Max"], out var maxOtherBaseScaling) ? maxOtherBaseScaling : 15
                );
            }
        }
    }

    public class AshOfWarConfig
    {
        public List<WeaponTypeCanMountWepFlag> WeaponTypeCanMountWepFlags { get; set; } = [];
    }

    public class WeaponTypeCanMountWepFlag
    {
        public long Id { get; set; }

        public string FriendlyName { get; set; } = string.Empty;

        public string FlagName { get; set; } = string.Empty;
    }
