namespace DSLRNet.Core.Generators;

using DSLRNet.Core.Common;
using DSLRNet.Core.Contracts.Params;
using DSLRNet.Core.DAL;
using System.Collections.Concurrent;

public class ItemLotGenerator : BaseHandler
{
    private ConcurrentDictionary<LootType, List<int>> generatedItemsStats = [];

    private const int ItemLotParamMax = 8;
    private readonly ArmorLootGenerator armorLootGenerator;
    private readonly WeaponLootGenerator weaponLootGenerator;
    private readonly TalismanLootGenerator talismanLootGenerator;
    private readonly RarityHandler rarityHandler;
    private readonly RandomProvider random;
    private readonly ILogger<ItemLotGenerator> logger;
    private readonly IOperationProgressTracker progressTracker;
    private readonly Settings settings;
    private readonly IDGenerator acquisitionFlagIDGenerator;
    private readonly Dictionary<int, ItemLotParam_map> itemLotParam_Map = [];
    private readonly Dictionary<int, ItemLotParam_enemy> itemLotParam_Enemy = [];

    public ItemLotGenerator(
        ArmorLootGenerator armorLootGenerator,
        WeaponLootGenerator weaponLootGenerator,
        TalismanLootGenerator talismanLootGenerator,
        RarityHandler rarityHandler,
        ParamEditsRepository dataRepository,
        RandomProvider random,
        IOptions<Configuration> configuration,
        IOptions<Settings> settingsOptions,
        DataAccess dataAccess,
        ILogger<ItemLotGenerator> logger,
        IOperationProgressTracker progressTracker) : base(dataRepository)
    {
        this.armorLootGenerator = armorLootGenerator;
        this.weaponLootGenerator = weaponLootGenerator;
        this.talismanLootGenerator = talismanLootGenerator;
        this.rarityHandler = rarityHandler;
        this.random = random;
        this.logger = logger;
        this.progressTracker = progressTracker;
        this.settings = settingsOptions.Value;
        this.acquisitionFlagIDGenerator = new IDGenerator()
        {
            StartingID = 1024260000,
            Multiplier = 1,
            AllowedOffsets = [0, 4, 7, 8, 9],
            WrapAroundLimit = 998,
            IsWrapAround = true
        };

        this.ItemLotTemplate = dataAccess.ItemLotBase.GetAll().First();
        this.itemLotParam_Map = dataAccess.ItemLotParamMap.GetAll().ToDictionary(k => k.ID);
        this.itemLotParam_Enemy = dataAccess.ItemLotParamEnemy.GetAll().ToDictionary(k => k.ID);
    }

    private ItemLotBase ItemLotTemplate { get; set; }

    public void CreateItemLots(IEnumerable<ItemLotSettings> itemLotBatches)
    {
        generatedItemsStats = [];

        foreach (ItemLotSettings itemLotEntry in itemLotBatches)
        {
            if (itemLotEntry.Category == ItemLotCategory.ItemLot_Map)
            {
                this.CreateItemLot_Map(itemLotEntry);
            }
            else
            {
                this.CreateItemLot_Enemy(itemLotEntry);
            }
        }

        this.logger.LogInformation($"Count of unique weapons: {this.weaponLootGenerator.uniqueWeaponCounter}");
        this.logger.LogInformation($"Current rarity generation counts {JsonConvert.SerializeObject(rarityHandler.CountByRarity.OrderBy(d => d.Key), Formatting.Indented)}");
        this.logger.LogInformation($"Current edit count: {JsonConvert.SerializeObject(this.GeneratedDataRepository.EditCountsByName(), Formatting.Indented)}");
    }

