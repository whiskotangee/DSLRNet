namespace DSLRNet.Core.Contracts;

public class WeaponModifications
{
    public WeaponModifications(DamageTypeSetup primaryDamage, DamageTypeSetup? secondaryDamage)
    {
        this.PrimaryDamageType = primaryDamage;
        this.SecondaryDamageType = secondaryDamage;
    }

    public DamageTypeSetup PrimaryDamageType { get; set; }

    public float? PrimaryDamageValue { get; set; }

    public DamageTypeSetup? SecondaryDamageType { get; set; }

    public float? SecondaryDamageValue { get; set; }

    public List<string> SpEffectDescriptions { get; set; } = [];

    public List<SpEffectText> SpEffectTexts { get; set; } = [];
}

public class NameParts
{
    public string Suffix { get; set; } = string.Empty;

    public string Prefix { get; set; } = string.Empty;

    public string Interfix { get; set; } = string.Empty;
}
