namespace DSLRNet.Core.Contracts;

public class DamageTypeAddition
{
    public static DamageTypeAddition CreateEmpty()
    {
        return new DamageTypeAddition()
        {
            PrimaryDamageType = new DamageDetails()
            {
                Params = new GenericDictionary
                {
                    Properties = []
                },
                Description = string.Empty,
                Value = 0.0f
            },
            SecondaryDamageType = new DamageDetails()
            {
                Params = new GenericDictionary
                {
                    Properties = []
                },
                Description = string.Empty,
                Value = 0.0f
            },
            SpEffectDescriptions = [],
            SpEffectTexts = []
        };
    }

    public DamageDetails PrimaryDamageType { get; set; }

    public DamageDetails SecondaryDamageType { get; set; }

    public List<string> SpEffectDescriptions { get; set; } = [];

    public List<SpEffectText> SpEffectTexts { get; set; }
}

public class DamageDetails
{
    public GenericDictionary Params { get; set; }

    public string Description { get; set; }

    public float Value { get; set; }
}

public class NameParts
{
    public string Suffix { get; set; } = string.Empty;

    public string Prefix { get; set; } = string.Empty;

    public string Interfix { get; set; } = string.Empty;
}
