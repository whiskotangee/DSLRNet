namespace DSLRNet.Core.Generators;

using DSLRNet.Core.Common;
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
    private readonly Configuration configuration;
    private readonly Settings settings;
    private readonly CumulativeID itemAcquisitionCumulativeId;
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
        this.configuration = configuration.Value;
        this.settings = settingsOptions.Value;
        this.itemAcquisitionCumulativeId = new CumulativeID(logger)
        {
            IsItemFlagAcquisitionCumulativeID = true,
            UseWrapAround = true
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
        this.progressTracker.CurrentStageStepCount = itemLotSettings.GameStageConfigs.Count * itemLotSettings.GameStageConfigs.Values.Sum(d => d.ItemLotIds.Distinct().Count());
        this.progressTracker.CurrentStageProgress = 0;

        foreach (var gameStageConfig in itemLotSettings.GameStageConfigs.Values)
        {
            List<int> itemLotIds = gameStageConfig.ItemLotIds.ToList();

            itemLotIds.AddRange(itemLotIds
                .SelectMany(id => this.FindSequentialItemLotIds(
                    itemLotSettings,
                    id,
                    this.settings.ItemLotGeneratorSettings.ItemLotsPerBaseEnemyLot,
                    (i) => this.itemLotParam_Enemy.ContainsKey(i))).ToList());

            itemLotIds = itemLotIds.Distinct().ToList();

            bool dropGuaranteed = this.settings.ItemLotGeneratorSettings.AllLootGauranteed || itemLotSettings.GuaranteedDrop;

            this.logger.LogDebug($"Generating {itemLotIds.Count} item lots for enemy with drop guaranteed: {dropGuaranteed}");

            for (int x = 0; x < itemLotIds.Count; x++)
            {
                if (this.GeneratedDataRepository.TryGetParamEdit(ParamNames.ItemLotParam_enemy, itemLotIds[x], out var itemLotParam))
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

                    if (!dropGuaranteed)
                    {
                        newItemLot.lotItemBasePoint01 = 1000;
                        newItemLot.lotItemId01 = 0;
                        newItemLot.lotItemNum01 = 0;
                        newItemLot.lotItemCategory01 = 0;
                    }
                }

                newItemLot.Name = string.Empty;

                int offset = Math.Max(newItemLot.GetIndexOfFirstOpenLotItemId(), dropGuaranteed ? 1 : 2);

                if (offset < 0)
                {
                    throw new Exception("No open item spots in item lot");
                }

                var startingIndex = offset;
                var endingIndex = Math.Clamp(offset + this.settings.ItemLotGeneratorSettings.LootPerItemLot_Enemy, 1, ItemLotParamMax);

                for (int y = startingIndex; y < endingIndex; y++)
                {
                    this.CreateItemLotEntry(
                        itemLotSettings,
                        gameStageConfig,
                        newItemLot,
                        this.settings.ItemLotGeneratorSettings.LootPerItemLot_Enemy,
                        y,
                        (float)itemLotSettings.DropChanceMultiplier,
                        dropGuaranteed);
                }

                this.CalculateNoItemChance(newItemLot);

                GenericParam genericDict = GenericParam.FromObject(newItemLot);

                this.GeneratedDataRepository.AddParamEdit(
                    new ParamEdit()
                    {
                        ParamName = itemLotSettings.ParamName,
                        Operation = ParamOperation.Create,
                        MassEditString = this.CreateMassEdit(genericDict, itemLotSettings.ParamName, newItemLot.ID),
                        MessageText = null,
                        ParamObject = genericDict
                    });

                this.progressTracker.CurrentStageProgress += 1;
            }
        }

        if (itemLotSettings.NpcIds.Count > 0 && itemLotSettings.NpcItemlotids.Count > 0)
        {
            this.GeneratedDataRepository.AddParamEdit(
                new ParamEdit()
                {
                    ParamName = ParamNames.NpcParam,
                    Operation = ParamOperation.MassEdit,
                    MassEditString = this.CreateNpcMassEdit(itemLotSettings, itemLotSettings.NpcIds, itemLotSettings.NpcItemlotids),
                    MessageText = null,
                    ParamObject = null
                });
        }
    }

    private void CreateItemLot_Map(ItemLotSettings itemLotSettings)
    {
        var defaultValue = GenericParam.FromObject(this.ItemLotTemplate.Clone());

        this.progressTracker.CurrentStageStepCount = itemLotSettings.GameStageConfigs.Count * itemLotSettings.GameStageConfigs.Values.Sum(d => d.ItemLotIds.Count);
        this.progressTracker.CurrentStageProgress = 0;

        foreach (var gameStageConfig in itemLotSettings.GameStageConfigs.Values)
        {
            foreach (var itemLotId in gameStageConfig.ItemLotIds)
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
                        newItemLot.getItemFlagId = this.itemAcquisitionCumulativeId.GetNext();
                    }

                    newItemLot.Name = string.Empty;

                    int offset = 1;

                    var lootPerLot = itemLotSettings.IsForBosses ? this.settings.ItemLotGeneratorSettings.LootPerItemLot_Bosses : this.settings.ItemLotGeneratorSettings.LootPerItemLot_Map;
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
                    string itemLotMassEdit = this.CreateMassEdit(genericParam, itemLotSettings.ParamName, newItemLot.ID, defaultValue: defaultValue);
                    this.GeneratedDataRepository.AddParamEdit(
                        new ParamEdit()
                        {
                            ParamName = itemLotSettings.ParamName,
                            Operation = ParamOperation.Create,
                            MassEditString = itemLotMassEdit,
                            MessageText = null,
                            ParamObject = genericParam
                        });
                }

                this.progressTracker.CurrentStageProgress += 1;
            }
        }

        if (itemLotSettings.NpcIds.Any() && itemLotSettings.NpcItemlotids.Any())
        {
            this.GeneratedDataRepository.AddParamEdit(
                new ParamEdit()
                {
                    ParamName = ParamNames.NpcParam,
                    Operation = ParamOperation.MassEdit,
                    MassEditString = this.CreateNpcMassEdit(itemLotSettings, itemLotSettings.NpcIds, itemLotSettings.NpcItemlotids),
                    MessageText = null,
                    ParamObject = null
                });
        }
    }

    private List<int> FindSequentialItemLotIds(ItemLotSettings itemLotSettings, int startingId, int goalItemLots, Func<int, bool> existsInDataCheck)
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
                break;
            }

            returnIds.Add(currentId);
        }

        return returnIds;

        bool IsIdTaken(int id)
        {
            var existsInData = existsInDataCheck(id);
            var existsInEdits = this.GeneratedDataRepository.ContainsParamEdit(itemLotSettings.ParamName, id);

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
            else if (editExists && paramLot.ParamObject.GetValue<long>("getItemFlagId") > 0)
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
        var itemDropChance = this.settings.ItemLotGeneratorSettings.GlobalDropChance + Math.Max(0, 6 - lootPerItemLot) * 9;

        return Math.Clamp(dropGauranteed ? 1000 / lootPerItemLot : (int)(itemDropChance * dropMutliplier), 0, 1000);
    }

    private ushort GetItemLotChanceSum(ItemLotBase itemLotDict, bool includeFirst = false)
    {
        ushort itemLotChanceSum = 0;

        int offset = includeFirst ? 1 : 2;

        for (int x = 0; x < ItemLotParamMax - (offset - 1); x++)
        {
            itemLotChanceSum += itemLotDict.GetValue<ushort>($"lotItemBasePoint0{x + offset}");
        }

        return itemLotChanceSum;
    }

    private void CalculateNoItemChance(ItemLotBase itemLot, ushort baseChance = 1000, ushort fallback = 25)
    {
        ushort finalBaseChance = baseChance;

        ushort otherBasePointTotal = this.GetItemLotChanceSum(itemLot, false);

        finalBaseChance -= otherBasePointTotal;
        finalBaseChance = Math.Clamp(finalBaseChance, fallback, baseChance);

        itemLot.lotItemBasePoint01 = finalBaseChance;
    }

    private string CreateNpcMassEdit(ItemLotSettings itemLotSettings, List<List<int>> npcIds, List<List<int>> npcItemLots)
    {
        if (npcIds.Count == 0 || npcIds.Any(d => d.Count == 0) || npcItemLots.Count == 0 || npcItemLots.Any(d => d.Count == 0))
        {
            return string.Empty;
        }

        string finalString = string.Empty;

        for (int x = 0; x < npcIds.Count - 1; x++)
        {
            List<int> currentIds = npcIds[x];
            List<int> currentItemLots = npcItemLots[x];
            int maxItemLots = currentItemLots.Count - 1;

            for (int y = 0; y < currentIds.Count; y++)
            {
                int assignedLot = currentItemLots[this.random.NextInt(0, maxItemLots)];
                finalString += CreateMassEditLine(ParamNames.NpcParam, currentIds[y], itemLotSettings.NpcParamCategory, assignedLot.ToString());
            }
        }

        return finalString;
    }
}


