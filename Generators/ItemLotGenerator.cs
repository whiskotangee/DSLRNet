using DSLRNet.Config;
using DSLRNet.Contracts;
using DSLRNet.Data;
using DSLRNet.Generators;
using Microsoft.Extensions.Options;
using Mods.Common;
using Newtonsoft.Json;
using Serilog;

namespace DSLRNet.Handlers;

enum ILEA { ItemId, Category, NumberOf, Chance, ItemAcquisitionFlag }
enum LootCategory { Weapon, Armor, Talisman }

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
    private readonly List<ItemLotBase> itemLotParam_Enemy = [];

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
        this.itemAcquisitionCumulativeId = new CumulativeID()
        {
            IsItemFlagAcquisitionCumulativeID = true,
            UseWrapAround = true
        };
        this.ItemLotTemplate = Data.Csv.LoadCsv<ItemLotBase>("DefaultData\\ER\\CSVs\\ItemLotBase.csv")[0];
        this.itemLotParam_Enemy = Csv.LoadCsv<ItemLotBase>("DefaultData\\ER\\CSVs\\LatestParams\\ItemLotParam_enemy.csv");
        this.itemLotParam_Map = Csv.LoadCsv<ItemLotBase>("DefaultData\\ER\\CSVs\\LatestParams\\ItemLotParam_map.csv");
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

        Log.Logger.Information($"Current edit count: {JsonConvert.SerializeObject(this.GeneratedDataRepository.ParamEditCount())}");
    }

    public void CreateItemLotForOthers(ItemLotQueueEntry queueEntry)
    {
        List<int> itemLotIds = GetAllItemLotIdsFromAllTiers(queueEntry);

        if (itemLotIds.Count > 0)
        {
            for (int x = 0; x < itemLotIds.Count; x++)
            {
                if (!queueEntry.BlackListIds.Contains(itemLotIds[x]))
                {
                    ItemLotBase newItemLot;
                    var existingItemLot = this.itemLotParam_Map.SingleOrDefault(d => d.ID == itemLotIds[x]);

                    if (existingItemLot != null)
                    {
                        Log.Logger.Information($"ItemLot {itemLotIds[x]} already exists in CSV data for type {queueEntry.Category}, basing template on existing");
                        newItemLot = JsonConvert.DeserializeObject<ItemLotBase>(JsonConvert.SerializeObject(existingItemLot));
                    }
                    else if (this.GeneratedDataRepository.TryGetParamEdit(queueEntry.ParamName, itemLotIds[x], out var paramEdit))
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

                    bool dropGuaranteed = this.configuration.Settings.AllLootGauranteed || queueEntry.GuaranteedDrop;
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

                    for (int y = offset; y < this.configuration.Settings.LootPerItemLot_Enemy; y++)
                    {
                        CreateItemLotEntry(queueEntry, newItemLot, y, itemLotIds[x], (float)queueEntry.DropChanceMultiplier, dropGuaranteed);
                    }

                    CalculateNoItemChance(newItemLot);

                    GenericDictionary genericDict = GenericDictionary.FromObject(newItemLot);
                    string itemLotMassEdit = CreateMassEditParamFromParamDictionary(genericDict, queueEntry.ParamName, newItemLot.ID, [], [], defaultValue: GenericDictionary.FromObject(CreateDefaultItemLotDictionary()));
                    this.GeneratedDataRepository.AddParamEdit(queueEntry.ParamName, ParamOperation.Create, itemLotMassEdit, null, genericDict);
                }
                else
                {
                    Log.Logger.Debug($"Itemlot ID {itemLotIds[x]} is blacklisted, skipping...");
                }
            }

            this.GeneratedDataRepository.AddParamEdit(
                ParamNames.NpcParam,
                ParamOperation.MassEdit,
                CreateNpcMassEditString(queueEntry, queueEntry.NpcIds, queueEntry.NpcItemlotids),
                null,
                null);
        }
    }

    public void CreateItemLotForMap(ItemLotQueueEntry queueEntry)
    {
        List<int> baseItemLotIds = GetAllItemLotIdsFromAllTiers(queueEntry);

        // take itemLotId and if it already exists find first empty itemLot
        if (baseItemLotIds.Count > 0)
        {
            for (int x = 0; x < baseItemLotIds.Count; x++)
            {
                if (!queueEntry.BlackListIds.Contains(baseItemLotIds[x]))
                {
                    List<int> itemLotIds = this.FindSequentialItemLotIds(queueEntry, baseItemLotIds[x], this.configuration.Settings.ItemLotsPerBaseLot);

                    for (int i = 0; i < itemLotIds.Count; i++)
                    {
                        ItemLotBase newItemLot = CreateDefaultItemLotDictionary();
                        newItemLot.ID = itemLotIds[i];
                        newItemLot.getItemFlagId = FindFlagId(queueEntry, newItemLot);

                        if (newItemLot.getItemFlagId <= 0)
                        {
                            newItemLot.getItemFlagId = this.itemAcquisitionCumulativeId.GetNext();
                        }

                        newItemLot.Name = string.Empty;

                        int offset = 1;

                        for (int y = 0; y < this.configuration.Settings.LootPerItemLot_Map; y++)
                        {
                            CreateItemLotEntry(queueEntry, newItemLot, offset + y, newItemLot.ID, (float)queueEntry.DropChanceMultiplier, true);
                        }

                        GenericDictionary genericDict = GenericDictionary.FromObject(newItemLot);
                        string itemLotMassEdit = CreateMassEditParamFromParamDictionary(genericDict, queueEntry.ParamName, newItemLot.ID, [], [], defaultValue: GenericDictionary.FromObject(CreateDefaultItemLotDictionary()));
                        this.GeneratedDataRepository.AddParamEdit(queueEntry.ParamName, ParamOperation.Create, itemLotMassEdit, null, genericDict);
                    }
                }
                else
                {
                    Log.Logger.Debug($"Itemlot ID {baseItemLotIds[x]} is blacklisted, skipping...");
                }
            }

            this.GeneratedDataRepository.AddParamEdit(
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

            while (this.itemLotParam_Map.SingleOrDefault(d => d.ID == currentId) != null 
                || this.GeneratedDataRepository.ContainsParamEdit(queueEntry.ParamName, currentId)
                || returnIds.Contains(currentId))
            {
                currentId++;
            }

            if (this.itemLotParam_Map.SingleOrDefault(d => d.ID == (currentId + 1)) != null
                || this.GeneratedDataRepository.ContainsParamEdit(queueEntry.ParamName, i + 1))
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
            var itemLot = this.itemLotParam_Map.SingleOrDefault(d => d.ID == currentItemLotId);
            bool editExists = this.GeneratedDataRepository.TryGetParamEdit(queueEntry.ParamName, currentItemLotId, out ParamEdit? paramLot);
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

    public (int FinalId, int FinalCategory) TaskLootGeneratorBasedOnLootType(ItemLotQueueEntry queueEntry, int rarityId = 0, int lootType = 0)
    {
        LootType itemType = this.random.NextWeightedValue([LootType.Weapon, LootType.Armor, LootType.Talisman], queueEntry.LootTypeWeights, 1.0);

        int finalCategory;
        int finalId;

        if (LootType.Armor == itemType && !this.armorLootGenerator.HasNoLootTemplates())
        {
            finalId = this.armorLootGenerator.CreateArmor(rarityId, queueEntry.Whitelistedlootids);
            finalCategory = 3;
        }
        else if (LootType.Talisman == itemType && !this.talismanLootGenerator.HasNoLootTemplates())
        {
            finalId = this.talismanLootGenerator.CreateTalisman(rarityId, queueEntry.Whitelistedlootids);
            finalCategory = 4;
        }
        else
        {
            finalId = this.weaponLootGenerator.CreateWeapon(rarityId, queueEntry.Whitelistedlootids);
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
        // THIS WILL HANDLE PROPERLY FILLING IN EACH ENTRY IN AN ITEMLOT'S PARAMETERS, FROM LOTID01-LOTID08
        // USE QUEUE DICTIONARY LOOTTYPE WEIGHTS TO DECIDE WHICH KIND OF LOOT TO MAKE IF WE CAN'T FIND A SET OVERRIDETYPE
        // FIND OVERRIDETYPE FROM ID
        int lootType = this.random.NextWeightedValue(ItemCategories, queueEntry.LootTypeWeights, 1.0f);

        // USE ITEMLOTID TO FIND THE PERMITTED RARITIES BASED ON THE TIER OF THE ITEMLOTID WITHIN QUEUEDICTIONARY WE'RE WORKING WITH
        int rarity = ChooseRarityFromItemLotIdTierAllowedRarities(queueEntry, itemLotId);

        // NOW WE'VE GOT THE VALUES WE NEED, TASK THE APPROPRIATE LOOT GENERATOR AND GET A DICTIONARY BACK
        // WE'LL USE TO FILL OUT THE ITEMLOT'S PARAMS - THE GENERATOR SHOULD TAKE CARE OF THE REST, I.E. WEAPON/ARMOR PARAM CREATION AND TEXT EXPORT
        (int FinalId, int FinalCategory) genResult = TaskLootGeneratorBasedOnLootType(queueEntry, rarity, lootType);

        // NOW APPLY THE GENRESULT DICTIONARY TO OUR ITEMLOTPARAMS
        ApplyItemLotEditingArray(itemLotDict, whichOne, genResult.FinalId, genResult.FinalCategory, (int)(GetGlobalDropChance() * dropMult), 1, dropGauranteed);
    }

    private float GetGlobalDropChance()
    {
        return Math.Clamp(this.configuration.Settings.GlobalDropChance + (Math.Max(0, 6 - this.configuration.Settings.LootPerItemLot_Enemy) * 9), 0, 1000);
    }

    public List<int> GetAllItemLotIdsFromAllTiers(ItemLotQueueEntry itemLotEntry)
    {
        return Enum.GetValues(typeof(GameStage))
            .Cast<GameStage>()
            .SelectMany(tier => itemLotEntry.GameStageConfigs
                .Where(d => d.Stage == tier && d.ItemLotIds.Count > 0)
                .SelectMany(s => s.ItemLotIds))
            .Distinct()
            .OrderBy(id => id)
            .ToList();
    }

    public GameStageConfig GetItemLotIdTier(ItemLotQueueEntry queueEntry, int itemLotId = 0)
    {
        return queueEntry.GameStageConfigs.FirstOrDefault(d => d.ItemLotIds.Contains(itemLotId)) ?? queueEntry.GameStageConfigs.First();
    }

    public List<int> GetItemLotIdTierAllowedRarities(ItemLotQueueEntry queueEntry, int itemLotId = 0)
    {
        // IF RARITYCHAOS RETURN ALL AVAILABLE RARITIES
        if (this.configuration.Settings.ChaosLootEnabled)
        {
            return this.rarityHandler.GetRaritiesWithinRange(this.random.NextInt(1, 11), 0);
        }

        // FIRST, WE NEED TO FIND WHICH TIER THE ITEMLOT WE'RE DEALING WITH IS IN
        GameStageConfig tier = GetItemLotIdTier(queueEntry, itemLotId);

        // OTHERWISE ASSUME IT'S TWO IN THE FORMAT ABOVE
        return this.rarityHandler.GetRaritiesWithinRange(tier.AllowedRarities.Min(), tier.AllowedRarities.Max());
    }

    public int ChooseRarityFromItemLotIdTierAllowedRarities(ItemLotQueueEntry queueEntry, int itemLotId = 0)
    {
        List<int> rarities = GetItemLotIdTierAllowedRarities(queueEntry, itemLotId);
        // INCREASE THE CHANCE OF DROPPING THE LOWEST TIER IN THE CURRENT RANGE BY 20%
        return this.rarityHandler.ChooseRarityFromIdSetWithBuiltInWeights(rarities, 1.2f);
    }

    // ITEMDROP CHANCE FUNCTIONS

    public int GetItemLotChanceSum(ItemLotBase itemLotDict, bool includeFirst = false)
    {
        // INITIALISE THE SUM, GET ITEMLOT CHANCE PARAM'S NAME FROM GAMETYPE, AND SETUP AN OFFSET TO X TO DETERMINE HOW MANY
        // OF THE ITEMLOTDICT'S CHANCE VALUES WE'LL BE CHECKING BASED ON WHETHER OR NOT WE'RE INCLUDING lotItemBasePoint01 IN THE SUM
        int itemLotChanceSum = 0;
        string itemLotParamName = GetItemLotItemParamName(ILEA.Chance);
        int offset = includeFirst ? 1 : 2;

        // REMOVE 1 FROM OFFSET TO GET FOR LOOP COUNT - WE WANT TO CHECK ALL EIGHT ITEMLOTBASEPOINTS IF WE'RE INCLUDING THE FIRST
        // AND ONLY SEVEN OF THEM IF WE'RE NOT
        for (int x = 0; x < ItemLotParamMax - (offset - 1); x++)
        {
            // MAKE SURE TO CONVERT WHATEVER WE GET FROM ITEMLOTDICT'S lotItemBasePoint0x VALUE TO INT JUST IN CASE
            itemLotChanceSum += itemLotDict.GetValue<int>(itemLotParamName + (x + offset).ToString());
        }
        return itemLotChanceSum;
    }

    public void CalculateNoItemChance(ItemLotBase itemLot, int baseChance = 1000, int fallback = 25)
    {
        // ASSUMING WE'RE MAKING A NON GUARANTEED ENEMY DROP, START BY STORING BASECHANCE IN A SEPARATE VARIABLE TO EDIT
        int finalBaseChance = baseChance;

        // GET ITEMLOTPARAMNAME FROM GAMETYPE AND ADD "1" TO THE END, AS WE'RE ASSUMING THIS WILL BE THE FIRST ITEMLOTBASEPOINT VALUE
        string itemLotParamName = GetItemLotItemParamName(ILEA.Chance) + "1";

        // GET THE SUM OF THE OTHER ITEMLOTS AFTER 1
        int otherBasePointTotal = GetItemLotChanceSum(itemLot, false);

        // REMOVE THAT SUM FROM FINALBASECHANCE, THEN CLAMP FBC TO THE FALLBACK VALUE AT MINIMUM AND BASECHANCE AT MAXIMUM
        // TO STOP US GETTING ANY NEGATIVE CHANCE VALUES
        finalBaseChance -= otherBasePointTotal;
        finalBaseChance = Math.Clamp(finalBaseChance, fallback, baseChance);

        // FINALLY, SET THE RESULT AS lotItemBasePoint01 OR ITS EQUIVALENT
        itemLot.SetPropertyByName(itemLotParamName, finalBaseChance);
    }

    private string GetItemLotItemParamName(ILEA ilea)
    {
        return this.configuration.Itemlots.ItemlotEditingArray.ItemlotParams[(int)ilea];
    }

    public string GetItemLotCategoriesForItemLotName(ItemLotBase itemLot)
    {
        // GRAB THE ITEMLOT CATEGORY PARAM STRING FROM ITEMLOTPARAMS[GAMETYPE]
        string itemLotCatParam = this.configuration.Itemlots.ItemlotEditingArray.ItemlotParams[0];
        string finalString = "";
        for (int x = 0; x < 8; x++)
        {
            string curParam = itemLotCatParam + (x + 1).ToString();
            // IIRC 1 = GOOD, 2 = WEAPON, 3 = ARMOR, 4 = TALISMAN/OTHER - CHECK ON WINDOWS
            switch (itemLot.GetValue<int>(curParam))
            {
                case 2:
                    finalString += "W ";
                    break;
                case 3:
                    finalString += "A ";
                    break;
                case 4:
                    finalString += "T ";
                    break;
            }
        }
        return finalString;
    }

    public void ApplyItemLotEditingArray(ItemLotBase dictionary, int itemNumber = 1, int itemId = 1000000, int itemCategory = 2, int itemChance = 50, int itemAmount = 1, bool guaranteedDrop = false)
    {
        ItemlotEditingArrayConfig editingArray = this.configuration.Itemlots.ItemlotEditingArray;
        dictionary.SetPropertyByName(editingArray.ItemlotParams[(int)ILEA.ItemId] + itemNumber.ToString(), itemId);
        dictionary.SetPropertyByName(editingArray.ItemlotParams[(int)ILEA.Category] + itemNumber.ToString(), itemCategory);
        dictionary.SetPropertyByName(editingArray.ItemlotParams[(int)ILEA.NumberOf] + itemNumber.ToString(), itemAmount);
        dictionary.SetPropertyByName(editingArray.ItemlotParams[(int)ILEA.Chance] + itemNumber.ToString(), guaranteedDrop ? 1000 / this.configuration.Settings.LootPerItemLot_Map : itemChance);
        dictionary.SetPropertyByName(editingArray.Luck + itemNumber.ToString(), 1);
    }

    public string CreateNpcMassEditString(ItemLotQueueEntry queueEntry, List<List<int>> npcIds = null, List<List<int>> npcItemLots = null)
    {
        // MAKE SURE WE HAVE EVERYTHING WE NEED TO WORK WITH
        if (npcIds == null || npcIds.Count == 0 || npcIds[0].Count == 0 || npcItemLots == null || npcItemLots.Count == 0 || npcItemLots[0].Count == 0)
        {
            return string.Empty;
        }

        string finalString = string.Empty;

        for (int x = 0; x < npcIds.Count - 1; x++)
        {
            List<int> currentIds = npcIds[x];
            List<int> currentItemLots = npcItemLots[x];
            int maxItemLots = currentItemLots.Count - 1;

            // CHOOSE ONE OF THE CURRENTITEMLOTS FOR EACH NPCID
            for (int y = 0; y < currentIds.Count; y++)
            {
                int assignedLot = currentItemLots[new Random().Next(0, maxItemLots + 1)];
                finalString += CreateMassEditLine(ParamNames.NpcParam, currentIds[y], queueEntry.NpcParamCategory, assignedLot.ToString());
            }
        }

        // Log.Logger.Debug(finalString);
        return finalString;
    }
}


