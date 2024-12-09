namespace DSLRNet.Core.Generators;

using DSLRNet.Core.Common;

public class ItemLotGenerator : BaseHandler
{
    public string[] ItemlotOutputRealName { get; } = ["ItemLotParam_enemy", "ItemLotParam_map"];
    public List<int> ItemCategories { get; } = [2, 3, 4];

    private const int ItemLotParamMax = 8;
    private readonly ArmorLootGenerator armorLootGenerator;
    private readonly WeaponLootGenerator weaponLootGenerator;
    private readonly TalismanLootGenerator talismanLootGenerator;
    private readonly RarityHandler rarityHandler;
    private readonly RandomProvider random;
    private readonly ILogger<ItemLotGenerator> logger;
    private readonly Configuration configuration;
    private readonly CumulativeID itemAcquisitionCumulativeId;
    private readonly IEnumerable<ItemLotParam_map> itemLotParam_Map = [];
    private readonly IEnumerable<ItemLotParam_enemy> itemLotParam_Enemy = [];

    public ItemLotGenerator(
        ArmorLootGenerator armorLootGenerator,
        WeaponLootGenerator weaponLootGenerator,
        TalismanLootGenerator talismanLootGenerator,
        RarityHandler rarityHandler,
        ParamEditsRepository dataRepository,
        RandomProvider random,
        IOptions<Configuration> configuration,
        IDataSource<ItemLotParam_map> mapDataSource,
        IDataSource<ItemLotParam_enemy> enemyDataSource,
        IDataSource<ItemLotBase> itemLotBaseDataSource,
        ILogger<ItemLotGenerator> logger) : base(dataRepository)
    {
        this.armorLootGenerator = armorLootGenerator;
        this.weaponLootGenerator = weaponLootGenerator;
        this.talismanLootGenerator = talismanLootGenerator;
        this.rarityHandler = rarityHandler;
        this.random = random;
        this.logger = logger;
        this.configuration = configuration.Value;
        this.itemAcquisitionCumulativeId = new CumulativeID(logger as Microsoft.Extensions.Logging.ILogger)
        {
            IsItemFlagAcquisitionCumulativeID = true,
            UseWrapAround = true
        };
        this.ItemLotTemplate = itemLotBaseDataSource.GetAll().First();
        this.itemLotParam_Map = mapDataSource.GetAll();
        this.itemLotParam_Enemy = enemyDataSource.GetAll();
    }

    private ItemLotBase ItemLotTemplate { get; set; }

    public void CreateItemLots(IEnumerable<ItemLotSettings> itemLotQueueEntries)
    {
        foreach (ItemLotSettings itemLotEntry in itemLotQueueEntries)
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

        this.logger.LogInformation($"Current edit count: {JsonConvert.SerializeObject(this.GeneratedDataRepository.EditCountsByName())}");
    }

