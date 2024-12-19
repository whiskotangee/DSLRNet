namespace DSLRNet.Core.Config;

public enum WeaponTypes { Normal, Shields, StaffsSeals, BowsCrossbows }

public class WeaponGeneratorSettings
{
    public float UniqueNameChance { get; set; }

    public float UniqueWeaponMultiplier { get; set; }

    public float SplitDamageTypeChance { get; set; }

    public int DamageIncreasesStaminaThreshold { get; set; }

    public IntValueRange CritChanceRange { get; set; }

    public IntValueRange PrimaryBaseScalingRange { get; set; }

    public IntValueRange SecondaryBaseScalingRange { get; set; }

    public IntValueRange OtherBaseScalingRange { get; set; }
}

public class AshOfWarConfig
{
    public List<WeaponTypeCanMountWepFlag> WeaponTypeCanMountWepFlags { get; set; }
}

public class WeaponTypeCanMountWepFlag
{
    public long Id { get; set; }

    public string FriendlyName { get; set; }

    public string FlagName { get; set; }
}
