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
        string friendName = loreConfig.Names[this.random.NextInt(0, loreConfig.Names.Count)];
        string enemyName = loreConfig.Names[this.random.NextInt(0, loreConfig.Names.Count)];
        string locationName = loreConfig.Locations[this.random.NextInt(0, loreConfig.Locations.Count)];

        string template = loreConfig.MadLibsConfig.GetRandomDescription(random);

        return template.Replace("{name1}", friendName)
                        .Replace("{name2}", enemyName)
                        .Replace("{itemName}", itemName)
                        .Replace("{locationName}", locationName);
    }

    public string CreateRandomUniqueName(string originalName, bool isShield)
    {
        return $"{this.random.GetRandomItem(loreConfig.UniqueNamesConfig.UniqueNameFirstWord)} " +
            $"{this.random.GetRandomItem(loreConfig.UniqueNamesConfig.UniqueNameFirstHalf)} " +
            $"{this.random.GetRandomItem(loreConfig.UniqueNamesConfig.UniqueNameSecondWord)} " +
            $"{(isShield ? this.random.GetRandomItem(loreConfig.UniqueNamesConfig.UniqueNameSecondHalfShield) : this.random.GetRandomItem(loreConfig.UniqueNamesConfig.UniqueNameSecondHalf))} ";
    }
}
