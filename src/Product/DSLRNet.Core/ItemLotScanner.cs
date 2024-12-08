namespace DSLRNet.Core;

public class ItemLotScanner(
    ILogger<ItemLotScanner> logger,
    RandomProvider random,
    IOptions<Configuration> configuration,
    IDataSource<ItemLotParam_map> mapItemLotSource,
    IDataSource<NpcParam> npcParamSource)
{
    private readonly ILogger<ItemLotScanner> logger = logger;
    private readonly RandomProvider random = random;
    private readonly Configuration configuration = configuration.Value;
    private readonly List<ItemLotParam_map> itemLotParam_Map = mapItemLotSource.GetAll().ToList();
    private readonly List<NpcParam> npcParams = npcParamSource.GetAll().ToList();

    public Dictionary<ItemLotCategory, HashSet<int>> ScanForItemLotIds(Dictionary<ItemLotCategory, HashSet<int>> claimedIds)
    {
        string modDir = $"{this.configuration.Settings.DeployPath}\\map\\mapstudio";

        List<string> mapStudioFiles = Directory.GetFiles(modDir, "*.msb.dcx")
            .ToList();

        Dictionary<ItemLotCategory, HashSet<int>> returnDictionary = new()
        {
            { ItemLotCategory.ItemLot_Enemy, [] },
            { ItemLotCategory.ItemLot_Map, [] }
        };

        foreach (string? mapFile in mapStudioFiles)
        {
            MSBE msb = MSBE.Read(mapFile);

            HashSet<int> npcIds = [];
            foreach (MSBE.Part.Enemy? enemy in msb.Parts.Enemies)
            {
                int modelNumber = int.Parse(enemy.ModelName.Substring(1));

                if (modelNumber >= 2000 && modelNumber <= 6000)
                {
                    npcIds.Add(enemy.NPCParamID);
                }
            }

            List<int> enemyIds = [];

            if (this.configuration.Settings.ItemLotGeneratorSettings.EnemyLootScannerSettings.Enabled)
            {
                List<int> candidateEnemyBaseLotIds = this.npcParams
                    .Where(d => npcIds.Contains(d.ID) && d.itemLotId_enemy > 100 && !claimedIds[ItemLotCategory.ItemLot_Enemy].Contains(d.itemLotId_enemy))
                    .Select(d => d.itemLotId_enemy)
                    .Distinct()
                    .ToList();

                enemyIds = this.GenerateSequentialItemLotIds(candidateEnemyBaseLotIds, ItemLotCategory.ItemLot_Enemy);
            }

            List<int> mapLotIds = [];

            if (this.configuration.Settings.ItemLotGeneratorSettings.ChestLootScannerSettings.Enabled
                || this.configuration.Settings.ItemLotGeneratorSettings.ChestLootScannerSettings.Enabled)
            {
                List<int> candidateTreasures = [];

                List<MSBE.Event.Treasure> baseFilteredMapTreasures = msb.Events.Treasures
                    .Where(d => d.ItemLotID > 0 && !claimedIds[ItemLotCategory.ItemLot_Map].Contains(d.ItemLotID)).ToList();

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
                        .Union(this.npcParams
                            .Where(d => npcIds.Contains(d.ID) && d.itemLotId_map > 0 && !claimedIds[ItemLotCategory.ItemLot_Map].Contains(d.itemLotId_map))
                            .Select(d => d.itemLotId_map)
                            .Distinct())
                        .ToList();
                }

                mapLotIds = this.GenerateSequentialItemLotIds(candidateTreasures, ItemLotCategory.ItemLot_Map);
            }

            mapLotIds.Distinct().ToList().ForEach(i => returnDictionary[ItemLotCategory.ItemLot_Map].Add(i));
            enemyIds.Distinct().ToList().ForEach(i => returnDictionary[ItemLotCategory.ItemLot_Enemy].Add(i));

            this.logger.LogInformation($"Found {enemyIds.Count} enemy itemLot Ids and {mapLotIds.Count} treasure Ids from {Path.GetFileName(mapFile)}");
        }

        return returnDictionary;
    }

    public List<int> GenerateSequentialItemLotIds(List<int> baseLotIds, ItemLotCategory itemLotCategory)
    {
        List<int> finalArray = [];
        List<int> allTakenIds = [];

        // Get the original IDs based on the category
        if (itemLotCategory == ItemLotCategory.ItemLot_Map)
        {
            finalArray = this.itemLotParam_Map
                .Where(d => baseLotIds.Contains(d.ID))
                .Where(d => d.getItemFlagId > 0)
                .Where(d => d.lotItemCategory01 >= 1
                            || d.lotItemCategory02 >= 1
                            || d.lotItemCategory03 >= 1
                            || d.lotItemCategory04 >= 1
                            || d.lotItemCategory05 >= 1
                            || d.lotItemCategory06 >= 1
                            || d.lotItemCategory07 >= 1
                            || d.lotItemCategory08 >= 1)
                .GroupBy(d => d.getItemFlagId)
                .Select(g => g.First().ID)
                .ToList();

            return finalArray;
        }
        else
        {
            return baseLotIds;
        }
    }
}
