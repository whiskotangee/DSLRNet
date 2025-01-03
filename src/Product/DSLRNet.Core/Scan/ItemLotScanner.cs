namespace DSLRNet.Core.Scan;

using DSLRNet.Core.Config;
using DSLRNet.Core.DAL;
using DSLRNet.Core.Extensions;
using System.Diagnostics;

public class ItemLotScanner(
    ILogger<ItemLotScanner> logger,
    RandomProvider random,
    IOptions<Configuration> configuration,
    IOptions<Settings> settings,
    DataAccess dataAccess,
    BossDropScannerV2 bossDropScanner,
    GameStageEvaluator gameStageEvaluator,
    ParamEditsRepository paramEditsRepository,
    MSBProvider msbProvider)
{
    private readonly ILogger<ItemLotScanner> logger = logger;
    private readonly RandomProvider random = random;
    private readonly Configuration configuration = configuration.Value;
    private readonly Settings settings = settings.Value;
    private readonly Dictionary<int, ItemLotParam_map> itemLotParam_Map = dataAccess.ItemLotParamMap.GetAll().ToDictionary(k => k.ID);
    private readonly Dictionary<int, ItemLotParam_enemy> itemLotParam_Enemy = dataAccess.ItemLotParamEnemy.GetAll().ToDictionary(k => k.ID);
    private readonly Dictionary<int, NpcParam> npcParams = dataAccess.NpcParam.GetAll().ToDictionary(k => k.ID);
    private readonly IDGenerator itemLotIdGenerator = new IDGenerator()
    {
        StartingID = 1000000000,
        Multiplier = 100,
        IsWrapAround = false
    };

    public Dictionary<ItemLotCategory, List<ItemLotSettings>> LoadScanned(Dictionary<ItemLotCategory, HashSet<int>> claimedIds)
    {
        Dictionary<ItemLotCategory, List<ItemLotSettings>> generatedItemLotSettings = new()
        {
            { ItemLotCategory.ItemLot_Map, [] },
            { ItemLotCategory.ItemLot_Enemy, [] }
        };

        if (!this.settings.ItemLotGeneratorSettings.EnemyLootScannerSettings.Enabled &&
            !this.settings.ItemLotGeneratorSettings.MapLootScannerSettings.Enabled &&
            !this.settings.ItemLotGeneratorSettings.ChestLootScannerSettings.Enabled)
        {
            return generatedItemLotSettings;
        }

        ItemLotSettings? remainingMapLots = this.GetItemLots(
            "Assets\\Data\\ItemLots\\Scanned\\MapDrops.ini",
            configuration.Itemlots.Categories[1],
            settings.ItemLotGeneratorSettings.MapLootScannerSettings,
            claimedIds[ItemLotCategory.ItemLot_Map]);

        ItemLotSettings? chests = this.GetItemLots(
            "Assets\\Data\\ItemLots\\Scanned\\Chests.ini",
            configuration.Itemlots.Categories[1],
            settings.ItemLotGeneratorSettings.ChestLootScannerSettings,
            claimedIds[ItemLotCategory.ItemLot_Map]);

        ItemLotSettings? enemies = this.GetItemLots(
            "Assets\\Data\\ItemLots\\Scanned\\Enemies.ini",
            configuration.Itemlots.Categories[0],
            settings.ItemLotGeneratorSettings.EnemyLootScannerSettings,
            claimedIds[ItemLotCategory.ItemLot_Enemy]);

        ItemLotSettings? bosses = this.GetItemLots(
            "Assets\\Data\\ItemLots\\Scanned\\Bosses.ini",
            configuration.Itemlots.Categories[1],
            new ScannerSettings() { Enabled = true, ApplyPercent = 100 },
            claimedIds[ItemLotCategory.ItemLot_Map]);

        RegisterNpcDuplicates(generatedItemLotSettings);

        if (enemies != null)
        {
            generatedItemLotSettings[ItemLotCategory.ItemLot_Enemy].Add(enemies);
        }

        if (chests != null)
        {
            generatedItemLotSettings[ItemLotCategory.ItemLot_Map].Add(chests);
        }

        if (remainingMapLots != null)
        {
            generatedItemLotSettings[ItemLotCategory.ItemLot_Map].Add(remainingMapLots);
        }

        if (bosses != null)
        {
            generatedItemLotSettings[ItemLotCategory.ItemLot_Map].Add(bosses);
        }

        return generatedItemLotSettings;
    }

    public void ScanAndCreateItemLotSets()
    {
        logger.LogInformation($"Beginning scan for item lots in msb files");

        Dictionary<ItemLotCategory, List<ItemLotSettings>> generatedItemLotSettings = [];

        ItemLotSettings mapLots = ItemLotSettings.Create("Assets\\Data\\ItemLots\\Default_Map_Drops.ini", configuration.Itemlots.Categories[1])
            ?? throw new Exception("Could not read default item lot settings for map drops");
        ItemLotSettings chestsLots = ItemLotSettings.Create("Assets\\Data\\ItemLots\\Default_Map_Drops.ini", configuration.Itemlots.Categories[1])
            ?? throw new Exception("Could not read default item lot settings for chests drops");
        ItemLotSettings enemyLots = ItemLotSettings.Create("Assets\\Data\\ItemLots\\Default_Enemy.ini", configuration.Itemlots.Categories[0])
            ?? throw new Exception("Could not read default item lot settings for enemy drops");
        ItemLotSettings bossLots = ItemLotSettings.Create("Assets\\Data\\ItemLots\\Default_Map_Drops.ini", configuration.Itemlots.Categories[1])
            ?? throw new Exception("Could not read default item lot settings for boss drops");

        List<EventDropItemLotDetails> eventItemLotDetails = bossDropScanner.ScanEventsForBossDrops();

        generatedItemLotSettings.TryAdd(ItemLotCategory.ItemLot_Enemy, [enemyLots]);
        generatedItemLotSettings.TryAdd(ItemLotCategory.ItemLot_Map, [mapLots, bossLots]);

        Dictionary<int, (NpcParam, GameStage)> itemLotToNpcMapping = [];
        Dictionary<int, NpcGameStage> scannedNpcDuplicates = [];

        foreach (KeyValuePair<string, MSBE> mapFile in msbProvider.GetAllMsbs().OrderBy(d => d.Key))
        {
            string mapFileName = mapFile.Key;
            MSBE msb = mapFile.Value;

            List<NpcParam> npcs = msb.FilterRelevantNpcs(logger, npcParams, mapFileName);

            Dictionary<GameStage, int> enemiesAdded = ScanEnemyLots(npcs, enemyLots, itemLotToNpcMapping, scannedNpcDuplicates, eventItemLotDetails);
            Dictionary<GameStage, int> mapItemsAdded = ScanMapLots(mapFileName, msb, npcs, mapLots, eventItemLotDetails, false, true);
            Dictionary<GameStage, int> chestItemsAdded = ScanMapLots(mapFileName, msb, npcs, chestsLots, eventItemLotDetails, true, false);
            Dictionary<GameStage, int> eventItemsAdded = ScanEventLots(msb, bossLots, eventItemLotDetails);

            logger.LogInformation($"Map {mapFileName} Enemies: {JsonConvert.SerializeObject(enemiesAdded)} Treasures: {JsonConvert.SerializeObject(mapItemsAdded)} BossDrops: {JsonConvert.SerializeObject(eventItemsAdded)}");
        }

        logger.LogInformation($"Map averages: {JsonConvert.SerializeObject(this.mapAverageStage, Formatting.Indented)}");

        Directory.CreateDirectory("ScannedLots");

        mapLots.Save("ScannedLots\\MapDrops.ini");
        chestsLots.Save("ScannedLots\\Chests.ini");
        enemyLots.Save("ScannedLots\\Enemies.ini");
        bossLots.Save("ScannedLots\\Bosses.ini");
        File.WriteAllText("ScannedLots\\npcGameStageEvaluations.json", JsonConvert.SerializeObject(scannedNpcDuplicates.Values, Formatting.Indented));
    }

    private ItemLotSettings? GetItemLots(string file, Category category, ScannerSettings scannerSettings, HashSet<int> claimedIds)
    {
        if (!scannerSettings.Enabled)
        {
            return null;
        }

        ItemLotSettings settings = ItemLotSettings.Create(file, category);

        foreach (KeyValuePair<GameStage, GameStageConfig> gameStage in settings.GameStageConfigs)
        {
            int countBefore = gameStage.Value.ItemLotIds.Count;

            gameStage.Value.ItemLotIds =
                gameStage.Value.ItemLotIds
                    .Where(d => !claimedIds.Contains(d) && this.random.PassesPercentCheck(scannerSettings.ApplyPercent))
                    .ToHashSet();
        }

        return settings;
    }

    private void RegisterNpcDuplicates(Dictionary<ItemLotCategory, List<ItemLotSettings>> generatedItemLotSettings)
    {
        // create one for the duplicate enemies, also register the NpcParam edits here
        ItemLotSettings? duplicateEnemies = ItemLotSettings.Create("Assets\\Data\\ItemLots\\Scanned\\Enemies.ini", configuration.Itemlots.Categories[0]);

        List<NpcGameStage> npcDuplicates = JsonConvert.DeserializeObject<List<NpcGameStage>>(File.ReadAllText("Assets\\Data\\ItemLots\\Scanned\\npcGameStageEvaluations.json"))
            .Where(d => d.RequiresNewItemLot)
            .ToList()
            ?? throw new Exception("Duplicate enemies config does not exist");

        foreach (var npcDuplicate in npcDuplicates)
        {
            var existingNpcParam = dataAccess.NpcParam.GetItemById(npcDuplicate.NpcID);
            var gameStage = npcDuplicate.GameStage;
            var itemLot = dataAccess.ItemLotParamEnemy.GetItemById(existingNpcParam.itemLotId_enemy).Clone();

            itemLot.ID = itemLotIdGenerator.GetNext();
            existingNpcParam.itemLotId_enemy = itemLot.ID;

            paramEditsRepository.AddParamEdit(new ParamEdit
            {
                ParamObject = itemLot.GenericParam,
                ParamName = ParamNames.ItemLotParam_enemy,
                Operation = ParamOperation.Create
            });

            paramEditsRepository.AddParamEdit(new ParamEdit
            {
                ParamObject = existingNpcParam.GenericParam,
                ParamName = ParamNames.NpcParam,
                Operation = ParamOperation.Create
            });

            duplicateEnemies.GetGameStageConfig(gameStage).ItemLotIds.Add(itemLot.ID);
        }

        generatedItemLotSettings[ItemLotCategory.ItemLot_Enemy].Add(duplicateEnemies);
    }

    private Dictionary<GameStage, int> ScanEventLots(MSBE msb, ItemLotSettings settings, List<EventDropItemLotDetails> lotDetails)
    {
        Dictionary<GameStage, int> assigned = [];

        IEnumerable<EventDropItemLotDetails> withEntityId = lotDetails.Where(d => d.EntityId > 0);

        foreach (EventDropItemLotDetails? details in withEntityId)
        {
            MSBE.Part.Enemy? foundEvent = msb.Parts.Enemies.Where(d => d.EntityID == details.EntityId).FirstOrDefault();
            if (foundEvent != null)
            {
                NpcParam foundNpc = npcParams[foundEvent.NPCParamID];
                details.NpcId = foundNpc.ID;

                GameStage evaluatedStage = gameStageEvaluator.EvaluateDifficulty(settings, foundNpc, true);

                settings.GetGameStageConfig(evaluatedStage).ItemLotIds.Add(details.ItemLotId);
                if (!assigned.TryGetValue(evaluatedStage, out int count))
                {
                    assigned.Add(evaluatedStage, 0);
                }

                assigned[evaluatedStage] += 1;

                settings.IsForBosses = true;
            }
        }

        return assigned;
    }

    private Dictionary<GameStage, int> ScanEnemyLots(
        List<NpcParam> npcParams, 
        ItemLotSettings settings, 
        Dictionary<int, (NpcParam, GameStage)> enemyItemLotMapping, 
        Dictionary<int, NpcGameStage> scannedNpcDuplicates,
        List<EventDropItemLotDetails> bossDetails)
    {
        Dictionary<GameStage, int> addedByStage = Enum.GetValues<GameStage>().ToDictionary(d => d, s => 0);

        var filteredNpcs = npcParams
            // has an item lot already
            .Where(d => d.itemLotId_enemy > 0)
            // does not end with 00
            .Where(d => d.getSoul > 0)
            // has scaling
            .Where(d => d.spEffectID3 > 0)
            .Where(d => !bossDetails.Any(b => d.ID == b.NpcId))
            .DistinctBy(d => d.ID)
            .ToList();

        logger.LogInformation($"Scanning {filteredNpcs.Count} npcs for item lots");

        foreach (NpcParam? npc in filteredNpcs)
        {
            GameStage assignedGameStage = gameStageEvaluator.EvaluateDifficulty(settings, npc, false);

            if (!enemyItemLotMapping.TryGetValue(npc.itemLotId_enemy, out (NpcParam OriginalNpc, GameStage OriginalGameStage) originalNpc))
            {
                originalNpc = (npc, assignedGameStage);
                enemyItemLotMapping[npc.itemLotId_enemy] = originalNpc;
            }

            var itemLotId = npc.itemLotId_enemy;

            // a single item lot could be shared across many npcs throughout the whole game.
            // i.e. Wolves have NPC ids 40700010, 40700011, 40700012, etc but all share item lot id 407000000
            // We need to create new duplicate item lots for each npc and evaluate the difficulty individually
            bool requiresNewItemLot =
                    npc.ID != originalNpc.OriginalNpc.ID
                    && npc.itemLotId_enemy == originalNpc.OriginalNpc.itemLotId_enemy
                    && assignedGameStage != originalNpc.OriginalGameStage && !scannedNpcDuplicates.ContainsKey(npc.ID);

            scannedNpcDuplicates.TryAdd(
                npc.ID,
                new NpcGameStage
                {
                    NpcID = npc.ID,
                    GameStage = assignedGameStage,
                    RequiresNewItemLot = requiresNewItemLot
                }
            );

            if (!requiresNewItemLot)
            {
                settings.GetGameStageConfig(assignedGameStage).ItemLotIds.Add(itemLotId);
                addedByStage[assignedGameStage] += 1;
            }
        }

        return addedByStage;
    }

    private Dictionary<GameStage, int> ScanMapLots(
        string name,
        MSBE msb,
        List<NpcParam> npcParams,
        ItemLotSettings settings,
        List<EventDropItemLotDetails> lotDetails,
        bool scanChests,
        bool scanMapDrops)
    {
        Dictionary<GameStage, int> addedByStage = Enum.GetValues<GameStage>().ToDictionary(d => d, s => 0);

        if (scanChests || scanMapDrops)
        {
            (GameStageConfig minConfig, GameStageConfig maxConfig) = GetGameStageConfigRangeForMap(name, msb, npcParams, settings, lotDetails);

            List<int> candidateTreasures = [];

            List<MSBE.Event.Treasure> baseFilteredMapTreasures = msb.Events.Treasures
                .Where(d => d.ItemLotID > 0).ToList();

            if (scanChests)
            {
                candidateTreasures.AddRange(baseFilteredMapTreasures
                    .Where(d => d.InChest == 1)
                    .Select(s => s.ItemLotID));
            }

            if (scanMapDrops)
            {
                candidateTreasures.AddRange(baseFilteredMapTreasures
                    .Where(d => d.InChest != 1)
                    .Select(s => s.ItemLotID));
            }

            if (candidateTreasures.Count != 0)
            {
                candidateTreasures = candidateTreasures
                    .Union(npcParams
                        .Where(d => d.itemLotId_map > 0)
                        .Select(d => d.itemLotId_map)
                        .Distinct())
                    .ToList();
            }

            candidateTreasures = candidateTreasures.Where(d => IsValidItemLotId(d, ItemLotCategory.ItemLot_Map)).ToList();
            foreach (int treasure in candidateTreasures)
            {
                GameStageConfig gameStage = this.random.PassesPercentCheck(60) ? maxConfig : minConfig;

                addedByStage[gameStage.Stage] += candidateTreasures.Count;

                gameStage.ItemLotIds.Add(treasure);
            }
        }

        return addedByStage;
    }

    readonly Dictionary<string, string> mapAverageStage = [];

    private (GameStageConfig minConfig, GameStageConfig maxConfig) GetGameStageConfigRangeForMap(string name, MSBE msb, List<NpcParam> npcs, ItemLotSettings settings, List<EventDropItemLotDetails> lotDetails)
    {
        GameStage gameStage = gameStageEvaluator.EvaluateDifficulty(settings, msb, npcs, name, lotDetails);

        mapAverageStage[name] = gameStage.ToString();

        return (settings.GetGameStageConfig((GameStage)Math.Clamp((int)gameStage - 1, (int)GameStage.Early, (int)GameStage.Late)), settings.GetGameStageConfig(gameStage));
    }

    private bool IsValidItemLotId(int itemLotId, ItemLotCategory itemLotCategory)
    {
        if (itemLotCategory == ItemLotCategory.ItemLot_Map)
        {
            if (itemLotParam_Map.TryGetValue(itemLotId, out ItemLotParam_map? match))
            {
                return match.GetFieldNamesByFilter("lotItemCategory0").Any(d => match.GetValue<int>(d) >= 1);
            }

            return false;
        }
        else
        {
            if (itemLotParam_Enemy.TryGetValue(itemLotId, out ItemLotParam_enemy? match))
            {
                return match.GetFieldNamesByFilter("lotItemCategory0").Any(d => match.GetValue<int>(d) >= 1);
            }

            return false;
        }
    }
}
