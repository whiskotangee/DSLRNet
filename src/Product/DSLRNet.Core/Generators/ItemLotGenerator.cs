using DSLRNet.Core.Common;
using DSLRNet.Core.Config;
using DSLRNet.Core.Contracts;
using DSLRNet.Core.Contracts.Params;
using DSLRNet.Core.Data;
using DSLRNet.Core.Handlers;
using Microsoft.Extensions.Options;

namespace DSLRNet.Core.Generators;

enum ILEA { ItemId, Category, NumberOf, Chance, ItemAcquisitionFlag }

public class ItemLotGenerator : BaseHandler
{
    public string[] ItemlotOutputRealName = ["ItemLotParam_enemy", "ItemLotParam_map"];
    public List<int> ItemCategories = [2, 3, 4];

    private const int ItemLotParamMax = 8;
    private readonly ArmorLootGenerator armorLootGenerator;
    private readonly WeaponLootGenerator weaponLootGenerator;
    private readonly TalismanLootGenerator talismanLootGenerator;
    private readonly RarityHandler rarityHandler;
    private readonly RandomNumberGetter random;
    private readonly Configuration configuration;
    private readonly CumulativeID itemAcquisitionCumulativeId;
    private readonly List<ItemLotBase> itemLotParam_Map = [];

    public ItemLotGenerator(
        ArmorLootGenerator armorLootGenerator,
        WeaponLootGenerator weaponLootGenerator,
        TalismanLootGenerator talismanLootGenerator,
        RarityHandler rarityHandler,
        AllowListHandler allowlistHandler,
        DataRepository dataRepository,
        RandomNumberGetter random,
        IOptions<Configuration> configuration) : base(dataRepository)
    {
        this.armorLootGenerator = armorLootGenerator;
        this.weaponLootGenerator = weaponLootGenerator;
        this.talismanLootGenerator = talismanLootGenerator;
        this.rarityHandler = rarityHandler;
        this.random = random;
        this.configuration = configuration.Value;
        itemAcquisitionCumulativeId = new CumulativeID()
        {
            IsItemFlagAcquisitionCumulativeID = true,
            UseWrapAround = true
        };
        ItemLotTemplate = Csv.LoadCsv<ItemLotBase>("DefaultData\\ER\\CSVs\\ItemLotBase.csv")[0];
        itemLotParam_Map = Csv.LoadCsv<ItemLotBase>("DefaultData\\ER\\CSVs\\LatestParams\\ItemLotParam_map.csv");
    }

    private ItemLotBase ItemLotTemplate { get; set; }

    public void CreateItemLots(IEnumerable<ItemLotQueueEntry> itemLotQueueEntries)
    {
        foreach (ItemLotQueueEntry itemLotEntry in itemLotQueueEntries)
        {
            if (itemLotEntry.Category == ItemLotCategory.ItemLot_Map)
            {
                CreateItemLotForMap(itemLotEntry);
            }
            else
            {
                CreateItemLotForOthers(itemLotEntry);
            }
        }

        Log.Logger.Information($"Current edit count: {JsonConvert.SerializeObject(GeneratedDataRepository.ParamEditCount())}");
    }

