using DSLRNet.Config;
using Microsoft.Extensions.Options;
using Mods.Common;

namespace DSLRNet.Generators;

public class LoreGenerator(IOptions<LoreConfig> config, RandomNumberGetter random)
{
    private readonly LoreConfig loreConfig = config.Value;
    private readonly RandomNumberGetter random = random;
    
    public string GenerateDescription(string itemName, bool isArmor)
    {
        string friendName = loreConfig.Names[this.random.NextInt(0, loreConfig.Names.Count - 1)];
        string enemyName = loreConfig.Names[this.random.NextInt(0, loreConfig.Names.Count - 1)];
        string locationName = loreConfig.Locations[this.random.NextInt(0, loreConfig.Locations.Count - 1)];

        (string Prefix, string Interfix, string Postfix) = loreConfig.MadLibsConfig.GetRandomDescription(random, ["name1", "name2", "location", "item"]);

        return (Prefix + " " + Interfix + " " + Postfix)
            .Replace("{name1}", friendName)
            .Replace("{name2}", enemyName)
            .Replace("{item}", itemName)
            .Replace("{location}", locationName);
    }

    public string CreateRandomUniqueName(bool isShield)
    {
        return $"{this.random.GetRandomItem(loreConfig.UniqueNamesConfig.UniqueNameFirstWord)} " +
            $"{this.random.GetRandomItem(loreConfig.UniqueNamesConfig.UniqueNameFirstHalf)} " +
            $"{this.random.GetRandomItem(loreConfig.UniqueNamesConfig.UniqueNameSecondWord)} " +
            $"{(isShield ? this.random.GetRandomItem(loreConfig.UniqueNamesConfig.UniqueNameSecondHalfShield) : this.random.GetRandomItem(loreConfig.UniqueNamesConfig.UniqueNameSecondHalf))} ";
    }
}
