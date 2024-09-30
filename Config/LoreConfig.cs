namespace DSLRNet.Config;

public class LoreConfig
{
    public List<string> Names { get; set; }
    public List<string> Locations { get; set; }
    public List<string> Templates { get; set; }

    public UniqueNameConfig UniqueNamesConfig { get; set; }
}

public class UniqueNameConfig
{
    public List<string> UniqueNameFirstHalf { get; set; }
    public List<string> UniqueNameSecondHalf { get; set; }
    public List<string> UniqueNameSecondHalfShield { get; set; }
    public List<string> UniqueNameFirstWord { get; set; }
    public List<string> UniqueNameSecondWord { get; set; }
}
