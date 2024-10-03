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
}
