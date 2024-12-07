namespace DSLRNet.Core.Config;

public enum WeaponTypes { Normal, Shields, StaffsSeals, BowsCrossbows }

public class WeaponGeneratorConfig
{
    public float UniqueNameChance { get; set; }

    public float UniqueWeaponMultiplier { get; set; }

    public float SplitDamageTypeChance { get; set; }

    public List<WeaponTypes> Types { get; set; }

    public List<int> Weights { get; set; }
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