    public void CreateItemLotForOthers(ItemLotQueueEntry queueEntry)
    {
        List<int> itemLotIds = queueEntry.GetAllItemLotIdsFromAllTiers();

        if (itemLotIds.Count > 0)
        {
            for (int x = 0; x < itemLotIds.Count; x++)
            {
                if (!queueEntry.BlackListIds.Contains(itemLotIds[x]))
                {
                    ItemLotBase newItemLot;
                    var existingItemLot = itemLotParam_Map.SingleOrDefault(d => d.ID == itemLotIds[x]);

                    if (existingItemLot != null)
                    {
                        Log.Logger.Information($"ItemLot {itemLotIds[x]} already exists in CSV data for type {queueEntry.Category}, basing template on existing");
                        newItemLot = JsonConvert.DeserializeObject<ItemLotBase>(JsonConvert.SerializeObject(existingItemLot));
                    }
                    else if (GeneratedDataRepository.TryGetParamEdit(queueEntry.ParamName, itemLotIds[x], out var paramEdit))
                    {
                        Log.Logger.Warning($"ItemLot {itemLotIds[x]} already modified for type {queueEntry.Category}, ignoring entry");
                        continue;
                    }
                    else
                    {
                        newItemLot = CreateDefaultItemLotDictionary();
                        newItemLot.ID = itemLotIds[x];
                    }

                    newItemLot.Name = string.Empty;

                    bool dropGuaranteed = configuration.Settings.AllLootGauranteed || queueEntry.GuaranteedDrop;
                    if (!dropGuaranteed)
                    {
                        newItemLot.lotItemBasePoint01 = 1000;
                        newItemLot.lotItemId01 = 0;
                    }

                    int offset = Math.Max(newItemLot.GetIndexOfFirstOpenLotItemId(), dropGuaranteed ? 1 : 2);

                    if (offset < 0)
                    {
                        throw new Exception("No open item spots in item lot");
                    }

                    for (int y = offset; y < configuration.Settings.LootPerItemLot_Enemy; y++)
                    {
                        CreateItemLotEntry(queueEntry, newItemLot, y, itemLotIds[x], (float)queueEntry.DropChanceMultiplier, dropGuaranteed);
                    }

                    CalculateNoItemChance(newItemLot);

                    GenericDictionary genericDict = GenericDictionary.FromObject(newItemLot);
                    string itemLotMassEdit = CreateMassEditParamFromParamDictionary(genericDict, queueEntry.ParamName, newItemLot.ID, [], [], defaultValue: GenericDictionary.FromObject(CreateDefaultItemLotDictionary()));
                    GeneratedDataRepository.AddParamEdit(queueEntry.ParamName, ParamOperation.Create, itemLotMassEdit, null, genericDict);
                }
                else
                {
                    Log.Logger.Debug($"Itemlot ID {itemLotIds[x]} is blacklisted, skipping...");
                }
            }

            GeneratedDataRepository.AddParamEdit(
                ParamNames.NpcParam,
                ParamOperation.MassEdit,
                CreateNpcMassEditString(queueEntry, queueEntry.NpcIds, queueEntry.NpcItemlotids),
                null,
                null);
        }
    }

    public void CreateItemLotForMap(ItemLotQueueEntry queueEntry)
    {
        List<int> baseItemLotIds = queueEntry.GetAllItemLotIdsFromAllTiers();

        // take itemLotId and if it already exists find first empty itemLot
        if (baseItemLotIds.Count > 0)
        {
            for (int x = 0; x < baseItemLotIds.Count; x++)
            {
                if (!queueEntry.BlackListIds.Contains(baseItemLotIds[x]))
                {
                    List<int> itemLotIds = FindSequentialItemLotIds(queueEntry, baseItemLotIds[x], configuration.Settings.ItemLotsPerBaseLot);

                    for (int i = 0; i < itemLotIds.Count; i++)
                    {
                        ItemLotBase newItemLot = CreateDefaultItemLotDictionary();
                        newItemLot.ID = itemLotIds[i];
                        newItemLot.getItemFlagId = FindFlagId(queueEntry, newItemLot);

                        if (newItemLot.getItemFlagId <= 0)
                        {
                            newItemLot.getItemFlagId = itemAcquisitionCumulativeId.GetNext();
                        }

                        newItemLot.Name = string.Empty;

                        int offset = 1;

                        for (int y = 0; y < configuration.Settings.LootPerItemLot_Map; y++)
                        {
                            CreateItemLotEntry(queueEntry, newItemLot, offset + y, newItemLot.ID, (float)queueEntry.DropChanceMultiplier, true);
                        }

                        GenericDictionary genericDict = GenericDictionary.FromObject(newItemLot);
                        string itemLotMassEdit = CreateMassEditParamFromParamDictionary(genericDict, queueEntry.ParamName, newItemLot.ID, [], [], defaultValue: GenericDictionary.FromObject(CreateDefaultItemLotDictionary()));
                        GeneratedDataRepository.AddParamEdit(queueEntry.ParamName, ParamOperation.Create, itemLotMassEdit, null, genericDict);
                    }
                }
                else
                {
                    Log.Logger.Debug($"Itemlot ID {baseItemLotIds[x]} is blacklisted, skipping...");
                }
            }

            GeneratedDataRepository.AddParamEdit(
                ParamNames.NpcParam,
                ParamOperation.MassEdit,
                CreateNpcMassEditString(queueEntry, queueEntry.NpcIds, queueEntry.NpcItemlotids),
                null,
                null);
        }
    }