    private void CreateItemLot_Enemy(ItemLotSettings itemLotSettings)
    {
        this.progressTracker.CurrentStageStepCount =
            itemLotSettings.GameStageConfigs.Values.Sum(d => d.ItemLotIds.Distinct().Count())
            * settings.ItemLotGeneratorSettings.ItemLotsPerBaseEnemyLot;

        this.progressTracker.CurrentStageProgress = 0;

        foreach (GameStageConfig gameStageConfig in itemLotSettings.GameStageConfigs.Values)
        {
            List<int> itemLotIds = [.. gameStageConfig.ItemLotIds];

            itemLotIds = [.. itemLotIds
                .SelectMany(id => this.FindSequentialItemLotIds(
                    itemLotSettings,
                    id,
                    this.settings.ItemLotGeneratorSettings.ItemLotsPerBaseEnemyLot,
                    (i) => this.itemLotParam_Enemy.ContainsKey(i),
                    allowUseSame: true))
                .Distinct()
                .OrderBy(d => d)];

            bool dropGauranteed = this.settings.ItemLotGeneratorSettings.AllLootGauranteed || itemLotSettings.GuaranteedDrop;

            this.logger.LogDebug($"Generating {itemLotIds.Count} item lots for enemy with drop guaranteed: {dropGauranteed}");

            for (int x = 0; x < itemLotIds.Count; x++)
            {
                if (this.GeneratedDataRepository.TryGetParamEdit(ParamNames.ItemLotParam_enemy, itemLotIds[x], out ParamEdit? itemLotParam))
                {
                    this.logger.LogDebug($"Enemy item lot {itemLotIds[x]} has already been processed, skipping");
                    continue;
                }

                ItemLotBase newItemLot;

                if (this.itemLotParam_Enemy.TryGetValue(itemLotIds[x], out ItemLotParam_enemy? existingItemLot))
                {
                    this.logger.LogDebug($"ItemLot {itemLotIds[x]} already exists in data for type {itemLotSettings.Category}, basing template on existing");
                    newItemLot = existingItemLot.CloneToBase();
                    
                }
                else if (this.GeneratedDataRepository.TryGetParamEdit(itemLotSettings.ParamName, itemLotIds[x], out ParamEdit? paramEdit))
                {
                    this.logger.LogWarning($"ItemLot {itemLotIds[x]} already modified for type {itemLotSettings.Category}, ignoring entry");
                    continue;
                }
                else
                {
                    newItemLot = this.ItemLotTemplate.Clone();
                    newItemLot.ID = itemLotIds[x];
                    this.logger.LogDebug($"Enemy itemlot {newItemLot.ID} does not exist in either source, creating new");

                    newItemLot.lotItemBasePoint01 = (ushort)(dropGauranteed ? 0 : 1000);
                    newItemLot.lotItemId01 = 0;
                    newItemLot.lotItemNum01 = 0;
                    newItemLot.lotItemCategory01 = 0;
                }

                newItemLot.Name = string.Empty;

                int offset = Math.Max(newItemLot.GetIndexOfFirstOpenLotItemId(), dropGauranteed ? 1 : 2);

                if (offset < 0)
                {
                    throw new Exception($"No open item spots in item lot {newItemLot.ID}");
                }

                int startingIndex = offset;
                int endingIndex = Math.Clamp(offset + this.settings.ItemLotGeneratorSettings.LootPerItemLot_Enemy, 1, ItemLotParamMax);

                for (int y = startingIndex; y < endingIndex; y++)
                {
                    this.CreateItemLotEntry(
                        itemLotSettings,
                        gameStageConfig,
                        newItemLot,
                        this.settings.ItemLotGeneratorSettings.LootPerItemLot_Enemy,
                        y,
                        (float)itemLotSettings.DropChanceMultiplier,
                        dropGauranteed);
                }

                this.RestrictItemChances(newItemLot, Enumerable.Range(startingIndex, endingIndex - startingIndex).ToArray(), 1000);

                this.GeneratedDataRepository.AddParamEdit(
                    new ParamEdit()
                    {
                        ParamName = itemLotSettings.ParamName,
                        Operation = ParamOperation.Create,
                        ItemText = null,
                        ParamObject = newItemLot.GenericParam
                    });

                this.progressTracker.CurrentStageProgress += 1;
            }
        }
    }

    private void CreateItemLot_Map(ItemLotSettings itemLotSettings)
    {
        GenericParam defaultValue = GenericParam.FromObject(this.ItemLotTemplate.Clone());

        this.progressTracker.CurrentStageStepCount = 
            (itemLotSettings.IsForBosses ? settings.ItemLotGeneratorSettings.LootPerItemLot_Map : settings.ItemLotGeneratorSettings.LootPerItemLot_Bosses) 
            * itemLotSettings.GameStageConfigs.Values.Sum(d => d.ItemLotIds.Count);

        this.progressTracker.CurrentStageProgress = 0;

        foreach (GameStageConfig gameStageConfig in itemLotSettings.GameStageConfigs.Values)
        {
            foreach (int itemLotId in gameStageConfig.ItemLotIds)
            {
                List<int> itemLotIds = this.FindSequentialItemLotIds(
                    itemLotSettings,
                    itemLotId,
                    itemLotSettings.IsForBosses ?
                        this.settings.ItemLotGeneratorSettings.ItemLotsPerBossLot :
                        this.settings.ItemLotGeneratorSettings.ItemLotsPerBaseMapLot,
                    (i) => this.itemLotParam_Map.ContainsKey(i));

                for (int i = 0; i < itemLotIds.Count; i++)
                {
                    ItemLotBase newItemLot = this.ItemLotTemplate.Clone();
                    newItemLot.ID = itemLotIds[i];
                    newItemLot.getItemFlagId = this.FindFlagId(itemLotSettings, newItemLot);

                    if (newItemLot.getItemFlagId <= 0)
                    {
                        newItemLot.getItemFlagId = Convert.ToUInt32(this.acquisitionFlagIDGenerator.GetNext());
                    }

                    newItemLot.Name = string.Empty;

                    int offset = 1;

                    int lootPerLot = itemLotSettings.IsForBosses ? this.settings.ItemLotGeneratorSettings.LootPerItemLot_Bosses : this.settings.ItemLotGeneratorSettings.LootPerItemLot_Map;
                    for (int y = 0; y < lootPerLot; y++)
                    {
                        this.CreateItemLotEntry(
                            itemLotSettings,
                            gameStageConfig,
                            newItemLot,
                            offset + y,
                            lootPerLot,
                            (float)itemLotSettings.DropChanceMultiplier,
                            true);
                    }

                    GenericParam genericParam = GenericParam.FromObject(newItemLot);
                    this.GeneratedDataRepository.AddParamEdit(
                        new ParamEdit()
                        {
                            ParamName = itemLotSettings.ParamName,
                            Operation = ParamOperation.Create,
                            ItemText = null,
                            ParamObject = genericParam
                        });
                }

                this.progressTracker.CurrentStageProgress += 1;
            }
        }
    }

