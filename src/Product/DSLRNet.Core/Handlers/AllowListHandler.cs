namespace DSLRNet.Core.Handlers;

public enum LootType { Weapon, Armor, Talisman }

public class AllowListHandler(
    IOptions<AllowListConfig> allowListConfig,
    RandomProvider random,
    ParamEditsRepository dataRepository) : BaseHandler(dataRepository)
{
    private readonly AllowListConfig whitelistConfig = allowListConfig.Value;
    private readonly RandomProvider random = random;

    public int GetLootByAllowList(List<int> ids, LootType type)
    {
        List<WeightedValue<int>> weightedValues = [];

        foreach (int id in ids)
        {
            if (this.whitelistConfig.Configs.Any(d => d.Id == id))
            {
                weightedValues.AddRange(GetRandomLootAndWeights(this.whitelistConfig.Configs.Single(d => d.Id == id), type));
            }
        }

        if (weightedValues.Count == 0)
        {
            List<AllowListConfigItem> validConfigs = this.whitelistConfig.Configs.Where(d =>
            {
                var weights = GetRandomLootAndWeights(d, type);
                return weights.Count > 0;
            }).ToList();
            
            AllowListConfigItem randomConfig = this.random.GetRandomItem(validConfigs);

            weightedValues.AddRange(GetRandomLootAndWeights(randomConfig, type));
        }

        return this.random.NextWeightedValue(weightedValues);
    }

    private static List<WeightedValue<int>> GetRandomLootAndWeights(AllowListConfigItem config, LootType lootType)
    {
        List<WeightedValue<int>> weightedValues = [];

        switch (lootType)
        {
            case LootType.Weapon:
                weightedValues = WeightedValue<int>.CreateFromLists(config.LootIds.Weapon, config.LootWeights.Weapon);
                break;
            case LootType.Armor:
                weightedValues = WeightedValue<int>.CreateFromLists(config.LootIds.Armor, config.LootWeights.Armor);
                break;
            case LootType.Talisman:
                weightedValues = WeightedValue<int>.CreateFromLists(config.LootIds.Talisman, config.LootWeights.Talisman);
                break;
        }

        return weightedValues;
    }
}