    private List<int> FindSequentialItemLotIds(ItemLotQueueEntry queueEntry, int startingId, int goalItemLots)
    {
        var returnIds = new List<int>();

        for (int i = startingId; i < startingId + goalItemLots; i++)
        {
            var currentId = i;

            while (itemLotParam_Map.SingleOrDefault(d => d.ID == currentId) != null
                || GeneratedDataRepository.ContainsParamEdit(queueEntry.ParamName, currentId)
                || returnIds.Contains(currentId))
            {
                currentId++;
            }

            if (itemLotParam_Map.SingleOrDefault(d => d.ID == currentId + 1) != null
                || GeneratedDataRepository.ContainsParamEdit(queueEntry.ParamName, i + 1))
            {
                Log.Logger.Warning($"Map item lot {startingId} could not find a sequential item lot, stopping at {currentId} but itemLot {currentId + 1} exists");
                continue;
            }

            returnIds.Add(currentId);
        }

        return returnIds;
    }

    private int FindFlagId(ItemLotQueueEntry queueEntry, ItemLotBase baseItem)
    {
        var flagId = baseItem.getItemFlagId;
        var currentItemLotId = baseItem.ID - 1;

        if (flagId > 0)
        {
            return flagId;
        }

        do
        {
            var itemLot = itemLotParam_Map.SingleOrDefault(d => d.ID == currentItemLotId);
            bool editExists = GeneratedDataRepository.TryGetParamEdit(queueEntry.ParamName, currentItemLotId, out ParamEdit? paramLot);
            if (itemLot == null && !editExists)
            {
                Log.Logger.Debug($"No entry exists for {currentItemLotId}, giving up search for previous flagId to use with {baseItem.ID} by returning {flagId}");
                break;
            }

            if (itemLot != null && itemLot.getItemFlagId > 0)
            {
                Log.Logger.Debug($"Reusing item flag {itemLot.getItemFlagId} from existing {currentItemLotId} for item lot {baseItem.ID}");
                flagId = itemLot.getItemFlagId;
            }
            else if (editExists && paramLot.ParamObject.GetValue<long>("getItemFlagId") > 0)
            {
                Log.Logger.Debug($"Reusing item flag {paramLot.ParamObject.GetValue<long>("getItemFlagId")} from param edit {currentItemLotId} for item lot {paramLot.ParamObject.GetValue<long>("ID")}");
                flagId = paramLot.ParamObject.GetValue<int>("getItemFlagId");
            }

            currentItemLotId -= 1;
        }
        while (flagId <= 0 && currentItemLotId % 10 != 0);

        if (flagId <= 0)
        {
            Log.Logger.Warning($"Could not find flag id for item lot base Id {baseItem.ID}");
        }

        return flagId;
    }

    public (int FinalId, int FinalCategory) TaskLootGeneratorBasedOnLootType(ItemLotQueueEntry queueEntry, int rarityId = 0)
    {
        LootType itemType = random.NextWeightedValue([LootType.Weapon, LootType.Armor, LootType.Talisman], queueEntry.LootTypeWeights);

        int finalCategory;
        int finalId;

        if (LootType.Armor == itemType && !armorLootGenerator.HasNoLootTemplates())
        {
            finalId = armorLootGenerator.CreateArmor(rarityId, queueEntry.AllowedLootIds);
            finalCategory = 3;
        }
        else if (LootType.Talisman == itemType && !talismanLootGenerator.HasNoLootTemplates())
        {
            finalId = talismanLootGenerator.CreateTalisman(rarityId, queueEntry.AllowedLootIds);
            finalCategory = 4;
        }
        else
        {
            finalId = weaponLootGenerator.CreateWeapon(rarityId, queueEntry.AllowedLootIds);
            finalCategory = 2;
        }

        return (finalId, finalCategory);
    }

    public ItemLotBase CreateDefaultItemLotDictionary()
    {
        return JsonConvert.DeserializeObject<ItemLotBase>(JsonConvert.SerializeObject(ItemLotTemplate));
    }

