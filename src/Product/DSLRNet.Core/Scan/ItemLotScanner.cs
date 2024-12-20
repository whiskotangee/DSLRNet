namespace DSLRNet.Core.Scan;

using DSLRNet.Core.DAL;
using DSLRNet.Core.Data;
using DSLRNet.Core.Extensions;

public class ItemLotScanner(
    ILogger<ItemLotScanner> logger,
    RandomProvider random,
    IOptions<Configuration> configuration,
    IOptions<Settings> settings,
    DataAccess dataAccess,
    BossDropScanner bossDropScanner,
    GameStageEvaluator gameStageEvaluator,
    MSBProvider msbProvider)
{
    private readonly ILogger<ItemLotScanner> logger = logger;
    private readonly RandomProvider random = random;
    private readonly Configuration configuration = configuration.Value;
    private readonly Settings settings = settings.Value;
    private readonly List<ItemLotParam_map> itemLotParam_Map = dataAccess.ItemLotParamMap.GetAll().ToList();
    private readonly List<ItemLotParam_enemy> itemLotParam_Enemy = dataAccess.ItemLotParamEnemy.GetAll().ToList();
    private readonly List<NpcParam> npcParams = dataAccess.NpcParam.GetAll().ToList();

    public Dictionary<ItemLotCategory, List<ItemLotSettings>> ScanAndCreateItemLotSets(Dictionary<ItemLotCategory, HashSet<int>> claimedIds)
    {
        Dictionary<ItemLotCategory, List<ItemLotSettings>> generatedItemLotSettings = [];

        if (!this.settings.ItemLotGeneratorSettings.EnemyLootScannerSettings.Enabled && 
            !this.settings.ItemLotGeneratorSettings.MapLootScannerSettings.Enabled &&
            !this.settings.ItemLotGeneratorSettings.ChestLootScannerSettings.Enabled)
        {
            return generatedItemLotSettings;
        }

        ItemLotSettings remainingMapLots = ItemLotSettings.Create("Assets\\Data\\ItemLots\\Default_Map_Drops.ini", configuration.Itemlots.Categories[1]);
        ItemLotSettings remainingEnemyLots = ItemLotSettings.Create("Assets\\Data\\ItemLots\\Default_Enemy.ini", configuration.Itemlots.Categories[0]);
        ItemLotSettings bossLots = ItemLotSettings.Create("Assets\\Data\\ItemLots\\Default_Map_Drops.ini", configuration.Itemlots.Categories[1]);

        List<EventDropItemLotDetails> eventItemLotDetails = bossDropScanner.ScanEventsForBossDrops();

        generatedItemLotSettings.TryAdd(ItemLotCategory.ItemLot_Enemy, [remainingEnemyLots]);
        generatedItemLotSettings.TryAdd(ItemLotCategory.ItemLot_Map, [remainingMapLots, bossLots]);

        foreach (var mapFile in msbProvider.GetAllMsbs())
        {
            string mapFileName = mapFile.Key;
            MSBE msb = mapFile.Value;

            List<NpcParam> npcs = msb.FilterRelevantNpcs(logger, npcParams, mapFileName);

            Dictionary<GameStage, int> enemiesAdded = SetupEnemyLots(mapFileName, claimedIds[ItemLotCategory.ItemLot_Enemy], npcs, remainingEnemyLots);
            Dictionary<GameStage, int> mapItemsAdded = SetupMapLots(mapFileName, msb, claimedIds[ItemLotCategory.ItemLot_Map], npcs, remainingMapLots, eventItemLotDetails);

            Dictionary<GameStage, int> eventItemsAdded = SetupEventLots(mapFileName, msb, claimedIds[ItemLotCategory.ItemLot_Map], bossLots, eventItemLotDetails);
            logger.LogInformation($"Map {mapFileName} Enemies: {JsonConvert.SerializeObject(enemiesAdded)} Treasures: {JsonConvert.SerializeObject(mapItemsAdded)} BossDrops: {JsonConvert.SerializeObject(eventItemsAdded)}");
        }

        logger.LogInformation($"Map averages: {JsonConvert.SerializeObject(this.mapAverageStage, Formatting.Indented)}");

        return generatedItemLotSettings;
    }

    private Dictionary<GameStage, int> SetupEventLots(string mapFile, MSBE msb, HashSet<int> claimedIds, ItemLotSettings settings, List<EventDropItemLotDetails> lotDetails)
    {
        Dictionary<GameStage, int> assigned = [];

        var withEntityId = lotDetails.Where(d => d.EntityId > 0);

        foreach (EventDropItemLotDetails? details in withEntityId)
        {
            MSBE.Part.Enemy? foundEvent = msb.Parts.Enemies.Where(d => d.EntityID == details.EntityId).FirstOrDefault();
            if (foundEvent != null)
            {
                NpcParam foundNpc = npcParams.Single(d => d.ID == foundEvent.NPCParamID);
                details.NpcId = foundNpc.ID;

                GameStage evaluatedStage = gameStageEvaluator.EvaluateDifficulty(settings, foundNpc, true);

                if (!claimedIds.Contains(details.ItemLotId))
                {
                    settings.GetGameStageConfig(evaluatedStage).ItemLotIds.Add(details.ItemLotId);
                    if (!assigned.TryGetValue(evaluatedStage, out int count))
                    {
                        assigned.Add(evaluatedStage, 0);
                    }

                    assigned[evaluatedStage] += 1;
                }
                else
                {
                    logger.LogInformation($"Boss item lot {details.ItemLotId} already claimed");
                }

                settings.IsForBosses = true;
            }
        }

        return assigned;
    }

    private Dictionary<GameStage, int> SetupEnemyLots(string mapFile, HashSet<int> claimedIds, List<NpcParam> npcParams, ItemLotSettings settings)
    {
        Dictionary<GameStage, int> addedByStage = Enum.GetValues<GameStage>().ToDictionary(d => d, s => 0);

        if (this.settings.ItemLotGeneratorSettings.EnemyLootScannerSettings.Enabled)
        {
            foreach (NpcParam? npc in npcParams.Where(d => d.itemLotId_enemy > 0))
            {
                if (claimedIds.Contains(npc.itemLotId_enemy) || !random.PassesPercentCheck(this.settings.ItemLotGeneratorSettings.EnemyLootScannerSettings.ApplyPercent))
                {
                    continue;
                }

                GameStage assignedGameStage = gameStageEvaluator.EvaluateDifficulty(settings, npc, false);

                settings.GetGameStageConfig(assignedGameStage).ItemLotIds.Add(npc.itemLotId_enemy);
                addedByStage[assignedGameStage] += 1;
            }

        }

        return addedByStage;
    }

    private Dictionary<GameStage, int> SetupMapLots(string name, MSBE msb, HashSet<int> claimedIds, List<NpcParam> npcParams, ItemLotSettings settings, List<EventDropItemLotDetails> lotDetails)
    {
        Dictionary<GameStage, int> addedByStage = Enum.GetValues<GameStage>().ToDictionary(d => d, s => 0);

        if (this.settings.ItemLotGeneratorSettings.ChestLootScannerSettings.Enabled
                || this.settings.ItemLotGeneratorSettings.MapLootScannerSettings.Enabled)
        {
            GameStageConfig gameStage = GetGameStageConfigForMap(name, msb, npcParams, settings, lotDetails);

            List<int> candidateTreasures = [];

            List<MSBE.Event.Treasure> baseFilteredMapTreasures = msb.Events.Treasures
                .Where(d => d.ItemLotID > 0 && !claimedIds.Contains(d.ItemLotID)).ToList();

            if (this.settings.ItemLotGeneratorSettings.ChestLootScannerSettings.Enabled)
            {
                candidateTreasures.AddRange(baseFilteredMapTreasures
                    .Where(d => d.InChest == 1)
                    .Where(d => random.PassesPercentCheck(this.settings.ItemLotGeneratorSettings.ChestLootScannerSettings.ApplyPercent))
                    .Select(s => s.ItemLotID));
            }

            if (this.settings.ItemLotGeneratorSettings.MapLootScannerSettings.Enabled)
            {
                candidateTreasures.AddRange(baseFilteredMapTreasures
                    .Where(d => d.InChest != 1)
                    .Where(d => random.PassesPercentCheck(this.settings.ItemLotGeneratorSettings.MapLootScannerSettings.ApplyPercent))
                    .Select(s => s.ItemLotID));
            }

            if (candidateTreasures.Any())
            {
                candidateTreasures = candidateTreasures
                    .Union(npcParams
                        .Where(d => d.itemLotId_map > 0 && !claimedIds.Contains(d.itemLotId_map))
                        .Select(d => d.itemLotId_map)
                        .Distinct())
                    .ToList();
            }

            candidateTreasures = candidateTreasures.Where(d => IsValidItemLotId(d, ItemLotCategory.ItemLot_Map)).ToList();
            foreach (var treasure in candidateTreasures)
            {
                addedByStage[gameStage.Stage] += candidateTreasures.Count;

                candidateTreasures.ForEach(d => gameStage.ItemLotIds.Add(d));
            }
        }

        return addedByStage;
    }

    Dictionary<string, string> mapAverageStage = [];

    private GameStageConfig GetGameStageConfigForMap(string name, MSBE msb, List<NpcParam> npcs, ItemLotSettings settings, List<EventDropItemLotDetails> lotDetails)
    {
        GameStage gameStage = gameStageEvaluator.EvaluateDifficulty(settings, msb, npcs, name, lotDetails);

        mapAverageStage[name] = gameStage.ToString();

        return settings.GetGameStageConfig(gameStage);
    }

    private bool IsValidItemLotId(int itemLotId, ItemLotCategory itemLotCategory)
    {
        if (itemLotCategory == ItemLotCategory.ItemLot_Map)
        {
            ItemLotParam_map? match = itemLotParam_Map
                .SingleOrDefault(d => itemLotId == d.ID);

            if (match == null)
            {
                return false;
            }

            return match.GetFieldNamesByFilter("lotItemCategory0").Any(d => match.GetValue<int>(d) >= 1);
        }
        else
        {
            ItemLotParam_enemy? match = itemLotParam_Enemy
                .SingleOrDefault(d => itemLotId == d.ID);

            if (match == null)
            {
                return false;
            }

            return match.GetFieldNamesByFilter("lotItemCategory0").Any(d => match.GetValue<int>(d) >= 1);
        }
    }
}