    public void CreateItemLot_Enemy(ItemLotSettings queueEntry)
    {
        List<int> itemLotIds = queueEntry.GetAllItemLotIdsFromAllTiers();

        bool dropGuaranteed = this.configuration.Settings.ItemLotGeneratorSettings.AllLootGauranteed || queueEntry.GuaranteedDrop;

        if (itemLotIds.Count > 0)
        {
            for (int x = 0; x < itemLotIds.Count; x++)
            {
                if (!queueEntry.BlackListIds.Contains(itemLotIds[x]))
                {
                    ItemLotBase newItemLot;
                    ItemLotParam_enemy? existingItemLot = this.itemLotParam_Enemy.SingleOrDefault(d => d.ID == itemLotIds[x]);

                    if (existingItemLot != null)
                    {
                        this.logger.LogDebug($"ItemLot {itemLotIds[x]} already exists in CSV data for type {queueEntry.Category}, basing template on existing");
                        newItemLot = existingItemLot.CloneToBase();
                    }
                    else if (this.GeneratedDataRepository.TryGetParamEdit(queueEntry.ParamName, itemLotIds[x], out ParamEdit? paramEdit))
                    {
                        this.logger.LogWarning($"ItemLot {itemLotIds[x]} already modified for type {queueEntry.Category}, ignoring entry");
                        continue;
                    }
                    else
                    {
                        newItemLot = this.CreateDefaultItemLotDictionary();
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

                    for (int y = 0; y < this.configuration.Settings.ItemLotGeneratorSettings.LootPerItemLot_Enemy; y++)
                    {
                        this.CreateItemLotEntry(queueEntry, newItemLot, y + offset, itemLotIds[x], (float)queueEntry.DropChanceMultiplier, dropGuaranteed);
                    }

                    this.CalculateNoItemChance(newItemLot);

                    GenericParam genericDict = GenericParam.FromObject(newItemLot);
                    string itemLotMassEdit = this.CreateMassEdit(genericDict, queueEntry.ParamName, newItemLot.ID, [], [], defaultValue: GenericParam.FromObject(this.CreateDefaultItemLotDictionary()));
                    this.GeneratedDataRepository.AddParamEdit(
                        new ParamEdit() 
                        {
                            ParamName = queueEntry.ParamName, 
                            Operation = ParamOperation.Create, 
                            MassEditString = itemLotMassEdit,
                            MessageText = null,
                            ParamObject = genericDict
                        });
                }
                else
                {
                    this.logger.LogDebug($"Itemlot ID {itemLotIds[x]} is blacklisted, skipping...");
                }
            }

            this.GeneratedDataRepository.AddParamEdit(
                new ParamEdit()
                {
                    ParamName = ParamNames.NpcParam,
                    Operation = ParamOperation.MassEdit,
                    MassEditString = this.CreateNpcMassEditString(queueEntry, queueEntry.NpcIds, queueEntry.NpcItemlotids),
                    MessageText = null,
                    ParamObject = null
                });
        }
    }

    public void CreateItemLot_Map(ItemLotSettings queueEntry)
    {
        List<int> baseItemLotIds = queueEntry.GetAllItemLotIdsFromAllTiers();

        // take itemLotId and if it already exists find first empty itemLot
        if (baseItemLotIds.Count > 0)
        {
            for (int x = 0; x < baseItemLotIds.Count; x++)
            {
                if (!queueEntry.BlackListIds.Contains(baseItemLotIds[x]))
                {
                    List<int> itemLotIds = this.FindSequentialItemLotIds(queueEntry, baseItemLotIds[x], this.configuration.Settings.ItemLotGeneratorSettings.ItemLotsPerBaseMapLot);

                    for (int i = 0; i < itemLotIds.Count; i++)
                    {
                        ItemLotBase newItemLot = this.CreateDefaultItemLotDictionary();
                        newItemLot.ID = itemLotIds[i];
                        newItemLot.getItemFlagId = this.FindFlagId(queueEntry, newItemLot);

                        if (newItemLot.getItemFlagId <= 0)
                        {
                            newItemLot.getItemFlagId = this.itemAcquisitionCumulativeId.GetNext();
                        }

                        newItemLot.Name = string.Empty;

                        int offset = 1;

                        for (int y = 0; y < this.configuration.Settings.ItemLotGeneratorSettings.LootPerItemLot_Map; y++)
                        {
                            this.CreateItemLotEntry(queueEntry, newItemLot, offset + y, newItemLot.ID, (float)queueEntry.DropChanceMultiplier, true);
                        }

                        GenericParam genericParam = GenericParam.FromObject(newItemLot);
                        string itemLotMassEdit = this.CreateMassEdit(genericParam, queueEntry.ParamName, newItemLot.ID, [], [], defaultValue: GenericParam.FromObject(this.CreateDefaultItemLotDictionary()));
                        this.GeneratedDataRepository.AddParamEdit(
                            new ParamEdit()
                            {
                                ParamName = queueEntry.ParamName,
                                Operation = ParamOperation.Create,
                                MassEditString = itemLotMassEdit,
                                MessageText = null,
                                ParamObject = genericParam
                            });
                    }
                }
                else
                {
                    this.logger.LogDebug($"Itemlot ID {baseItemLotIds[x]} is blacklisted, skipping...");
                }
            }

            this.GeneratedDataRepository.AddParamEdit(
                new ParamEdit()
                {
                    ParamName = ParamNames.NpcParam,
                    Operation = ParamOperation.MassEdit,
                    MassEditString = this.CreateNpcMassEditString(queueEntry, queueEntry.NpcIds, queueEntry.NpcItemlotids),
                    MessageText = null,
                    ParamObject = null
                });
        }
    }

    private List<int> FindSequentialItemLotIds(ItemLotSettings queueEntry, int startingId, int goalItemLots)
    {
        List<int> returnIds = [];

        for (int i = startingId; i < startingId + goalItemLots; i++)
        {
            int currentId = i;

            while (this.itemLotParam_Map.SingleOrDefault(d => d.ID == currentId) != null
                || this.GeneratedDataRepository.ContainsParamEdit(queueEntry.ParamName, currentId)
                || returnIds.Contains(currentId))
            {
                currentId++;
            }

            if (this.itemLotParam_Map.SingleOrDefault(d => d.ID == currentId + 1) != null
                || this.GeneratedDataRepository.ContainsParamEdit(queueEntry.ParamName, i + 1))
            {
                this.logger.LogDebug($"Map item lot {startingId} could not find a sequential item lot, stopping at {currentId} but itemLot {currentId + 1} exists");
                continue;
            }

            returnIds.Add(currentId);
        }

        return returnIds;
    }

    private int FindFlagId(ItemLotSettings queueEntry, ItemLotBase baseItem)
    {
        int flagId = baseItem.getItemFlagId;
        int currentItemLotId = baseItem.ID - 1;

        if (flagId > 0)
        {
            return flagId;
        }

        do
        {
            ItemLotParam_map? itemLot = this.itemLotParam_Map.SingleOrDefault(d => d.ID == currentItemLotId);
            bool editExists = this.GeneratedDataRepository.TryGetParamEdit(queueEntry.ParamName, currentItemLotId, out ParamEdit? paramLot);
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
                flagId = paramLot.ParamObject.GetValue<int>("getItemFlagId");
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

    public (int FinalId, int FinalCategory) TaskLootGeneratorBasedOnLootType(ItemLotSettings queueEntry, int rarityId = 0)
    {
        LootType itemType = this.random.NextWeightedValue(queueEntry.LootWeightsByType);

        int finalCategory;
        int finalId;

        if (LootType.Armor == itemType && this.armorLootGenerator.HasLootTemplates())
        {
            finalId = this.armorLootGenerator.CreateArmor(rarityId, queueEntry.AllowedLootIds);
            finalCategory = 3;
        }
        else if (LootType.Talisman == itemType && this.talismanLootGenerator.HasLootTemplates())
        {
            finalId = this.talismanLootGenerator.CreateTalisman(rarityId, queueEntry.AllowedLootIds);
            finalCategory = 4;
        }
        else
        {
            finalId = this.weaponLootGenerator.CreateWeapon(rarityId, queueEntry.AllowedLootIds);
            finalCategory = 2;
        }

        return (finalId, finalCategory);
    }

    public ItemLotBase CreateDefaultItemLotDictionary()
    {
        return this.ItemLotTemplate.Clone();
    }

    public void CreateItemLotEntry(ItemLotSettings queueEntry, ItemLotBase itemLotDict, int whichOne = 1, int itemLotId = 0, float dropMult = 1.0f, bool dropGauranteed = false)
    {
        int rarity = this.ChooseRarityFromItemLotIdTierAllowedRarities(queueEntry, itemLotId);

        (int FinalId, int FinalCategory) genResult = this.TaskLootGeneratorBasedOnLootType(queueEntry, rarity);

        this.ApplyItemLotEditingArray(itemLotDict, whichOne, genResult.FinalId, genResult.FinalCategory, (int)(this.GetGlobalDropChance() * dropMult), 1, dropGauranteed);
    }

    private float GetGlobalDropChance()
    {
        return Math.Clamp(this.configuration.Settings.ItemLotGeneratorSettings.GlobalDropChance + Math.Max(0, 6 - this.configuration.Settings.ItemLotGeneratorSettings.LootPerItemLot_Enemy) * 9, 0, 1000);
    }

    public List<int> GetItemLotIdTierAllowedRarities(ItemLotSettings queueEntry, int itemLotId = 0)
    {
        if (this.configuration.Settings.ItemLotGeneratorSettings.ChaosLootEnabled)
        {
            return this.rarityHandler.GetRaritiesWithinRange(this.random.NextInt(1, 10), 0);
        }

        GameStageConfig tier = queueEntry.GetItemLotIdTier(itemLotId);

        return this.rarityHandler.GetRaritiesWithinRange(tier.AllowedRarities.Min(), tier.AllowedRarities.Max());
    }

    public int ChooseRarityFromItemLotIdTierAllowedRarities(ItemLotSettings queueEntry, int itemLotId = 0)
    {
        List<int> rarities = this.GetItemLotIdTierAllowedRarities(queueEntry, itemLotId);
        return this.rarityHandler.ChooseRarityFromIdSet(rarities);
    }

    public int GetItemLotChanceSum(ItemLotBase itemLotDict, bool includeFirst = false)
    {
        int itemLotChanceSum = 0;
        int offset = includeFirst ? 1 : 2;

        for (int x = 0; x < ItemLotParamMax - (offset - 1); x++)
        {
            itemLotChanceSum += itemLotDict.GetValue<int>($"lotItemBasePoint0{x + offset}");
        }

        return itemLotChanceSum;
    }

    public void CalculateNoItemChance(ItemLotBase itemLot, int baseChance = 1000, int fallback = 25)
    {
        int finalBaseChance = baseChance;

        int otherBasePointTotal = this.GetItemLotChanceSum(itemLot, false);

        finalBaseChance -= otherBasePointTotal;
        finalBaseChance = Math.Clamp(finalBaseChance, fallback, baseChance);

        itemLot.lotItemBasePoint01 = finalBaseChance;
    }

    public void ApplyItemLotEditingArray(ItemLotBase itemLot, int itemNumber = 1, int itemId = 1000000, int itemCategory = 2, int itemChance = 50, int itemAmount = 1, bool guaranteedDrop = false)
    {
        itemLot.GenericParam.SetValue($"lotItemId0{itemNumber}", itemId);
        itemLot.GenericParam.SetValue($"lotItemCategory0{itemNumber}", itemCategory);
        itemLot.GenericParam.SetValue($"lotItemNum0{itemNumber}", itemAmount);
        itemLot.GenericParam.SetValue($"lotItemBasePoint0{itemNumber}", guaranteedDrop ? 1000 / this.configuration.Settings.ItemLotGeneratorSettings.LootPerItemLot_Map : itemChance);
        itemLot.GenericParam.SetValue($"enableLuck0{itemNumber}", 1);
    }

    public string CreateNpcMassEditString(ItemLotSettings queueEntry, List<List<int>> npcIds, List<List<int>> npcItemLots)
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
                finalString += CreateMassEditLine(ParamNames.NpcParam, currentIds[y], queueEntry.NpcParamCategory, assignedLot.ToString());
            }
        }

        return finalString;
    }
}


