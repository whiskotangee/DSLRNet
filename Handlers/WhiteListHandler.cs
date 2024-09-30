using DSLRNet.Data;
using DSLRNet.Handlers;
using Microsoft.Extensions.Options;
using Mods.Common;
using Serilog;

namespace DSLRNet.Config;

public enum LootType { Weapon, Armor, Talisman }

public class WhiteListHandler(
    IOptions<WhiteListConfig> whitelistConfig, 
    RandomNumberGetter random,
    DataRepository dataRepository) : BaseHandler(dataRepository)
{
    private readonly WhiteListConfig whitelistConfig = whitelistConfig.Value;
    private readonly RandomNumberGetter random = random;

    public int GetLootByWhiteList(List<int> ids, LootType type)
    {
        List<int> itemIds = [];
        List<int> itemWeights = [];

        foreach (var id in ids)
        {
            if (this.whitelistConfig.Configs.Any(d => d.Id == id))
            {
                var (lootIds, weights) = GetRandomLootAndWeightsFromConfig(this.whitelistConfig.Configs.Single(d => d.Id == id), type);

                if (lootIds.Count != 0)
                {
                    itemIds.AddRange(lootIds);
                    itemWeights.AddRange(weights);
                }
            }
            else
            {
                Log.Logger.Warning($"Id {id} was not found in loot whitelist config");
            }
        }

        if (itemIds.Count == 0)
        {
            var randomConfig = this.random.GetRandomItem(whitelistConfig.Configs);

            var (lootIds, weights) = GetRandomLootAndWeightsFromConfig(randomConfig, type);

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