    private List<int> FindSequentialItemLotIds(ItemLotSettings itemLotSettings, int startingId, int goalItemLots, Func<int, bool> existsInDataCheck, bool allowUseSame = false)
    {
        List<int> returnIds = [];

        for (int i = startingId; i < startingId + goalItemLots; i++)
        {
            int currentId = i;

            while (IsIdTaken(currentId) || returnIds.Contains(currentId))
            {
                currentId++;
            }

            if ( IsIdTaken(currentId + 1)
                || returnIds.Contains(currentId + 1))
            {
                this.logger.LogWarning($"Base item lot {startingId} could not find a sequential item lot, tried {currentId} but itemLot {currentId + 1} exists.");
                if (allowUseSame)
                {
                    returnIds.Add(startingId);
                }
                break;
            }

            returnIds.Add(currentId);
        }

        return returnIds;

        bool IsIdTaken(int id)
        {
            bool existsInData = existsInDataCheck(id);
            bool existsInEdits = this.GeneratedDataRepository.ContainsParamEdit(itemLotSettings.ParamName, id);

            return existsInData || existsInEdits;
        }
    }

    private uint FindFlagId(ItemLotSettings itemLotSettings, ItemLotBase baseItem)
    {
        uint flagId = baseItem.getItemFlagId;
        int currentItemLotId = baseItem.ID - 1;

        if (flagId > 0)
        {
            return flagId;
        }

        do
        {
            this.itemLotParam_Map.TryGetValue(currentItemLotId, out ItemLotParam_map? itemLot);
            bool editExists = this.GeneratedDataRepository.TryGetParamEdit(itemLotSettings.ParamName, currentItemLotId, out ParamEdit? paramLot);
            if (itemLot == null && !editExists)
            {
                this.logger.LogDebug($"No entry exists for {currentItemLotId}, giving up search for previous flagId to use with {baseItem.ID} by returning {flagId}");
                break;
            }

            if (itemLot != null && itemLot.getItemFlagId > 0)
            {
                this.logger.LogDebug($"Reusing item flag {itemLot.getItemFlagId} from existing {currentItemLotId} for item lot {baseItem.ID}");
                flagId = itemLot.getItemFlagId;
            }
            else if (editExists && paramLot?.ParamObject.GetValue<long>("getItemFlagId") > 0)
            {
                this.logger.LogDebug($"Reusing item flag {paramLot.ParamObject.GetValue<long>("getItemFlagId")} from param edit {currentItemLotId} for item lot {paramLot.ParamObject.GetValue<long>("ID")}");
                flagId = paramLot.ParamObject.GetValue<uint>("getItemFlagId");
            }

            currentItemLotId -= 1;
        }
        while (flagId <= 0 && currentItemLotId % 10 != 0);

        if (flagId <= 0)
        {
            this.logger.LogWarning($"Could not find flag id for item lot base Id {baseItem.ID}");
        }

        return flagId;
    }

    private (int finalId, int finalCategory) GenerateLoot(ItemLotSettings itemLotSettings, int rarityId)
    {
        LootType itemType = this.random.NextWeightedValue(itemLotSettings.LootWeightsByType);

        int itemCategory;
        int itemId;

        if (LootType.Armor == itemType && this.armorLootGenerator.HasLootTemplates())
        {
            itemId = this.armorLootGenerator.CreateArmor(rarityId);
            itemCategory = 3;
        }
        else if (LootType.Talisman == itemType && this.talismanLootGenerator.HasLootTemplates())
        {
            itemId = this.talismanLootGenerator.CreateTalisman(rarityId);
            itemCategory = 4;
        }
        else
        {
            itemId = this.weaponLootGenerator.CreateWeapon(itemLotSettings, rarityId);
            itemCategory = 2;
        }

        generatedItemsStats.GetOrAdd(itemType, (type) => []).Add(itemId);

        return (itemId, itemCategory);
    }

