using DSLRNet.Data;
using DSLRNet.Handlers;
using Microsoft.Extensions.Options;
using Mods.Common;

namespace DSLRNet.Config;

public enum LootType { Weapon, Armor, Talisman }

public class AllowListHandler(
    IOptions<AllowListConfig> allowListConfig, 
    RandomNumberGetter random,
    DataRepository dataRepository) : BaseHandler(dataRepository)
{
    private readonly AllowListConfig whitelistConfig = allowListConfig.Value;
    private readonly RandomNumberGetter random = random;

    public int GetLootByWhiteList(List<int> ids, LootType type)
    {
        List<int> itemIds = [];
        List<int> itemWeights = [];

        foreach (int id in ids)
        {
            if (this.whitelistConfig.Configs.Any(d => d.Id == id))
            {
                (List<int> lootIds, List<int> weights) = GetRandomLootAndWeightsFromConfig(this.whitelistConfig.Configs.Single(d => d.Id == id), type);

                if (lootIds.Count != 0)
                {
                    itemIds.AddRange(lootIds);
                    itemWeights.AddRange(weights);
                }
            }
        }

        if (itemIds.Count == 0)
        {
            List<WhiteListConfigItem> validConfigs = whitelistConfig.Configs.Where(d =>
            {
                (List<int> lootIds, List<int> weights) = GetRandomLootAndWeightsFromConfig(d, type);
                return lootIds.Count > 0;
            }).ToList();

            WhiteListConfigItem randomConfig = this.random.GetRandomItem(validConfigs);

            (List<int> lootIds, List<int> weights) = GetRandomLootAndWeightsFromConfig(randomConfig, type);

            if (lootIds.Count != 0)
            {
                itemIds.AddRange(lootIds);
                itemWeights.AddRange(weights);
            }
        }

        return (int)this.random.NextWeightedValue(itemIds, itemWeights, 1.0f);
    }

    private static (List<int> lootIds, List<int> weights) GetRandomLootAndWeightsFromConfig(WhiteListConfigItem config, LootType lootType)
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