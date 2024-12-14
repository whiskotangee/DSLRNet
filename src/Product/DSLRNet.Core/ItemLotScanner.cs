namespace DSLRNet.Core;

using System.Collections.Concurrent;

public class ItemLotScanner(
    ILogger<ItemLotScanner> logger,
    RandomProvider random,
    IOptions<Configuration> configuration,
    IDataSource<ItemLotParam_map> mapItemLotSource,
    IDataSource<ItemLotParam_enemy> enemyItemLotSource,
    IDataSource<NpcParam> npcParamSource,
    IDataSource<SpEffectParam> spEffectParam,
    IDataSource<RaritySetup> raritySetup)
{
    private readonly ILogger<ItemLotScanner> logger = logger;
    private readonly RandomProvider random = random;
    private readonly Configuration configuration = configuration.Value;
    private readonly List<ItemLotParam_map> itemLotParam_Map = mapItemLotSource.GetAll().ToList();
    private readonly List<ItemLotParam_enemy> itemLotParam_Enemy = enemyItemLotSource.GetAll().ToList();
    private readonly List<NpcParam> npcParams = npcParamSource.GetAll().ToList();
    private readonly List<SpEffectParam> areaScalingSpEffects = 
        spEffectParam
            .GetAll()
            .Where(d => configuration.Value.Settings.ItemLotGeneratorSettings.ScannerAutoScalingSettings.AreaScalingSpEffectIds.Contains(d.ID))
            .ToList();

    public async Task<Dictionary<ItemLotCategory, ItemLotSettings>> ScanAndCreateItemLotSetsAsync(Dictionary<ItemLotCategory, HashSet<int>> claimedIds)
    {
        string modDir = $"{this.configuration.Settings.DeployPath}\\map\\mapstudio";

        Dictionary<ItemLotCategory, ItemLotSettings> generatedItemLotSettings = [];

        ItemLotSettings remainingMapLots = ItemLotSettings.Create("Assets\\Data\\ItemLots\\Default_Map_And_Events.ini", this.configuration.Itemlots.Categories[1]);
        ItemLotSettings remainingEnemyLots = ItemLotSettings.Create("Assets\\Data\\ItemLots\\Default_Enemy.ini", this.configuration.Itemlots.Categories[0]);

        generatedItemLotSettings.TryAdd(ItemLotCategory.ItemLot_Enemy, remainingEnemyLots);
        generatedItemLotSettings.TryAdd(ItemLotCategory.ItemLot_Map, remainingMapLots);

        List<string> mapStudioFiles = [.. Directory.GetFiles(modDir, "*.msb.dcx")];

        List<int> globalNpcIds = new List<int>();

        foreach (string mapFile in mapStudioFiles) 
        {
            MSBE msb = MSBE.Read(mapFile);

            string mapFileName = Path.GetFileName(mapFile);

            Dictionary<int, List<NpcParam>> npcs = [];
            foreach (MSBE.Part.Enemy? enemy in msb.Parts.Enemies)
            {
                if (globalNpcIds.Contains(enemy.NPCParamID))
                {
                    continue;
                }

                int modelNumber = int.Parse(enemy.ModelName.Substring(1));

                // Range is to ignore wildlife drops
                if (modelNumber >= 2000 && modelNumber <= 6000 || modelNumber >= 6200)
                {
                    if (!npcs.TryGetValue(modelNumber, out List<NpcParam> value))
                    {
                        value = new List<NpcParam>();
                        npcs[modelNumber] = value;
                    }

                    var item = npcParamSource.GetItemById(enemy.NPCParamID);
                    if (item == null)
                    {
                        logger.LogWarning($"NPC with ID {enemy.NPCParamID} from map {mapFileName} with model {modelNumber} did not match a param");
                        continue;
                    }
                    else if (item.itemLotId_enemy < 0 && item.itemLotId_map < 0)
                    {
                        logger.LogWarning($"NPC with ID {enemy.NPCParamID} from map {mapFileName} with model {modelNumber} did not have an item lot associated with it");
                        continue;
                    }

                    if (!value.Any(d => d.ID == enemy.NPCParamID))
                    {
                        value.Add(item);
                    }
                }
            }

            var enemiesAdded = this.SetupEnemyLots(mapFileName, claimedIds[ItemLotCategory.ItemLot_Enemy], npcs, remainingEnemyLots);
            var mapItemsAdded = this.SetupMapLots(mapFileName, msb, claimedIds[ItemLotCategory.ItemLot_Map], npcs, remainingMapLots);

            this.logger.LogInformation($"Map {mapFileName} Enemies: {JsonConvert.SerializeObject(enemiesAdded)} Treasures: {JsonConvert.SerializeObject(mapItemsAdded)}");
        }

        return generatedItemLotSettings;
    }

    private Dictionary<GameStage, int> SetupEnemyLots(string mapFile, HashSet<int> claimedIds, Dictionary<int, List<NpcParam>> npcParams, ItemLotSettings settings)
    {
        Dictionary<GameStage, int> addedByStage = Enum.GetValues<GameStage>().ToDictionary(d => d, s => 0);

        if (this.configuration.Settings.ItemLotGeneratorSettings.EnemyLootScannerSettings.Enabled)
        {
            var hpRates = areaScalingSpEffects.Select(d => (double)d.maxHpRate).OrderBy(d => d).ToList();
            hpRates.Prepend(1.0f);

            var hpMultToRarityMap = MathFunctions.MapToRange(
                hpRates,
                settings.GameStageConfigs.Min(d => d.AllowedRarities.Min()),
                settings.GameStageConfigs.Max(d => d.AllowedRarities.Max()));

            // TODO: Check baseHp of enemies and do a range based on those as well

            foreach (var npcMapping in npcParams.Keys)
            {
                foreach (var npc in npcParams[npcMapping].Where(d => d.itemLotId_enemy > 0))
                {
                    if (!IsValidItemLotId(npc.itemLotId_enemy, ItemLotCategory.ItemLot_Enemy))
                    {
                        logger.LogInformation($"Skipping item lot Id as it does not give any items {npc.itemLotId_enemy}");
                        continue;
                    }

                    if (claimedIds.Contains(npc.itemLotId_enemy) ||
                        !this.random.PassesPercentCheck(this.configuration.Settings.ItemLotGeneratorSettings.EnemyLootScannerSettings.ApplyPercent))
                    {
                        continue;
                    }

                    var maxHpMultiplier = 1.0f;
                    var spEffects = npc.GenericParam.GetFieldNamesByFilter("spEffectID").Select(npc.GenericParam.GetValue<int>)
                        .Where(d => areaScalingSpEffects.SingleOrDefault(s => s.ID == d) != null)
                        .Select(d => areaScalingSpEffects.SingleOrDefault(s => s.ID == d))
                        .ToList();

                    if (!spEffects.Any())
                    {
                        logger.LogWarning($"NPC id {npc.ID} missing area scaling of model number {npcMapping} from map {mapFile}");
                    }
                    else
                    {
                        maxHpMultiplier = spEffects.Max(s => s.maxHpRate);
                    }

                    if (!hpMultToRarityMap.TryGetValue(maxHpMultiplier, out var goalRarityMap))
                    {
                        throw new Exception("Excuse me?");
                    }

                    bool assigned = false;

                    foreach (var stage in settings.GameStageConfigs)
                    {
                        if (IntValueRange.CreateFrom(stage.AllowedRarities).Contains(goalRarityMap))
                        {
                            assigned = true;
                            stage.ItemLotIds.Add(npc.itemLotId_enemy);
                            addedByStage[stage.Stage] += 1;
                        }
                    }

                    // TODO: Adjust drop rate depending on how many instances of the enemy there are across all msbs
                    if (!assigned)
                    {
                        throw new Exception("Hello?");
                    }
                }
            }
        }

        return addedByStage;
    }

    private Dictionary<GameStage, int> SetupMapLots(string name, MSBE msb, HashSet<int> claimedIds, Dictionary<int, List<NpcParam>> npcParams, ItemLotSettings settings)
    {
        Dictionary<GameStage, int> addedByStage = Enum.GetValues<GameStage>().ToDictionary(d => d, s => 0);

        if (this.configuration.Settings.ItemLotGeneratorSettings.ChestLootScannerSettings.Enabled
                || this.configuration.Settings.ItemLotGeneratorSettings.ChestLootScannerSettings.Enabled)
        {
            GameStageConfig gameStage = GetGameStageConfigForMap(name, msb, settings);

            List<int> candidateTreasures = [];

            List<MSBE.Event.Treasure> baseFilteredMapTreasures = msb.Events.Treasures
                .Where(d => d.ItemLotID > 0 && !claimedIds.Contains(d.ItemLotID)).ToList();

            if (this.configuration.Settings.ItemLotGeneratorSettings.ChestLootScannerSettings.Enabled)
            {
                candidateTreasures.AddRange(baseFilteredMapTreasures
                    .Where(d => d.InChest == 1)
                    .Where(d => this.random.PassesPercentCheck(this.configuration.Settings.ItemLotGeneratorSettings.ChestLootScannerSettings.ApplyPercent))
                    .Select(s => s.ItemLotID));
            }

            if (this.configuration.Settings.ItemLotGeneratorSettings.MapLootScannerSettings.Enabled)
            {
                candidateTreasures.AddRange(baseFilteredMapTreasures
                    .Where(d => d.InChest != 1)
                    .Where(d => this.random.PassesPercentCheck(this.configuration.Settings.ItemLotGeneratorSettings.MapLootScannerSettings.ApplyPercent))
                    .Select(s => s.ItemLotID));
            }

            if (candidateTreasures.Any())
            {
                candidateTreasures = candidateTreasures
                    .Union(npcParams.Values.SelectMany(d => d)
                        .Where(d => d.itemLotId_map > 0 && !claimedIds.Contains(d.itemLotId_map))
                        .Select(d => d.itemLotId_map)
                        .Distinct())
                    .ToList();
            }

            candidateTreasures = candidateTreasures.Where(d => this.IsValidItemLotId(d, ItemLotCategory.ItemLot_Map)).ToList();
            addedByStage[gameStage.Stage] += candidateTreasures.Count;

            candidateTreasures.ForEach(d => gameStage.ItemLotIds.Add(d));
        }

        return addedByStage;
    }

    private GameStageConfig GetGameStageConfigForMap(string name, MSBE msb, ItemLotSettings settings)
    {
        // TODO: Mapping from msb to game stage
        return random.GetRandomItem(settings.GameStageConfigs);
    }

    private bool IsValidItemLotId(int itemLotId, ItemLotCategory itemLotCategory)
    {
        if (itemLotCategory == ItemLotCategory.ItemLot_Map)
        {
            var match = this.itemLotParam_Map
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
            var match = this.itemLotParam_Enemy
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
