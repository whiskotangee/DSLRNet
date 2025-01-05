namespace DSLRNet.Core.Scan;

using DSLRNet.Core.Config;
using DSLRNet.Core.DAL;
using DSLRNet.Core.Extensions;
using System.Diagnostics;

public class ItemLotScanner(
    ILogger<ItemLotScanner> logger,
    RandomProvider random,
    IOptions<Configuration> configuration,
    DataAccess dataAccess,
    BossDropScannerV2 bossDropScanner,
    DifficultyEvaluator difficultyEvaluator,
    MSBProvider msbProvider)
{
    private readonly ILogger<ItemLotScanner> logger = logger;
    private readonly RandomProvider random = random;
    private readonly Configuration configuration = configuration.Value;
    private readonly Dictionary<int, ItemLotParam_map> itemLotParam_Map = dataAccess.ItemLotParamMap.GetAll().ToDictionary(k => k.ID);
    private readonly Dictionary<int, ItemLotParam_enemy> itemLotParam_Enemy = dataAccess.ItemLotParamEnemy.GetAll().ToDictionary(k => k.ID);
    private readonly Dictionary<int, NpcParam> npcParams = dataAccess.NpcParam.GetAll().ToDictionary(k => k.ID);

    public void ScanAndCreateItemLotSets()
    {
        logger.LogInformation($"Beginning scan for item lots in msb files");

        Dictionary<ItemLotCategory, List<ItemLotSettings>> generatedItemLotSettings = [];

        var startingId = 9990;

        ItemLotSettings mapLots = ItemLotSettings.Create("Assets\\Data\\ItemLots\\Default_Map_Drops.ini", configuration.Itemlots.Categories[1])
            ?? throw new Exception("Could not read default item lot settings for map drops");
        mapLots.ID = startingId++;
        mapLots.Realname = "Map Drops";

        ItemLotSettings chestsLots = ItemLotSettings.Create("Assets\\Data\\ItemLots\\Default_Map_Drops.ini", configuration.Itemlots.Categories[1])
            ?? throw new Exception("Could not read default item lot settings for chests drops");
        chestsLots.ID = startingId++;
        chestsLots.Realname = "Opened Chests Drops";

        ItemLotSettings enemyLots = ItemLotSettings.Create("Assets\\Data\\ItemLots\\Default_Enemy.ini", configuration.Itemlots.Categories[0])
            ?? throw new Exception("Could not read default item lot settings for enemy drops");
        enemyLots.ID = startingId++;
        enemyLots.Realname = "Enemy Drops";

        ItemLotSettings bossLots = ItemLotSettings.Create("Assets\\Data\\ItemLots\\Default_Map_Drops.ini", configuration.Itemlots.Categories[1])
            ?? throw new Exception("Could not read default item lot settings for boss drops");
        bossLots.ID = startingId++;
        bossLots.Realname = "Boss Drops";

        List<EventDropItemLotDetails> bossDetails = bossDropScanner.ScanEventsForBossDrops();

        var allMSBs = msbProvider.GetAllMsbs().OrderBy(d => d.Key);

        generatedItemLotSettings.TryAdd(ItemLotCategory.ItemLot_Enemy, [enemyLots]);
        generatedItemLotSettings.TryAdd(ItemLotCategory.ItemLot_Map, [mapLots, bossLots]);

        Dictionary<int, (NpcParam, GameStage)> itemLotToNpcMapping = [];
        Dictionary<int, NpcGameStage> scannedNpcDuplicates = [];

        foreach (KeyValuePair<string, MSBE> mapFile in allMSBs)
        {
            string mapFileName = mapFile.Key;
            MSBE msb = mapFile.Value;

            List<NpcParam> npcs = msb.FilterRelevantNpcs(logger, npcParams, mapFileName);

            Dictionary<GameStage, int> enemiesAdded = ScanEnemyLots(npcs, enemyLots, itemLotToNpcMapping, scannedNpcDuplicates, bossDetails);
            Dictionary<GameStage, int> mapItemsAdded = ScanMapLots(mapFileName, msb, npcs, mapLots, bossDetails, false, true);
            Dictionary<GameStage, int> chestItemsAdded = ScanMapLots(mapFileName, msb, npcs, chestsLots, bossDetails, true, false);

            var duplicates = enemyLots.GameStageConfigs.SelectMany(d => d.Value.ItemLotIds).GroupBy(d => d).Where(c => c.Count() > 1).ToList();

            if (duplicates.Any())
            {
                logger.LogError($"{duplicates.Count()} duplicated enemy itemlot entries found");
            }

            logger.LogInformation($"Map {mapFileName} Enemies: {JsonConvert.SerializeObject(enemiesAdded)} Treasures: {JsonConvert.SerializeObject(mapItemsAdded)}");
        }

        logger.LogInformation($"Map averages: {JsonConvert.SerializeObject(this.mapAverageStage, Formatting.Indented)}");

        difficultyEvaluator.AssignBossGameStages(allMSBs.ToDictionary(), bossLots, bossDetails);

        foreach (var gameStage in bossLots.GameStageConfigs)
        {
            var bosses = bossDetails.Where(d => d.FinalGameStage == gameStage.Key);
            gameStage.Value.ItemLotIds.UnionWith(bosses.Select(d => d.ItemLotId));
            logger.LogInformation($"{gameStage.Key} has {bosses.Count()} bosses");
            logger.LogInformation($"Bosses for {gameStage.Key}: {string.Join(Environment.NewLine, bosses.Select(d => d.NpcParam?.Name))}");
        }

        bossLots.IsForBosses = true;

        var savePath = Path.Combine("Assets", "Data", "ItemLots", "Scanned");
        Directory.CreateDirectory(savePath);

        mapLots.Save(Path.Combine(savePath, "MapDrops.ini"));
        chestsLots.Save(Path.Combine(savePath, "Chests.ini"));
        enemyLots.Save(Path.Combine(savePath, "Enemies.ini"));
        bossLots.Save(Path.Combine(savePath, "Bosses.ini"));
        File.WriteAllText(Path.Combine(savePath, "npcGameStageEvaluations.json"), JsonConvert.SerializeObject(scannedNpcDuplicates.Values, Formatting.Indented));
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
            GameStage assignedGameStage = difficultyEvaluator.EvaluateDifficultyByScalingSpEffect(settings, npc);

            if (!enemyItemLotMapping.TryGetValue(npc.itemLotId_enemy, out (NpcParam OriginalNpc, GameStage OriginalGameStage) originalNpc))
            {
                originalNpc = (npc, assignedGameStage);
                enemyItemLotMapping[npc.itemLotId_enemy] = originalNpc;
            }

            var itemLotId = npc.itemLotId_enemy;

            // a single item lot could be shared across many npcs throughout the whole game.
            // i.e. Wolves have NPC ids 40700010, 40700011, 40700012, etc but all share item lot id 407000000
            // We need to create new duplicate item lots for each npc and evaluate the difficulty individually
            bool hasBeenSeenBefore = scannedNpcDuplicates.ContainsKey(npc.ID);

            bool requiresNewItemLot =
                    npc.ID != originalNpc.OriginalNpc.ID
                    && npc.itemLotId_enemy == originalNpc.OriginalNpc.itemLotId_enemy
                    && assignedGameStage != originalNpc.OriginalGameStage && !hasBeenSeenBefore;

            scannedNpcDuplicates.TryAdd(
                npc.ID,
                new NpcGameStage
                {
                    NpcID = npc.ID,
                    GameStage = assignedGameStage,
                    RequiresNewItemLot = requiresNewItemLot
                }
            );

            if (!requiresNewItemLot && !hasBeenSeenBefore)
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
        GameStage gameStage = difficultyEvaluator.EvaluateDifficulty(settings, msb, npcs, name, lotDetails);

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
