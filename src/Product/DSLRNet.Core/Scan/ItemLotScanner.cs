namespace DSLRNet.Core.Scan;

using DSLRNet.Core.Data;
using DSLRNet.Core.Extensions;
using System.Collections.Concurrent;
using System.Diagnostics;
using static SoulsFormats.EMEVD.Instruction;

public class ItemLotScanner(
    ILogger<ItemLotScanner> logger,
    RandomProvider random,
    IOptions<Configuration> configuration,
    IDataSource<ItemLotParam_map> mapItemLotSource,
    IDataSource<ItemLotParam_enemy> enemyItemLotSource,
    IDataSource<NpcParam> npcParamSource,
    IDataSource<SpEffectParam> spEffectParam,
    IDataSource<RaritySetup> raritySetup,
    BossDropScanner bossDropScanner,
    GameStageEvaluator gameStageEvaluator)
{
    private readonly ILogger<ItemLotScanner> logger = logger;
    private readonly RandomProvider random = random;
    private readonly Configuration configuration = configuration.Value;
    private readonly List<ItemLotParam_map> itemLotParam_Map = mapItemLotSource.GetAll().ToList();
    private readonly List<ItemLotParam_enemy> itemLotParam_Enemy = enemyItemLotSource.GetAll().ToList();
    private readonly List<NpcParam> npcParams = npcParamSource.GetAll().ToList();

    public async Task<Dictionary<ItemLotCategory, ItemLotSettings>> ScanAndCreateItemLotSetsAsync(Dictionary<ItemLotCategory, HashSet<int>> claimedIds)
    {
        string modDir = $"{configuration.Settings.DeployPath}\\map\\mapstudio";

        Dictionary<ItemLotCategory, ItemLotSettings> generatedItemLotSettings = [];

        ItemLotSettings remainingMapLots = ItemLotSettings.Create("Assets\\Data\\ItemLots\\Default_Map_And_Events.ini", configuration.Itemlots.Categories[1]);
        ItemLotSettings remainingEnemyLots = ItemLotSettings.Create("Assets\\Data\\ItemLots\\Default_Enemy.ini", configuration.Itemlots.Categories[0]);

        List<EventDropItemLotDetails> eventItemLotDetails = bossDropScanner.ScanEventsForBossDrops();

        generatedItemLotSettings.TryAdd(ItemLotCategory.ItemLot_Enemy, remainingEnemyLots);
        generatedItemLotSettings.TryAdd(ItemLotCategory.ItemLot_Map, remainingMapLots);

        List<int> globalNpcIds = [];

        List<string> mapStudioFiles = Directory.GetFiles(Path.Combine(this.configuration.Settings.DeployPath, "map", "mapstudio"), "*.msb.dcx").ToList();
        List<string> additionalMapFiles = Directory.GetFiles(Path.Combine(this.configuration.Settings.GamePath, "map", "mapstudio"), "*.msb.dcx")
            .Where(d => !mapStudioFiles.Any(s => Path.GetFileName(s) == Path.GetFileName(d)))
            .ToList();

        mapStudioFiles.AddRange(additionalMapFiles);

        foreach (string mapFile in mapStudioFiles)
        {
            MSBE msb = MSBE.Read(mapFile);

            string mapFileName = Path.GetFileName(mapFile);

            List<NpcParam> npcs = msb.FilterRelevantNpcs(logger, npcParams, mapFile);

            Dictionary<GameStage, int> enemiesAdded = SetupEnemyLots(mapFileName, claimedIds[ItemLotCategory.ItemLot_Enemy], npcs, remainingEnemyLots);
            Dictionary<GameStage, int> mapItemsAdded = SetupMapLots(mapFileName, msb, claimedIds[ItemLotCategory.ItemLot_Map], npcs, remainingMapLots, eventItemLotDetails);

            Dictionary<GameStage, int> eventItemsAdded = SetupEventLots(mapFileName, msb, claimedIds[ItemLotCategory.ItemLot_Map], remainingMapLots, eventItemLotDetails);
            logger.LogInformation($"Map {mapFileName} Enemies: {JsonConvert.SerializeObject(enemiesAdded)} Treasures: {JsonConvert.SerializeObject(mapItemsAdded)}");
        }

        logger.LogInformation($"Map averages: {JsonConvert.SerializeObject(this.mapAverageStage, Formatting.Indented)}");

        return generatedItemLotSettings;
    }

    private Dictionary<GameStage, int> SetupEventLots(string mapFile, MSBE msb, HashSet<int> claimedIds, ItemLotSettings settings, List<EventDropItemLotDetails> lotDetails)
    {
        Dictionary<GameStage, int> assigned = [];

        foreach (EventDropItemLotDetails? details in lotDetails.Where(d => d.EntityId > 0))
        {
            MSBE.Part.Enemy? foundEvent = msb.Parts.Enemies.Where(d => d.EntityID == details.EntityId).FirstOrDefault();
            if (foundEvent != null)
            {
                NpcParam foundNpc = npcParams.Single(d => d.ID == foundEvent.NPCParamID);

                GameStage evaluatedStage = gameStageEvaluator.EvaluateDifficulty(settings, foundNpc);

                settings.GetGameStageConfig(evaluatedStage).ItemLotIds.Add(details.ItemLotId);
                if (!assigned.TryGetValue(evaluatedStage, out int count))
                {
                    assigned.Add(evaluatedStage, 0);
                }

                assigned[evaluatedStage] += 1;
            }
        }

        return assigned;
    }

    private Dictionary<GameStage, int> SetupEnemyLots(string mapFile, HashSet<int> claimedIds, List<NpcParam> npcParams, ItemLotSettings settings)
    {
        Dictionary<GameStage, int> addedByStage = Enum.GetValues<GameStage>().ToDictionary(d => d, s => 0);

        if (configuration.Settings.ItemLotGeneratorSettings.EnemyLootScannerSettings.Enabled)
        {
            foreach (NpcParam? npc in npcParams.Where(d => d.itemLotId_enemy > 0))
            {
                if (!IsValidItemLotId(npc.itemLotId_enemy, ItemLotCategory.ItemLot_Enemy))
                {
                    logger.LogInformation($"Skipping item lot Id as it does not give any items {npc.itemLotId_enemy}");
                    continue;
                }

                if (claimedIds.Contains(npc.itemLotId_enemy) ||
                    !random.PassesPercentCheck(configuration.Settings.ItemLotGeneratorSettings.EnemyLootScannerSettings.ApplyPercent))
                {
                    continue;
                }

                GameStage assignedGameStage = gameStageEvaluator.EvaluateDifficulty(settings, npc);

                settings.GetGameStageConfig(assignedGameStage).ItemLotIds.Add(npc.itemLotId_enemy);
                addedByStage[assignedGameStage] += 1;
            }
            
        }

        return addedByStage;
    }

    private Dictionary<GameStage, int> SetupMapLots(string name, MSBE msb, HashSet<int> claimedIds, List<NpcParam> npcParams, ItemLotSettings settings, List<EventDropItemLotDetails> lotDetails)
    {
        Dictionary<GameStage, int> addedByStage = Enum.GetValues<GameStage>().ToDictionary(d => d, s => 0);

        if (configuration.Settings.ItemLotGeneratorSettings.ChestLootScannerSettings.Enabled
                || configuration.Settings.ItemLotGeneratorSettings.ChestLootScannerSettings.Enabled)
        {
            GameStageConfig gameStage = GetGameStageConfigsForMap(name, msb, npcParams, settings, lotDetails);

            List<int> candidateTreasures = [];

            List<MSBE.Event.Treasure> baseFilteredMapTreasures = msb.Events.Treasures
                .Where(d => d.ItemLotID > 0 && !claimedIds.Contains(d.ItemLotID)).ToList();

            if (configuration.Settings.ItemLotGeneratorSettings.ChestLootScannerSettings.Enabled)
            {
                candidateTreasures.AddRange(baseFilteredMapTreasures
                    .Where(d => d.InChest == 1)
                    .Where(d => random.PassesPercentCheck(configuration.Settings.ItemLotGeneratorSettings.ChestLootScannerSettings.ApplyPercent))
                    .Select(s => s.ItemLotID));
            }

            if (configuration.Settings.ItemLotGeneratorSettings.MapLootScannerSettings.Enabled)
            {
                candidateTreasures.AddRange(baseFilteredMapTreasures
                    .Where(d => d.InChest != 1)
                    .Where(d => random.PassesPercentCheck(configuration.Settings.ItemLotGeneratorSettings.MapLootScannerSettings.ApplyPercent))
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

    private GameStageConfig GetGameStageConfigsForMap(string name, MSBE msb, List<NpcParam> npcs, ItemLotSettings settings, List<EventDropItemLotDetails> lotDetails)
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

            return match.getItemFlagId > 0
                && (match.lotItemCategory01 >= 1
                    || match.lotItemCategory02 >= 1
                    || match.lotItemCategory03 >= 1
                    || match.lotItemCategory04 >= 1
                    || match.lotItemCategory05 >= 1
                    || match.lotItemCategory06 >= 1
                    || match.lotItemCategory07 >= 1
                    || match.lotItemCategory08 >= 1);
        }
        else
        {
            ItemLotParam_enemy? match = itemLotParam_Enemy
                .SingleOrDefault(d => itemLotId == d.ID);

            if (match == null)
            {
                return false;
            }

            return match.lotItemCategory01 >= 1
                    || match.lotItemCategory02 >= 1
                    || match.lotItemCategory03 >= 1
                    || match.lotItemCategory04 >= 1
                    || match.lotItemCategory05 >= 1
                    || match.lotItemCategory06 >= 1
                    || match.lotItemCategory07 >= 1
                    || match.lotItemCategory08 >= 1;
        }
    }
}
