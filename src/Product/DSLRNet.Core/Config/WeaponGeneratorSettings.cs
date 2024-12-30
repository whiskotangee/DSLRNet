namespace DSLRNet.Core.Config;

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
