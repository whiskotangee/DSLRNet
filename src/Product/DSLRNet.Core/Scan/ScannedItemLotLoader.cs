namespace DSLRNet.Core.Scan;

using DSLRNet.Core.Config;
using DSLRNet.Core.DAL;
using System;
using System.Diagnostics;

public class ScannedItemLotLoader(
    ILogger<ScannedItemLotLoader> logger,
    RandomProvider random,
    IOptions<Configuration> configuration,
    IOptions<Settings> settings,
    DataAccess dataAccess,
    ParamEditsRepository paramEditsRepository)
{
    private readonly RandomProvider random = random;
    private readonly Configuration configuration = configuration.Value;
    private readonly Settings settings = settings.Value;
    private readonly IDGenerator itemLotIdGenerator = new()
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

        // TODO allow user config of boss drops
        ItemLotSettings? bosses = this.GetItemLots(
            "Assets\\Data\\ItemLots\\Scanned\\Bosses.ini",
            configuration.Itemlots.Categories[1],
            new ScannerSettings() { Enabled = true, ApplyPercent = 100 },
            claimedIds[ItemLotCategory.ItemLot_Map]);

        ItemLotSettings? enemies = this.GetItemLots(
            "Assets\\Data\\ItemLots\\Scanned\\Enemies.ini",
            configuration.Itemlots.Categories[0],
            settings.ItemLotGeneratorSettings.EnemyLootScannerSettings,
            claimedIds[ItemLotCategory.ItemLot_Enemy]);

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

        ItemLotSettings? enemiesWithNewItemLots = CreateForEnemiesRequiringNewItemLotIds();

        if (enemies != null)
        {
            logger.LogInformation($"Loaded {enemies.GameStageConfigs.Sum(d => d.Value.ItemLotIds.Count)} enemy item lots");
            generatedItemLotSettings[ItemLotCategory.ItemLot_Enemy].Add(enemies);
        }

        if (enemiesWithNewItemLots != null)
        {
            logger.LogInformation($"Loaded {enemiesWithNewItemLots.GameStageConfigs.Sum(d => d.Value.ItemLotIds.Count)} enemy item lots requiring new item lots");
            generatedItemLotSettings[ItemLotCategory.ItemLot_Enemy].Add(enemiesWithNewItemLots);
        }

        if (chests != null)
        {
            logger.LogInformation($"Loaded {chests.GameStageConfigs.Sum(d => d.Value.ItemLotIds.Count)} chest item lots");
            generatedItemLotSettings[ItemLotCategory.ItemLot_Map].Add(chests);
        }

        if (remainingMapLots != null)
        {
            logger.LogInformation($"Loaded {remainingMapLots.GameStageConfigs.Sum(d => d.Value.ItemLotIds.Count)} map item lots");
            generatedItemLotSettings[ItemLotCategory.ItemLot_Map].Add(remainingMapLots);
        }

        if (bosses != null)
        {
            logger.LogInformation($"Loaded {bosses.GameStageConfigs.Sum(d => d.Value.ItemLotIds.Count)} boss item lots");
            generatedItemLotSettings[ItemLotCategory.ItemLot_Map].Add(bosses);
        }

        List<IGrouping<int, int>> duplicates = generatedItemLotSettings[ItemLotCategory.ItemLot_Enemy].SelectMany(d => d.GameStageConfigs).SelectMany(d => d.Value.ItemLotIds).GroupBy(d => d).Where(d => d.Count() > 1).ToList();
        return generatedItemLotSettings;
    }

    private ItemLotSettings? GetItemLots(string file, Category category, ScannerSettings scannerSettings, HashSet<int> claimedIds)
    {
        if (!scannerSettings.Enabled)
        {
            return null;
        }

        ItemLotSettings settings = ItemLotSettings.Create(logger, PathHelper.FullyQualifyAppDomainPath(file), category);

        foreach (KeyValuePair<GameStage, GameStageConfig> gameStage in settings.GameStageConfigs)
        {
            int countBefore = gameStage.Value.ItemLotIds.Count;

            gameStage.Value.ItemLotIds =
                gameStage.Value.ItemLotIds
                    .Where(d => !claimedIds.Contains(d) && this.random.PassesPercentCheck(scannerSettings.ApplyPercent))
                    .ToHashSet();

            claimedIds.UnionWith(gameStage.Value.ItemLotIds);
        }

        return settings;
    }

    private ItemLotSettings? CreateForEnemiesRequiringNewItemLotIds()
    {
        if (!this.settings.ItemLotGeneratorSettings.EnemyLootScannerSettings.Enabled)
        {
            return null;
        }

        // create one for the duplicate enemies, also register the NpcParam edits here
        ItemLotSettings? enemyRequiringNewLots = ItemLotSettings.Create(logger, PathHelper.FullyQualifyAppDomainPath("Assets\\Data\\ItemLots\\Default_Enemy.ini"), configuration.Itemlots.Categories[0]);

        enemyRequiringNewLots.ID = Int32.MaxValue - 1;

        string npcGameStageEvaluationsJson = File.ReadAllText(PathHelper.FullyQualifyAppDomainPath("Assets", "Data", "ItemLots", "Scanned", "npcGameStageEvaluations.json"));

        if (string.IsNullOrEmpty(npcGameStageEvaluationsJson))
        {
            return null;
        }

        List<NpcGameStage>? npcEvaluations = JsonConvert.DeserializeObject<List<NpcGameStage>>(npcGameStageEvaluationsJson);

        if (npcEvaluations == null)
        {
            return null;
        }

        npcEvaluations = npcEvaluations.Where(d => d.RequiresNewItemLot).ToList();

        foreach (NpcGameStage npcEvaluation in npcEvaluations)
        {
            NpcParam? existingNpcParam = dataAccess.NpcParam.GetItemById(npcEvaluation.NpcID);
            int originalItemLotId = existingNpcParam?.itemLotId_enemy ?? -1;

            if (existingNpcParam == null || existingNpcParam.itemLotId_enemy <= 0 || !random.PassesPercentCheck(this.settings.ItemLotGeneratorSettings.EnemyLootScannerSettings.ApplyPercent))
            {
                continue;
            }

            GameStage gameStage = npcEvaluation.GameStage;

            ItemLotParam_enemy? itemLot = dataAccess.ItemLotParamEnemy.GetItemById(originalItemLotId)?.Clone();
            if (itemLot == null)
            {
                continue;
            }

            itemLot.ID = itemLotIdGenerator.GetNext();
            existingNpcParam.itemLotId_enemy = itemLot.ID;

            if (!random.PassesPercentCheck(this.settings.ItemLotGeneratorSettings.EnemyLootScannerSettings.ApplyPercent))
            {
                continue;
            }

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

            enemyRequiringNewLots.GetGameStageConfig(gameStage).ItemLotIds.Add(itemLot.ID);
        }

        return enemyRequiringNewLots;
    }
}
