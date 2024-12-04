namespace DSLRNet.Core.Generators;

public class LoreGenerator(IOptions<LoreConfig> config, RandomProvider random)
{
    private readonly LoreConfig loreConfig = config.Value;
    private readonly RandomProvider random = random;

    public string GenerateDescription(string itemName, bool isArmor)
    {
        string friendName = this.loreConfig.Names[this.random.NextInt(0, this.loreConfig.Names.Count - 1)];
        string enemyName = this.loreConfig.Names[this.random.NextInt(0, this.loreConfig.Names.Count - 1)];
        string locationName = this.loreConfig.Locations[this.random.NextInt(0, this.loreConfig.Locations.Count - 1)];

        (string Prefix, string Interfix, string Postfix) = this.loreConfig.MadLibsConfig.GetRandomDescription(this.random, ["name1", "name2", "location", "item"]);

        return (Prefix + " " + Interfix + " " + Postfix)
            .Replace("{name1}", friendName)
            .Replace("{name2}", enemyName)
            .Replace("{item}", itemName)
            .Replace("{location}", locationName);
    }

    public string CreateRandomUniqueName(bool isShield)
    {
        return $"{this.random.GetRandomItem(this.loreConfig.UniqueNamesConfig.UniqueNameFirstWord)} " +
            $"{this.random.GetRandomItem(this.loreConfig.UniqueNamesConfig.UniqueNameFirstHalf)} " +
            $"{this.random.GetRandomItem(this.loreConfig.UniqueNamesConfig.UniqueNameSecondWord)} " +
            $"{(isShield ? this.random.GetRandomItem(this.loreConfig.UniqueNamesConfig.UniqueNameSecondHalfShield) : this.random.GetRandomItem(this.loreConfig.UniqueNamesConfig.UniqueNameSecondHalf))} ";
    }
}