    private void CreateItemLotEntry(
        ItemLotSettings itemLotSettings,
        GameStageConfig gameStageConfig,
        ItemLotBase itemLot,
        int lootPerItemLot,
        int itemNumber,
        float dropMult,
        bool dropGauranteed = false)
    {
        if (itemNumber >= ItemLotParamMax)
        {
            throw new Exception($"Item lot {itemLot.ID} has too many items");
        }   

        int rarity = this.rarityHandler.ChooseRarityFromIdSet(IntValueRange.CreateFrom(gameStageConfig.AllowedRarities));

        (int finalId, int finalCategory) = this.GenerateLoot(itemLotSettings, rarity);

        itemLot.SetValue($"lotItemId0{itemNumber}", finalId);
        itemLot.SetValue($"lotItemCategory0{itemNumber}", finalCategory);
        itemLot.SetValue($"lotItemNum0{itemNumber}", 1);
        itemLot.SetValue($"lotItemBasePoint0{itemNumber}", this.GetDropChance(dropGauranteed, dropMult, lootPerItemLot));
        itemLot.SetValue($"enableLuck0{itemNumber}", 1);

        this.logger.LogDebug($"Generated item lot entry {itemNumber} for {itemLot.ID} with item {finalId} of category {finalCategory} and rarity {rarity}");
    }

    private float GetDropChance(bool dropGauranteed, float dropMutliplier, int lootPerItemLot)
    {
        float itemDropChance = this.settings.ItemLotGeneratorSettings.GlobalDropChance + Math.Max(0, 6 - lootPerItemLot) * 9;

        return Math.Clamp(dropGauranteed ? 1000 / lootPerItemLot : (int)(itemDropChance * dropMutliplier), 0, 1000);
    }

    private void RestrictItemChances(ItemLotBase itemLot, int[] addedItemIndexes, ushort max = 1000)
    {
        if (addedItemIndexes.Length == 0)
        {
            logger.LogError($"Tried to restrict item chances for item lot {itemLot.ID} but no items were added");
            return;
        }

        List<(string name, ushort basePointValue, ushort quantity, bool isNew)> items = [];

        (string name, ushort basePointValue, ushort quantity, bool isNew) noDropItem = (string.Empty, 0, 0, false);

        // do a pass and figure out what items slots are for what
        var itemChanceFields = itemLot.GetFieldNamesByFilter("lotItemBasePoint0");
        for(int i = 1; i < itemChanceFields.Count; i++)
        {
            (string name, ushort basePointValue, ushort quantity, bool isNew) = 
                ($"lotItemBasePoint0{i}", itemLot.GetValue<ushort>($"lotItemBasePoint0{i}"), itemLot.GetValue<ushort>($"lotItemNum0{i}"), addedItemIndexes.Contains(i));

            if (basePointValue > 0 && itemLot.GetValue<int>($"lotItemId0{i}") == 0 && noDropItem.basePointValue == 0)
            {
                noDropItem = (name, basePointValue, quantity, isNew);
            }
            else
            {
                items.Add((name, basePointValue, quantity, isNew));
            }
        }

        var totalSum = items.Sum(d => d.basePointValue) + noDropItem.basePointValue;
        var excessPoints = totalSum - max;
        if (excessPoints > 0)
        {
            // first, remove from the no drop entry down to zero
            if (noDropItem.basePointValue > 0)
            {
                var coveredAmount = Math.Min(excessPoints, noDropItem.basePointValue);
                if (coveredAmount > 0)
                {
                    itemLot.SetValue(noDropItem.name, Math.Max(noDropItem.basePointValue - coveredAmount, 1));
                    excessPoints -= coveredAmount;
                }
            }

            // if remaining then take it from the new items
            if (excessPoints > 0)
            {
                var itemsToSplitCost = items.Where(d => d.isNew).ToList();
                var splitAmount = (int)Math.Floor((double)excessPoints / itemsToSplitCost.Count);

                if (splitAmount > itemsToSplitCost.Min(d => d.basePointValue))
                {
                    itemsToSplitCost = items.Where(d => d.basePointValue > 0).ToList();
                }

                foreach (var (name, basePointValue, quantity, isNew) in itemsToSplitCost)
                {
                    itemLot.SetValue(name, basePointValue - splitAmount);
                    excessPoints -= splitAmount;
                }
            }
        }
    }
}


