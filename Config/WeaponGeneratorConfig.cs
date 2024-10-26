namespace DSLRNet.Config;

public enum WeaponTypes { Normal, Shields, StaffsSeals, BowsCrossbows }

public class WeaponGeneratorConfig
{
    public string DamageDescription { get; set; }

    public float UniqueNameChance { get; set; }

    public float UniqueWeaponMultiplier { get; set; }

    public float SplitDamageTypeChance { get; set; }

    public List<WeaponTypes> Types { get; set; }

    public List<int> Weights { get; set; }

    public AshOfWarConfig AshOfWarConfig { get; set;}
}

public class AshOfWarConfig
{
    public List<WeaponTypeToCanMountWepFlag> WeaponTypeToCanMountWepFlags { get; set; }
}

public class WeaponTypeToCanMountWepFlag
{
    public long Id { get; set; }

    public string FriendlyName { get; set; }

    public string FlagName { get; set; }
}
