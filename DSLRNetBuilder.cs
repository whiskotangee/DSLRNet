using DSLRNet.Config;
using DSLRNet.Contracts;
using DSLRNet.Data;
using DSLRNet.Handlers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mods.Common;
using SoulsFormats;
using Newtonsoft.Json;
using Serilog;
using DotNext.Collections.Generic;

namespace DSLRNet;

public class DSLRNetBuilder(
    ILogger<DSLRNetBuilder> logger,
    ItemLotGenerator itemLotGenerator,
    IOptions<Configuration> configuration,
    DataRepository dataRepository)
{
    private readonly Configuration configuration = configuration.Value;
    private readonly ILogger<DSLRNetBuilder> logger = logger;
    private readonly ProcessRunner processRunner = new(logger);
    private List<ItemLotBase> itemLotParam_Map = [];
    private List<ItemLotBase> itemLotParam_Enemy = [];

    public async Task BuildAndApply()
    {
        Directory.CreateDirectory(this.configuration.Settings.DeployPath);

        this.itemLotParam_Enemy = Csv.LoadCsv<ItemLotBase>("DefaultData\\ER\\CSVs\\LatestParams\\ItemLotParam_enemy.csv");
        this.itemLotParam_Map = Csv.LoadCsv<ItemLotBase>("DefaultData\\ER\\CSVs\\LatestParams\\ItemLotParam_map.csv");

        // get all queue entries

        List<ItemLotQueueEntry> enemyItemLotsSetups = Directory.GetFiles("DefaultData\\ER\\ItemLots\\Enemies", "*.ini", SearchOption.AllDirectories)
            .Select(s => ItemLotQueueEntry.Create(s, this.configuration.Itemlots.Categories[0]))
            .ToList();

        List<ItemLotQueueEntry> mapItemLotsSetups = Directory.GetFiles("DefaultData\\ER\\ItemLots\\Map", "*.ini", SearchOption.AllDirectories)
            .Select(s => ItemLotQueueEntry.Create(s, this.configuration.Itemlots.Categories[1]))
            .ToList();

        var takenIds = new Dictionary<ItemLotCategory, HashSet<int>>()
        {
            { ItemLotCategory.ItemLot_Map, new HashSet<int>() },
            { ItemLotCategory.ItemLot_Enemy, new HashSet<int>() }
        };

        takenIds[ItemLotCategory.ItemLot_Enemy] = enemyItemLotsSetups.SelectMany(s => s.GameStageConfigs).SelectMany(s => s.ItemLotIds).Distinct().ToHashSet();
        takenIds[ItemLotCategory.ItemLot_Map] = mapItemLotsSetups.SelectMany(s => s.GameStageConfigs).SelectMany(s => s.ItemLotIds).Distinct().ToHashSet();

        var remainingIds = GetRemainingIds(takenIds);

        var remainingMapLots = ItemLotQueueEntry.Create("DefaultData\\ER\\ItemLots\\Default_Map.ini", this.configuration.Itemlots.Categories[1]);
        remainingMapLots.GameStageConfigs.First().ItemLotIds = remainingIds[ItemLotCategory.ItemLot_Map].OrderBy(d => d).ToList();

        var remainingEnemyLots = ItemLotQueueEntry.Create("DefaultData\\ER\\ItemLots\\Default_Enemy.ini", this.configuration.Itemlots.Categories[0]);
        remainingEnemyLots.GameStageConfigs.First().ItemLotIds = remainingIds[ItemLotCategory.ItemLot_Enemy].OrderBy(d => d).ToList();

        itemLotGenerator.CreateItemLots(enemyItemLotsSetups);
        itemLotGenerator.CreateItemLots(mapItemLotsSetups);
        itemLotGenerator.CreateItemLots([remainingMapLots]);
        itemLotGenerator.CreateItemLots([remainingEnemyLots]);

        if (!dataRepository.VerifyItemLots())
        {
            throw new Exception("Ids referenced not found");
        }

        string regulationFile = Path.Combine(this.configuration.Settings.DeployPath, "regulation.pre-dslr.bin");
        string destinationFile = Path.Combine(this.configuration.Settings.DeployPath, "regulation.working.bin");

        if (!File.Exists(regulationFile))
        {
            regulationFile = Path.Combine(this.configuration.Settings.DeployPath, "regulation.bin");
            if (!File.Exists(regulationFile))
            {
                regulationFile = Path.Combine(this.configuration.Settings.GamePath, "regulation.bin");
            }
        }
        
        File.Copy(regulationFile, destinationFile, true);
        
        List<ParamEdit> generatedData = dataRepository.GetParamEdits(ParamOperation.MassEdit);

        IEnumerable<IGrouping<ParamNames, ParamEdit>> groups = generatedData.GroupBy(d => d.ParamName);

        var massEditFiles = Directory.GetFiles("DefaultData\\ER\\MassEdit\\", "*.massedit")
            .ToList();

        foreach (var massEdit in massEditFiles)
        {
            await this.ApplyMassEdit(massEdit, destinationFile);
        }

        foreach (IGrouping<ParamNames, ParamEdit> group in groups)
        {
            File.WriteAllLines(Path.Combine(this.configuration.Settings.DeployPath, $"{group.Key}.massedit"), group.Select(s => s.MassEditString));
            await this.ApplyMassEdit(Path.Combine(this.configuration.Settings.DeployPath, $"{group.Key}.massedit"), destinationFile);
        }

        await this.ApplyCreates(destinationFile, dataRepository);

        if (!File.Exists(destinationFile.Replace("working.", "pre-dslr.")))
        {
            File.Copy(destinationFile.Replace("working.", ""), destinationFile.Replace("working.", "pre-dslr."), true);
        }

        File.Copy(destinationFile, destinationFile.Replace(".working.bin", ".bin"), true);

        await UpdateMessages(dataRepository.GetParamEdits());
    }

    public async Task ApplyCreates(string regulationFile, DataRepository repository)
    {
        // write csv file with headers, but only for new things, aka none of the 
        List<ParamEdit> edits = repository.GetParamEdits(ParamOperation.Create);

        IEnumerable<ParamNames> paramNames = edits.Select(d => d.ParamName).Distinct();

        foreach(ParamNames paramName in paramNames)
        {
            // write csv
            string csvFile = Path.Combine(this.configuration.Settings.DeployPath, $"{paramName}.csv");

            List<GenericDictionary> parms = edits.Where(d => d.ParamName == paramName).OrderBy(d => d.ParamObject.GetValue<int>("ID")).Select(d => d.ParamObject).ToList();

            Csv.WriteCsv(csvFile, parms.Select(d =>
            {
                var ret = d.Clone() as GenericDictionary;
                ret.SetValue<string>("Name", string.Empty);
                return ret;
            }).ToList());

            // dsms csv
            await this.processRunner.RunProcessAsync(new ProcessRunnerArgs<string>()
            {
                ExePath = this.configuration.Settings.DSMSPortablePath,
                Arguments = $"\"{regulationFile}\" -G ER -P \"{this.configuration.Settings.GamePath}\" -C \"{csvFile}\"",
                RetryCount = 0
            });
        }
    }

    private async Task ApplyMassEdit(string massEditFile, string destinationFile)
    {
        await this.processRunner.RunProcessAsync(new ProcessRunnerArgs<string>()
        {
            ExePath = this.configuration.Settings.DSMSPortablePath,
            Arguments = $"\"{destinationFile}\" -G ER -P \"{this.configuration.Settings.GamePath}\" -M+ \"{massEditFile}\"",
            RetryCount = 0
        });
    }

    public async Task UpdateMessages(List<ParamEdit> paramEdits)
    {
        List<string> gameMsgFiles = [];

        foreach (var fileName in this.configuration.Settings.MessageFileNames)
        {
            string existingPath = string.Empty;

            foreach (var msgPath in this.configuration.Settings.MessageSourcePaths)
            {
                if (File.Exists(Path.Combine(msgPath, fileName)))
                {
                    existingPath = Path.Combine(msgPath, fileName);
                    break;
                }
            }

            if (string.IsNullOrEmpty(existingPath))
            {
                throw new Exception("plz");
            }

            gameMsgFiles.Add(existingPath);
        }

        Directory.CreateDirectory(Path.Combine(this.configuration.Settings.DeployPath, "msg", "engus"));

        await Parallel.ForEachAsync(gameMsgFiles, (gameMsgFile, c) =>
        {
            // delete working file
            // copy msg file to a working file
            // process using working file as base
            string destinationFile = Path.Combine(this.configuration.Settings.DeployPath, "msg", "engus", Path.GetFileName(gameMsgFile));
            string sourceFile = destinationFile.Replace(".dcx", "pre-dslr.dcx");

            if (!File.Exists(sourceFile))
            {
                File.Copy(gameMsgFile, sourceFile);
            }

            Log.Logger.Information($"Processing {Path.GetFileName(sourceFile)}");
            BND4 bnd = BND4.Read(sourceFile);

            var categories = paramEdits.Where(d => d.MessageText != null).GroupBy(d => d.MessageText.Category).ToList();

            foreach ( var category in categories)
            {
                Log.Logger.Information($"Processing category {Path.GetFileName(sourceFile)}-{category.Key}");
                var captionFilesToUpdate = bnd.Files.Where(d => d.Name.Contains($"{category.Key}Caption")).ToList();
                var infoFilesToUpdate = bnd.Files.Where(d => d.Name.Contains($"{category.Key}Info")).ToList();
                var nameFilesToUpdate = bnd.Files.Where(d => d.Name.Contains($"{category.Key}Name")).ToList();
                var effectFilesToUpdate = bnd.Files.Where(d => d.Name.Contains($"{category.Key}Effect")).ToList();

                Log.Logger.Information($"Processing category {Path.GetFileName(sourceFile)}-{category.Key} captions");
                foreach (var captionFile in captionFilesToUpdate)
                {
                    FMG fmg = FMG.Read(captionFile.Bytes.ToArray());
                    fmg.Entries.AddRange(category.Where(d => d.MessageText.Caption != null).Select(d => new FMG.Entry(fmg, (int)d.ParamObject.Properties["ID"], d.MessageText.Caption)));
                    captionFile.Bytes = fmg.Write();
                }

                Log.Logger.Information($"Processing category {Path.GetFileName(sourceFile)}-{category.Key} info");
                foreach (var infoFile in infoFilesToUpdate)
                {
                    FMG fmg = FMG.Read(infoFile.Bytes.ToArray());
                    fmg.Entries.AddRange(category.Where(d => d.MessageText.Info != null).Select(d => new FMG.Entry(fmg, (int)d.ParamObject.Properties["ID"], d.MessageText.Info)));
                    infoFile.Bytes = fmg.Write();
                }

                Log.Logger.Information($"Processing category {Path.GetFileName(sourceFile)}-{category.Key} names");
                foreach (var nameFile in nameFilesToUpdate)
                {
                    FMG fmg = FMG.Read(nameFile.Bytes.ToArray());
                    fmg.Entries.AddRange(category.Where(d => d.MessageText.Name != null).Select(d => new FMG.Entry(fmg, (int)d.ParamObject.Properties["ID"], d.MessageText.Name)));
                    nameFile.Bytes = fmg.Write();
                }

                Log.Logger.Information($"Processing category {Path.GetFileName(sourceFile)}-{category.Key} effects");
                foreach (var effectFile in effectFilesToUpdate)
                {
                    FMG fmg = FMG.Read(effectFile.Bytes.ToArray());
                    fmg.Entries.AddRange(category.Where(d => d.MessageText.Effect != null).Select(d => new FMG.Entry(fmg, (int)d.ParamObject.Properties["ID"], d.MessageText.Effect)));
                    effectFile.Bytes = fmg.Write();
                }

                Log.Logger.Information($"Finished Processing category {Path.GetFileName(sourceFile)}-{category.Key}");
            }

            bnd.Write(destinationFile);

            Log.Logger.Information($"Finished Processing {Path.GetFileName(sourceFile)}");

            return ValueTask.CompletedTask;
        });
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
            //finalArray = this.itemLotParam_Enemy
            //    .Where(d => baseLotIds.Contains(d.ID))
            //    .Select(d => d.ID)
            //    .ToList();

            //allTakenIds = this.itemLotParam_Enemy.Select(s => s.ID).ToList();

            //// List to store the result
            //List<int> result = [];

            //// Iterate over each original ID
            //foreach (var id in finalArray)
            //{
            //    for (int i = 1; i <= this.configuration.Settings.ItemLotsPerBaseLot; i++)
            //    {
            //        int newId = id + i;
            //        if (newId % 10 != 0 &&
            //            !allTakenIds.Contains(newId) &&
            //            !finalArray.Contains(newId))
            //        {
            //            result.Add(newId);
            //        }
            //    }
            //}
            //return baseLotIds.Union(result).ToList();
        }
    }

    public Dictionary<ItemLotCategory, HashSet<int>> GetRemainingIds(Dictionary<ItemLotCategory, HashSet<int>> claimedIds)
    {
        var modDir = $"{this.configuration.Settings.DeployPath}\\map\\mapstudio";

        var npcParams = Csv.LoadCsv<NpcParam>("DefaultData\\ER\\CSVs\\LatestParams\\NpcParam.csv");

        var mapStudioFiles = Directory.GetFiles(modDir, "*.msb.dcx")
            .ToList();

        var returnDictionary = new Dictionary<ItemLotCategory, HashSet<int>>()
        {
            { ItemLotCategory.ItemLot_Enemy, [] },
            { ItemLotCategory.ItemLot_Map, [] }
        };

        foreach (var mapFile in mapStudioFiles)
        {
            MSBE msb = MSBE.Read(mapFile);

            var searchString = JsonConvert.SerializeObject(msb, Formatting.Indented);

            var npcIds = new HashSet<int>();
            foreach (var enemy in msb.Parts.Enemies)
            {
                int modelNumber = int.Parse(enemy.ModelName.Substring(1));

                if (modelNumber >= 2000 && modelNumber <= 6000)
                {
                    npcIds.Add(enemy.NPCParamID);
                }
            }

            var candidateEnemyBaseLotIds = npcParams
                .Where(d => npcIds.Contains(d.ID) && d.itemLotId_enemy > 100 && !claimedIds[ItemLotCategory.ItemLot_Enemy].Contains(d.itemLotId_enemy))
                .Select(d => d.itemLotId_enemy)
                .Distinct()
                .ToList();

            var candidateMapBaseLotIds = msb.Events.Treasures
                .Where(d => d.ItemLotID > 0 && !claimedIds[ItemLotCategory.ItemLot_Map].Contains(d.ItemLotID))
                .Select(d => d.ItemLotID)
                .Distinct()
                .ToList()
                .Union(npcParams
                        .Where(d => npcIds.Contains(d.ID) && d.itemLotId_map > 0 && !claimedIds[ItemLotCategory.ItemLot_Map].Contains(d.itemLotId_map))
                        .Select(d => d.itemLotId_map)
                        .Distinct())
                .Distinct()
                .ToList();

            var enemyIds = this.GenerateSequentialItemLotIds(candidateEnemyBaseLotIds, ItemLotCategory.ItemLot_Enemy);
            var mapIds = this.GenerateSequentialItemLotIds(candidateMapBaseLotIds, ItemLotCategory.ItemLot_Map);

            returnDictionary[ItemLotCategory.ItemLot_Enemy].AddAll<int>(enemyIds.Distinct());
            returnDictionary[ItemLotCategory.ItemLot_Map].AddAll<int>(mapIds.Distinct());

            Log.Logger.Debug($"Found {enemyIds.Count} enemy itemLot Ids and {mapIds.Count} treasure Ids from {Path.GetFileName(mapFile)}");
        }

        return returnDictionary;
    }
}
