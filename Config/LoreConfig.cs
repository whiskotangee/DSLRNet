using Mods.Common;

namespace DSLRNet.Config;

public class LoreConfig
{
    public List<string> Names { get; set; }
    public List<string> Locations { get; set; }

    public LoreTemplates MadLibsConfig { get; set; }

    public UniqueNameConfig UniqueNamesConfig { get; set; }
}

public class LoreTemplates
{
    public string GetRandomDescription(RandomNumberGetter random)
    {
        return random.GetRandomItem(Prefixes) + " " + random.GetRandomItem(Interfixes) + " " + random.GetRandomItem(PostFixes);
    }

    public List<string> Prefixes { get; set; }

    public List<string> Interfixes { get; set; }

    public List<string> PostFixes { get; set; }
}
public class UniqueNameConfig
{
    public List<string> UniqueNameFirstHalf { get; set; }
    public List<string> UniqueNameSecondHalf { get; set; }
    public List<string> UniqueNameSecondHalfShield { get; set; }
    public List<string> UniqueNameFirstWord { get; set; }
    public List<string> UniqueNameSecondWord { get; set; }
}
