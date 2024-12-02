namespace DSLRNet.Core.Generators;

public class LoreGenerator(IOptions<LoreConfig> config, RandomProvider random)
{
    private readonly LoreConfig loreConfig = config.Value;
    private readonly RandomProvider random = random;

    public string GenerateDescription(string itemName, bool isArmor)
    {
        string friendName = loreConfig.Names[random.NextInt(0, loreConfig.Names.Count - 1)];
        string enemyName = loreConfig.Names[random.NextInt(0, loreConfig.Names.Count - 1)];
        string locationName = loreConfig.Locations[random.NextInt(0, loreConfig.Locations.Count - 1)];

        (string Prefix, string Interfix, string Postfix) = loreConfig.MadLibsConfig.GetRandomDescription(random, ["name1", "name2", "location", "item"]);

        return (Prefix + " " + Interfix + " " + Postfix)
            .Replace("{name1}", friendName)
            .Replace("{name2}", enemyName)
            .Replace("{item}", itemName)
            .Replace("{location}", locationName);
    }

    public string CreateRandomUniqueName(bool isShield)
    {
        return $"{random.GetRandomItem(loreConfig.UniqueNamesConfig.UniqueNameFirstWord)} " +
            $"{random.GetRandomItem(loreConfig.UniqueNamesConfig.UniqueNameFirstHalf)} " +
            $"{random.GetRandomItem(loreConfig.UniqueNamesConfig.UniqueNameSecondWord)} " +
            $"{(isShield ? random.GetRandomItem(loreConfig.UniqueNamesConfig.UniqueNameSecondHalfShield) : random.GetRandomItem(loreConfig.UniqueNamesConfig.UniqueNameSecondHalf))} ";
    }
}
