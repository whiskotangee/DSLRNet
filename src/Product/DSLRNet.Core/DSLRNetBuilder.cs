namespace DSLRNet.Core;

using DSLRNet.Core.Generators;
using Microsoft.Extensions.Logging;
using SoulsFormats;
using SoulsFormats.Formats;

public class DSLRNetBuilder(
    ILogger<DSLRNetBuilder> logger,
    ItemLotGenerator itemLotGenerator,
    IOptions<Configuration> configuration,
    ParamEditsRepository dataRepository,
    ItemLotScanner itemLotScanner,
    ProcessRunner processRunner,
    Csv csv)
{
    private readonly Configuration configuration = configuration.Value;
    private readonly ILogger<DSLRNetBuilder> logger = logger;
    private readonly ProcessRunner processRunner = processRunner;

    public async Task BuildAndApply()
    {
        // ensure oo2core file is there
        string? existingFile = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "oo2core*dll").FirstOrDefault();
        if (existingFile == null)
        {
            string? oo2GameCore = Directory.GetFiles(this.configuration.Settings.GamePath, "oo2core*dll").FirstOrDefault();
            File.Copy(oo2GameCore, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(oo2GameCore)));
        }

        Directory.CreateDirectory(this.configuration.Settings.DeployPath);

        List<ItemLotSettings> enemyItemLotsSetups = Directory.GetFiles("Assets\\Data\\ItemLots\\Enemies", "*.ini", SearchOption.AllDirectories)
            .Select(s => ItemLotSettings.Create(s, this.configuration.Itemlots.Categories[0]))
            .ToList();

        List<ItemLotSettings> mapItemLotsSetups = Directory.GetFiles("Assets\\Data\\ItemLots\\Map", "*.ini", SearchOption.AllDirectories)
            .Select(s => ItemLotSettings.Create(s, this.configuration.Itemlots.Categories[1]))
            .ToList();

        Dictionary<ItemLotCategory, HashSet<int>> takenIds = new()
        {
            { ItemLotCategory.ItemLot_Map, new HashSet<int>() },
            { ItemLotCategory.ItemLot_Enemy, new HashSet<int>() }
        };

        takenIds[ItemLotCategory.ItemLot_Enemy] = enemyItemLotsSetups.SelectMany(s => s.GameStageConfigs).SelectMany(s => s.ItemLotIds).Distinct().ToHashSet();
        takenIds[ItemLotCategory.ItemLot_Map] = mapItemLotsSetups.SelectMany(s => s.GameStageConfigs).SelectMany(s => s.ItemLotIds).Distinct().ToHashSet();

        itemLotGenerator.CreateItemLots(enemyItemLotsSetups);
        itemLotGenerator.CreateItemLots(mapItemLotsSetups);

        Dictionary<ItemLotCategory, HashSet<int>> remainingIds = itemLotScanner.ScanForItemLotIds(takenIds);

        if (remainingIds.Any())
        {
            ItemLotSettings remainingMapLots = ItemLotSettings.Create("Assets\\Data\\ItemLots\\Default_Map.ini", this.configuration.Itemlots.Categories[1]);
            remainingMapLots.GameStageConfigs.First().ItemLotIds = remainingIds[ItemLotCategory.ItemLot_Map].OrderBy(d => d).ToList();

            ItemLotSettings remainingEnemyLots = ItemLotSettings.Create("Assets\\Data\\ItemLots\\Default_Enemy.ini", this.configuration.Itemlots.Categories[0]);
            remainingEnemyLots.GameStageConfigs.First().ItemLotIds = remainingIds[ItemLotCategory.ItemLot_Enemy].OrderBy(d => d).ToList();

            itemLotGenerator.CreateItemLots([remainingMapLots]);
            itemLotGenerator.CreateItemLots([remainingEnemyLots]);
        }

        dataRepository.VerifyItemLots();

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

        List<string> massEditFiles = Directory.GetFiles("Data\\MassEdit\\", "*.massedit")
            .ToList();

        foreach (string? massEdit in massEditFiles)
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

        await this.UpdateMessages(dataRepository.GetParamEdits());
    }

    public async Task ApplyCreates(string regulationFile, ParamEditsRepository repository)
    {
        // write csv file with headers, but only for new things, aka none of the 
        List<ParamEdit> edits = repository.GetParamEdits(ParamOperation.Create);

        IEnumerable<ParamNames> paramNames = edits.Select(d => d.ParamName).Distinct();

        foreach (ParamNames paramName in paramNames)
        {
            // write csv
            string csvFile = Path.Combine(this.configuration.Settings.DeployPath, $"{paramName}.csv");

            List<GenericParam> parms = edits.Where(d => d.ParamName == paramName).OrderBy(d => d.ParamObject.ID).Select(d => d.ParamObject).ToList();

            csv.WriteCsv(csvFile, parms.Select(d =>
            {
                GenericParam? ret = d.Clone() as GenericParam;
                ret.Name = string.Empty;
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

        foreach (string fileName in this.configuration.Settings.MessageFileNames)
        {
            string existingPath = string.Empty;

            foreach (string msgPath in this.configuration.Settings.MessageSourcePaths)
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

            this.logger.LogInformation($"Processing {Path.GetFileName(sourceFile)}");
            BND4 bnd = BND4.Read(sourceFile);

            List<IGrouping<string, ParamEdit>> categories = paramEdits.Where(d => d.MessageText != null).GroupBy(d => d.MessageText.Category).ToList();

            foreach (IGrouping<string, ParamEdit>? category in categories)
            {
                this.logger.LogInformation($"Processing category {Path.GetFileName(sourceFile)}-{category.Key}");
                List<BinderFile> captionFilesToUpdate = bnd.Files.Where(d => d.Name.Contains($"{category.Key}Caption")).ToList();
                List<BinderFile> infoFilesToUpdate = bnd.Files.Where(d => d.Name.Contains($"{category.Key}Info")).ToList();
                List<BinderFile> nameFilesToUpdate = bnd.Files.Where(d => d.Name.Contains($"{category.Key}Name")).ToList();
                List<BinderFile> effectFilesToUpdate = bnd.Files.Where(d => d.Name.Contains($"{category.Key}Effect")).ToList();

                this.logger.LogInformation($"Processing category {Path.GetFileName(sourceFile)}-{category.Key} captions");
                foreach (BinderFile? captionFile in captionFilesToUpdate)
                {
                    FMG fmg = FMG.Read(captionFile.Bytes.ToArray());
                    fmg.Entries.AddRange(category.Where(d => d.MessageText.Caption != null).Select(d => new FMG.Entry((int)d.ParamObject.Properties["ID"], d.MessageText.Caption)));
                    captionFile.Bytes = fmg.Write();
                }

                this.logger.LogInformation($"Processing category {Path.GetFileName(sourceFile)}-{category.Key} info");
                foreach (BinderFile? infoFile in infoFilesToUpdate)
                {
                    FMG fmg = FMG.Read(infoFile.Bytes.ToArray());
                    fmg.Entries.AddRange(category.Where(d => d.MessageText.Info != null).Select(d => new FMG.Entry((int)d.ParamObject.Properties["ID"], d.MessageText.Info)));
                    infoFile.Bytes = fmg.Write();
                }

                this.logger.LogInformation($"Processing category {Path.GetFileName(sourceFile)}-{category.Key} names");
                foreach (BinderFile? nameFile in nameFilesToUpdate)
                {
                    FMG fmg = FMG.Read(nameFile.Bytes.ToArray());
                    fmg.Entries.AddRange(category.Where(d => d.MessageText.Name != null).Select(d => new FMG.Entry((int)d.ParamObject.Properties["ID"], d.MessageText.Name)));
                    nameFile.Bytes = fmg.Write();
                }

                this.logger.LogInformation($"Processing category {Path.GetFileName(sourceFile)}-{category.Key} effects");
                foreach (BinderFile? effectFile in effectFilesToUpdate)
                {
                    FMG fmg = FMG.Read(effectFile.Bytes.ToArray());
                    fmg.Entries.AddRange(category.Where(d => d.MessageText.Effect != null).Select(d => new FMG.Entry((int)d.ParamObject.Properties["ID"], d.MessageText.Effect)));
                    effectFile.Bytes = fmg.Write();
                }

                this.logger.LogInformation($"Finished Processing category {Path.GetFileName(sourceFile)}-{category.Key}");
            }

            bnd.Write(destinationFile);

            this.logger.LogInformation($"Finished Processing {Path.GetFileName(sourceFile)}");

            return ValueTask.CompletedTask;
        });
    }
}
