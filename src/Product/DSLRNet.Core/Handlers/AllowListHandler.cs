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
        List<int> itemIds = [];
        List<int> itemWeights = [];

        foreach (int id in ids)
        {
            if (whitelistConfig.Configs.Any(d => d.Id == id))
            {
                (List<int> lootIds, List<int> weights) = GetRandomLootAndWeightsFromConfig(whitelistConfig.Configs.Single(d => d.Id == id), type);

                if (lootIds.Count != 0)
                {
                    itemIds.AddRange(lootIds);
                    itemWeights.AddRange(weights);
                }
            }
        }

        if (itemIds.Count == 0)
        {
            List<AllowListConfigItem> validConfigs = whitelistConfig.Configs.Where(d =>
            {
                (List<int> lootIds, List<int> weights) = GetRandomLootAndWeightsFromConfig(d, type);
                return lootIds.Count > 0;
            }).ToList();

            AllowListConfigItem randomConfig = random.GetRandomItem(validConfigs);

            (List<int> lootIds, List<int> weights) = GetRandomLootAndWeightsFromConfig(randomConfig, type);

            if (lootIds.Count != 0)
            {
                itemIds.AddRange(lootIds);
                itemWeights.AddRange(weights);
            }
        }

        return random.NextWeightedValue(itemIds, itemWeights);
    }

    private static (List<int> lootIds, List<int> weights) GetRandomLootAndWeightsFromConfig(AllowListConfigItem config, LootType lootType)
    {
        List<int> itemIds = [];
        List<int> itemWeights = [];

        switch (lootType)
        {
            case LootType.Weapon:
                itemIds.AddRange(config.LootIds.Weapon);
                itemWeights.AddRange(config.LootWeights.Weapon);
                break;
            case LootType.Armor:
                itemIds.AddRange(config.LootIds.Armor);
                itemWeights.AddRange(config.LootWeights.Armor);
                break;
            case LootType.Talisman:
                itemIds.AddRange(config.LootIds.Talisman);
                itemWeights.AddRange(config.LootWeights.Talisman);
                break;
        }

        if (itemIds.Count > itemWeights.Count)
        {
            itemWeights.AddRange(Enumerable.Range(0, itemIds.Count - itemWeights.Count).Select(s => 10));
        }

        return (itemIds, itemWeights);
    }
}