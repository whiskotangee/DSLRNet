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

        ItemLotSettings? enemiesWithNewItemLots = CreateForEnemiesRequiringNewItemLotIds(claimedIds[ItemLotCategory.ItemLot_Enemy]);

        if (enemies != null)
        {
            generatedItemLotSettings[ItemLotCategory.ItemLot_Enemy].Add(enemies);
        }

        if (enemiesWithNewItemLots != null)
        {
            generatedItemLotSettings[ItemLotCategory.ItemLot_Enemy].Add(enemiesWithNewItemLots);
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

        List<IGrouping<int, int>> duplicates = generatedItemLotSettings[ItemLotCategory.ItemLot_Enemy].SelectMany(d => d.GameStageConfigs).SelectMany(d => d.Value.ItemLotIds).GroupBy(d => d).Where(d => d.Count() > 1).ToList();
        return generatedItemLotSettings;
    }

    private ItemLotSettings? GetItemLots(string file, Category category, ScannerSettings scannerSettings, HashSet<int> claimedIds)
    {
        if (!scannerSettings.Enabled)
        {
            return null;
        }

        ItemLotSettings settings = ItemLotSettings.Create(logger, file, category);

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

    private ItemLotSettings? CreateForEnemiesRequiringNewItemLotIds(HashSet<int> claimedIds)
    {
        if (!this.settings.ItemLotGeneratorSettings.EnemyLootScannerSettings.Enabled)
        {
            return null;
        }

        // create one for the duplicate enemies, also register the NpcParam edits here
        ItemLotSettings? enemyRequiringNewLots = ItemLotSettings.Create(logger, "Assets\\Data\\ItemLots\\Default_Enemy.ini", configuration.Itemlots.Categories[0]);

        List<NpcGameStage> npcEvaluations = JsonConvert.DeserializeObject<List<NpcGameStage>>(File.ReadAllText(PathHelper.FullyQualifyAppDomainPath("Assets", "Data", "ItemLots", "Scanned", "npcGameStageEvaluations.json")))
            .Where(d => d.RequiresNewItemLot)
            .ToList()
            ?? throw new Exception("Duplicate enemies config does not exist");

        foreach (NpcGameStage npcEvaluation in npcEvaluations)
        {
            NpcParam? existingNpcParam = dataAccess.NpcParam.GetItemById(npcEvaluation.NpcID);
            int originalItemLotId = existingNpcParam.itemLotId_enemy;

            if (existingNpcParam == null || existingNpcParam.itemLotId_enemy <= 0 || !random.PassesPercentCheck(this.settings.ItemLotGeneratorSettings.EnemyLootScannerSettings.ApplyPercent))
            {
                continue;
            }

            GameStage gameStage = npcEvaluation.GameStage;

            ItemLotParam_enemy itemLot = dataAccess.ItemLotParamEnemy.GetItemById(existingNpcParam.itemLotId_enemy).Clone();
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

            if (enemyRequiringNewLots.GameStageConfigs.Any(d => d.Value.ItemLotIds.Contains(214000000)))
            {
                Debugger.Break();
            }
        }

        if (enemyRequiringNewLots.GameStageConfigs.Any(d => d.Value.ItemLotIds.Contains(214000000)))
        {
            Debugger.Break();
        }

        return enemyRequiringNewLots;
    }
}
