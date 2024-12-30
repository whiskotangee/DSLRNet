namespace DSLRNet.Core.Contracts;

public class SpEffectText
{
    public long ID { get; set; }

    public string Description { get; set; } = string.Empty;

    public NameParts? NameParts { get; set; }

    public string Summary { get; set; } = string.Empty;
}
