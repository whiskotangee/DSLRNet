namespace DSLRNet.Core.Contracts;

public class WeaponModifications(DamageTypeSetup primaryDamage, DamageTypeSetup? secondaryDamage)
{
    public DamageTypeSetup PrimaryDamageType { get; set; } = primaryDamage;

    public float? PrimaryDamageValue { get; set; }

    public DamageTypeSetup? SecondaryDamageType { get; set; } = secondaryDamage;

    public float? SecondaryDamageValue { get; set; }

    public List<string> SpEffectDescriptions { get; set; } = [];

    public List<SpEffectDetails> SpEffectTexts { get; set; } = [];
}

public class NameParts
{
    public string Suffix { get; set; } = string.Empty;

    public string Prefix { get; set; } = string.Empty;

    public string Interfix { get; set; } = string.Empty;
}