    public void CreateItemLotEntry(ItemLotQueueEntry queueEntry, ItemLotBase itemLotDict, int whichOne = 1, int itemLotId = 0, float dropMult = 1.0f, bool dropGauranteed = false)
    {
        int rarity = ChooseRarityFromItemLotIdTierAllowedRarities(queueEntry, itemLotId);

        (int FinalId, int FinalCategory) genResult = TaskLootGeneratorBasedOnLootType(queueEntry, rarity);

        ApplyItemLotEditingArray(itemLotDict, whichOne, genResult.FinalId, genResult.FinalCategory, (int)(GetGlobalDropChance() * dropMult), 1, dropGauranteed);
    }

    private float GetGlobalDropChance()
    {
        return Math.Clamp(configuration.Settings.GlobalDropChance + Math.Max(0, 6 - configuration.Settings.LootPerItemLot_Enemy) * 9, 0, 1000);
    }

    public List<int> GetItemLotIdTierAllowedRarities(ItemLotQueueEntry queueEntry, int itemLotId = 0)
    {
        if (configuration.Settings.ChaosLootEnabled)
        {
            return rarityHandler.GetRaritiesWithinRange(random.NextInt(1, 11), 0);
        }

        GameStageConfig tier = queueEntry.GetItemLotIdTier(itemLotId);

        return rarityHandler.GetRaritiesWithinRange(tier.AllowedRarities.Min(), tier.AllowedRarities.Max());
    }

    public int ChooseRarityFromItemLotIdTierAllowedRarities(ItemLotQueueEntry queueEntry, int itemLotId = 0)
    {
        List<int> rarities = GetItemLotIdTierAllowedRarities(queueEntry, itemLotId);
        return rarityHandler.ChooseRarityFromIdSetWithBuiltInWeights(rarities);
    }

    public int GetItemLotChanceSum(ItemLotBase itemLotDict, bool includeFirst = false)
    {
        int itemLotChanceSum = 0;
        string itemLotParamName = GetItemLotItemParamName(ILEA.Chance);
        int offset = includeFirst ? 1 : 2;

        for (int x = 0; x < ItemLotParamMax - (offset - 1); x++)
        {
            itemLotChanceSum += itemLotDict.GetValue<int>(itemLotParamName + (x + offset).ToString());
        }

        return itemLotChanceSum;
    }

    public void CalculateNoItemChance(ItemLotBase itemLot, int baseChance = 1000, int fallback = 25)
    {
        int finalBaseChance = baseChance;

        string itemLotParamName = GetItemLotItemParamName(ILEA.Chance) + "1";

        int otherBasePointTotal = GetItemLotChanceSum(itemLot, false);

        finalBaseChance -= otherBasePointTotal;
        finalBaseChance = Math.Clamp(finalBaseChance, fallback, baseChance);

        itemLot.SetPropertyByName(itemLotParamName, finalBaseChance);
    }

    private string GetItemLotItemParamName(ILEA ilea)
    {
        return configuration.Itemlots.ItemlotEditingArray.ItemlotParams[(int)ilea];
    }

    public void ApplyItemLotEditingArray(ItemLotBase dictionary, int itemNumber = 1, int itemId = 1000000, int itemCategory = 2, int itemChance = 50, int itemAmount = 1, bool guaranteedDrop = false)
    {
        ItemlotEditingArrayConfig editingArray = configuration.Itemlots.ItemlotEditingArray;
        dictionary.SetPropertyByName(editingArray.ItemlotParams[(int)ILEA.ItemId] + itemNumber.ToString(), itemId);
        dictionary.SetPropertyByName(editingArray.ItemlotParams[(int)ILEA.Category] + itemNumber.ToString(), itemCategory);
        dictionary.SetPropertyByName(editingArray.ItemlotParams[(int)ILEA.NumberOf] + itemNumber.ToString(), itemAmount);
        dictionary.SetPropertyByName(editingArray.ItemlotParams[(int)ILEA.Chance] + itemNumber.ToString(), guaranteedDrop ? 1000 / configuration.Settings.LootPerItemLot_Map : itemChance);
        dictionary.SetPropertyByName(editingArray.Luck + itemNumber.ToString(), 1);
    }

    public string CreateNpcMassEditString(ItemLotQueueEntry queueEntry, List<List<int>> npcIds, List<List<int>> npcItemLots)
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
                int assignedLot = currentItemLots[this.random.NextInt(0, maxItemLots + 1)];
                finalString += CreateMassEditLine(ParamNames.NpcParam, currentIds[y], queueEntry.NpcParamCategory, assignedLot.ToString());
            }
        }

        return finalString;
    }
}


