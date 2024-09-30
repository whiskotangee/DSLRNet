namespace DSLRNet.Config;

public enum WeaponTypes { Normal, Shields, StaffsSeals, BowsCrossbows }

public class WeaponGeneratorConfig
{
    public string DamageDescription { get; set; }

    public double UniqueNameChance { get; set; }

    public double UniqueWeaponMultiplier { get; set; }

    public double SplitDamageTypeChance { get; set; }

    public List<WeaponTypes> Types { get; set; }

    public List<int> Weights { get; set; }
}
