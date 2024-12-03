using Microsoft.Extensions.Logging;
using DSLRNet.Core.Generators;
using DSLRNet.Core.Extensions;

namespace DSLRNet.Core;

public class DSLRNetBuilder(
    ILogger<DSLRNetBuilder> logger,
    ItemLotGenerator itemLotGenerator,
    IOptions<Configuration> configuration,
    ParamEditsRepository dataRepository,
    ItemLotScanner itemLotScanner)
{
    private readonly Configuration configuration = configuration.Value;
    private readonly ILogger<DSLRNetBuilder> logger = logger;
    private readonly ProcessRunner processRunner = new(logger);

    public async Task BuildAndApply()
    {
        // ensure oo2core file is there
        var existingFile = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "oo2core*dll").FirstOrDefault();
        if (existingFile == null)
        {
            var oo2GameCore = Directory.GetFiles(this.configuration.Settings.GamePath, "oo2core*dll").FirstOrDefault();
            File.Copy(oo2GameCore, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(oo2GameCore)));
        }

        Directory.CreateDirectory(configuration.Settings.DeployPath);

        List<ItemLotQueueEntry> enemyItemLotsSetups = Directory.GetFiles("DefaultData\\ER\\ItemLots\\Enemies", "*.ini", SearchOption.AllDirectories)
            .Select(s => ItemLotQueueEntry.Create(s, configuration.Itemlots.Categories[0]))
            .ToList();

        List<ItemLotQueueEntry> mapItemLotsSetups = Directory.GetFiles("DefaultData\\ER\\ItemLots\\Map", "*.ini", SearchOption.AllDirectories)
            .Select(s => ItemLotQueueEntry.Create(s, configuration.Itemlots.Categories[1]))
            .ToList();

        var takenIds = new Dictionary<ItemLotCategory, HashSet<int>>()
        {
            { ItemLotCategory.ItemLot_Map, new HashSet<int>() },
            { ItemLotCategory.ItemLot_Enemy, new HashSet<int>() }
        };

        takenIds[ItemLotCategory.ItemLot_Enemy] = enemyItemLotsSetups.SelectMany(s => s.GameStageConfigs).SelectMany(s => s.ItemLotIds).Distinct().ToHashSet();
        takenIds[ItemLotCategory.ItemLot_Map] = mapItemLotsSetups.SelectMany(s => s.GameStageConfigs).SelectMany(s => s.ItemLotIds).Distinct().ToHashSet();

        itemLotGenerator.CreateItemLots(enemyItemLotsSetups);
        itemLotGenerator.CreateItemLots(mapItemLotsSetups);

        var remainingIds = itemLotScanner.ScanForItemLotIds(takenIds);

        if (remainingIds.Any())
        {
            var remainingMapLots = ItemLotQueueEntry.Create("DefaultData\\ER\\ItemLots\\Default_Map.ini", configuration.Itemlots.Categories[1]);
            remainingMapLots.GameStageConfigs.First().ItemLotIds = remainingIds[ItemLotCategory.ItemLot_Map].OrderBy(d => d).ToList();

            var remainingEnemyLots = ItemLotQueueEntry.Create("DefaultData\\ER\\ItemLots\\Default_Enemy.ini", configuration.Itemlots.Categories[0]);
            remainingEnemyLots.GameStageConfigs.First().ItemLotIds = remainingIds[ItemLotCategory.ItemLot_Enemy].OrderBy(d => d).ToList();

            itemLotGenerator.CreateItemLots([remainingMapLots]);
            itemLotGenerator.CreateItemLots([remainingEnemyLots]);
        }

        dataRepository.VerifyItemLots();

        string regulationFile = Path.Combine(configuration.Settings.DeployPath, "regulation.pre-dslr.bin");
        string destinationFile = Path.Combine(configuration.Settings.DeployPath, "regulation.working.bin");

        if (!File.Exists(regulationFile))
        {
            regulationFile = Path.Combine(configuration.Settings.DeployPath, "regulation.bin");
            if (!File.Exists(regulationFile))
            {
                regulationFile = Path.Combine(configuration.Settings.GamePath, "regulation.bin");
            }
        }

        File.Copy(regulationFile, destinationFile, true);

        List<ParamEdit> generatedData = dataRepository.GetParamEdits(ParamOperation.MassEdit);

        IEnumerable<IGrouping<ParamNames, ParamEdit>> groups = generatedData.GroupBy(d => d.ParamName);

        var massEditFiles = Directory.GetFiles("DefaultData\\ER\\MassEdit\\", "*.massedit")
            .ToList();

        foreach (var massEdit in massEditFiles)
        {
            await ApplyMassEdit(massEdit, destinationFile);
        }

        foreach (IGrouping<ParamNames, ParamEdit> group in groups)
        {
            File.WriteAllLines(Path.Combine(configuration.Settings.DeployPath, $"{group.Key}.massedit"), group.Select(s => s.MassEditString));
            await ApplyMassEdit(Path.Combine(configuration.Settings.DeployPath, $"{group.Key}.massedit"), destinationFile);
        }

        await ApplyCreates(destinationFile, dataRepository);

        if (!File.Exists(destinationFile.Replace("working.", "pre-dslr.")))
        {
            File.Copy(destinationFile.Replace("working.", ""), destinationFile.Replace("working.", "pre-dslr."), true);
        }

        File.Copy(destinationFile, destinationFile.Replace(".working.bin", ".bin"), true);

        await UpdateMessages(dataRepository.GetParamEdits());
    }

    public async Task ApplyCreates(string regulationFile, ParamEditsRepository repository)
    {
        // write csv file with headers, but only for new things, aka none of the 
        List<ParamEdit> edits = repository.GetParamEdits(ParamOperation.Create);

        IEnumerable<ParamNames> paramNames = edits.Select(d => d.ParamName).Distinct();

        foreach (ParamNames paramName in paramNames)
        {
            // write csv
            string csvFile = Path.Combine(configuration.Settings.DeployPath, $"{paramName}.csv");

            List<GenericParam> parms = edits.Where(d => d.ParamName == paramName).OrderBy(d => d.ParamObject.ID).Select(d => d.ParamObject).ToList();

            Csv.WriteCsv(csvFile, parms.Select(d =>
            {
                var ret = d.Clone() as GenericParam;
                ret.Name = string.Empty;
                return ret;
            }).ToList());

            // dsms csv
            await processRunner.RunProcessAsync(new ProcessRunnerArgs<string>()
            {
                ExePath = configuration.Settings.DSMSPortablePath,
                Arguments = $"\"{regulationFile}\" -G ER -P \"{configuration.Settings.GamePath}\" -C \"{csvFile}\"",
                RetryCount = 0
            });
        }
    }

    private async Task ApplyMassEdit(string massEditFile, string destinationFile)
    {
        await processRunner.RunProcessAsync(new ProcessRunnerArgs<string>()
        {
            ExePath = configuration.Settings.DSMSPortablePath,
            Arguments = $"\"{destinationFile}\" -G ER -P \"{configuration.Settings.GamePath}\" -M+ \"{massEditFile}\"",
            RetryCount = 0
        });
    }

    public async Task UpdateMessages(List<ParamEdit> paramEdits)
    {
        List<string> gameMsgFiles = [];

        foreach (var fileName in configuration.Settings.MessageFileNames)
        {
            string existingPath = string.Empty;

            foreach (var msgPath in configuration.Settings.MessageSourcePaths)
            {
                if (File.Exists(Path.Combine(msgPath, fileName)))
                {
                    existingPath = Path.Combine(msgPath, fileName);
                    break;
                }
            }

            if (string.IsNullOrEmpty(existingPath))
            {
                throw new Exception($"Could not find message file {fileName}");
            }

            gameMsgFiles.Add(existingPath);
        }

        Directory.CreateDirectory(Path.Combine(configuration.Settings.DeployPath, "msg", "engus"));

        await Parallel.ForEachAsync(gameMsgFiles, (gameMsgFile, c) =>
        {
            // delete working file
            // copy msg file to a working file
            // process using working file as base
            string destinationFile = Path.Combine(configuration.Settings.DeployPath, "msg", "engus", Path.GetFileName(gameMsgFile));
            string sourceFile = destinationFile.Replace(".dcx", "pre-dslr.dcx");

            if (!File.Exists(sourceFile))
            {
                File.Copy(gameMsgFile, sourceFile);
            }

            logger.LogInformation($"Processing {Path.GetFileName(sourceFile)}");
            BND4 bnd = BND4.Read(sourceFile);

            var categories = paramEdits.Where(d => d.MessageText != null).GroupBy(d => d.MessageText.Category).ToList();

            foreach (var category in categories)
            {
                logger.LogInformation($"Processing category {Path.GetFileName(sourceFile)}-{category.Key}");
                var captionFilesToUpdate = bnd.Files.Where(d => d.Name.Contains($"{category.Key}Caption")).ToList();
                var infoFilesToUpdate = bnd.Files.Where(d => d.Name.Contains($"{category.Key}Info")).ToList();
                var nameFilesToUpdate = bnd.Files.Where(d => d.Name.Contains($"{category.Key}Name")).ToList();
                var effectFilesToUpdate = bnd.Files.Where(d => d.Name.Contains($"{category.Key}Effect")).ToList();

                logger.LogInformation($"Processing category {Path.GetFileName(sourceFile)}-{category.Key} captions");
                foreach (var captionFile in captionFilesToUpdate)
                {
                    FMG fmg = FMG.Read(captionFile.Bytes.ToArray());
                    fmg.Entries.AddRange(category.Where(d => d.MessageText.Caption != null).Select(d => new FMG.Entry((int)d.ParamObject.Properties["ID"], d.MessageText.Caption)));
                    captionFile.Bytes = fmg.Write();
                }

                logger.LogInformation($"Processing category {Path.GetFileName(sourceFile)}-{category.Key} info");
                foreach (var infoFile in infoFilesToUpdate)
                {
                    FMG fmg = FMG.Read(infoFile.Bytes.ToArray());
                    fmg.Entries.AddRange(category.Where(d => d.MessageText.Info != null).Select(d => new FMG.Entry((int)d.ParamObject.Properties["ID"], d.MessageText.Info)));
                    infoFile.Bytes = fmg.Write();
                }

                logger.LogInformation($"Processing category {Path.GetFileName(sourceFile)}-{category.Key} names");
                foreach (var nameFile in nameFilesToUpdate)
                {
                    FMG fmg = FMG.Read(nameFile.Bytes.ToArray());
                    fmg.Entries.AddRange(category.Where(d => d.MessageText.Name != null).Select(d => new FMG.Entry((int)d.ParamObject.Properties["ID"], d.MessageText.Name)));
                    nameFile.Bytes = fmg.Write();
                }

                logger.LogInformation($"Processing category {Path.GetFileName(sourceFile)}-{category.Key} effects");
                foreach (var effectFile in effectFilesToUpdate)
                {
                    FMG fmg = FMG.Read(effectFile.Bytes.ToArray());
                    fmg.Entries.AddRange(category.Where(d => d.MessageText.Effect != null).Select(d => new FMG.Entry((int)d.ParamObject.Properties["ID"], d.MessageText.Effect)));
                    effectFile.Bytes = fmg.Write();
                }

                logger.LogInformation($"Finished Processing category {Path.GetFileName(sourceFile)}-{category.Key}");
            }

            bnd.Write(destinationFile);

            logger.LogInformation($"Finished Processing {Path.GetFileName(sourceFile)}");

            return ValueTask.CompletedTask;
        });
    }
}
