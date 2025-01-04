namespace DSLRNet.Core.Config;

using IniParser.Model;
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
            UniqueNameChance = weaponSection.ContainsKey("UniqueNameChance") ? int.Parse(weaponSection["UniqueNameChance"]) : 4;
            UniqueWeaponMultiplier = weaponSection.ContainsKey("UniqueWeaponMultiplier") ? float.Parse(weaponSection["UniqueWeaponMultiplier"]) : 1.2f;
            UniqueItemNameColor = weaponSection.ContainsKey("UniqueItemNameColor") ? weaponSection["UniqueItemNameColor"] : string.Empty;
            SplitDamageTypeChance = weaponSection.ContainsKey("SplitDamageTypeChance") ? int.Parse(weaponSection["SplitDamageTypeChance"]) : 50;
            DamageIncreasesStaminaThreshold = weaponSection.ContainsKey("DamageIncreasesStaminaThreshold") ? int.Parse(weaponSection["DamageIncreasesStaminaThreshold"]) : 170;

            var critChanceRangeSection = $"{section}.CritChanceRange";
            if (data.Sections.ContainsSection(critChanceRangeSection))
            {
                CritChanceRange = new IntValueRange(
                    int.Parse(data[critChanceRangeSection]["Min"]),
                    int.Parse(data[critChanceRangeSection]["Max"])
                );
            }

            var primaryScalingRangeSection = $"{section}.PrimaryBaseScalingRange";
            if (data.Sections.ContainsSection(primaryScalingRangeSection))
            {
                PrimaryBaseScalingRange = new IntValueRange(
                    int.Parse(data[primaryScalingRangeSection]["Min"]),
                    int.Parse(data[primaryScalingRangeSection]["Max"])
                );
            }

            var secondaryScalingRangeSection = $"{section}.SecondaryBaseScalingRange";
            if (data.Sections.ContainsSection(secondaryScalingRangeSection))
            {
                SecondaryBaseScalingRange = new IntValueRange(
                    int.Parse(data[secondaryScalingRangeSection]["Min"]),
                    int.Parse(data[secondaryScalingRangeSection]["Max"])
                );
            }

            var otherScalingRangeSection = $"{section}.OtherBaseScalingRange";
            if (data.Sections.ContainsSection(otherScalingRangeSection))
            {
                OtherBaseScalingRange = new IntValueRange(
                    int.Parse(data[otherScalingRangeSection]["Min"]),
                    int.Parse(data[otherScalingRangeSection]["Max"])
                );
            }
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
