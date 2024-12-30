namespace DSLRNet.Core;

using DSLRNet.Core.DAL;
using DSLRNet.Core.Generators;
using DSLRNet.Core.Scan;
using Microsoft.Extensions.Logging;
using SoulsFormats;

public class DSLRNetBuilder(
    ILogger<DSLRNetBuilder> logger,
    ItemLotGenerator itemLotGenerator,
    IOptions<Settings> settingsOptions,
    IOptions<Configuration> configOptions,
    ParamEditsRepository dataRepository,
    ItemLotScanner itemLotScanner,
    FileSourceHandler fileSourceHandler,
    Csv csv,
    IOperationProgressTracker progressTracker)
{
    private readonly Settings settings = settingsOptions.Value;
    private readonly Configuration configuration = configOptions.Value;

    private readonly ILogger<DSLRNetBuilder> logger = logger;

    public void BuildItemLots()
    {
        Directory.CreateDirectory(this.settings.DeployPath);

        List<ItemLotSettings> enemyOverrides = Directory.GetFiles("Assets\\Data\\ItemLots\\EnemiesOverrides", "*.ini", SearchOption.AllDirectories)
            .Select(s => ItemLotSettings.Create(s, this.configuration.Itemlots.Categories[0]))
            .Where(s => s != null)
            .ToList();

        List<ItemLotSettings> mapOverrides = Directory.GetFiles("Assets\\Data\\ItemLots\\MapsOverrides", "*.ini", SearchOption.AllDirectories)
            .Select(s => ItemLotSettings.Create(s, this.configuration.Itemlots.Categories[1]))
            .Where(s => s != null)
            .ToList();

        Dictionary<ItemLotCategory, HashSet<int>> takenIds = new()
        {
            { ItemLotCategory.ItemLot_Map, new HashSet<int>() },
            { ItemLotCategory.ItemLot_Enemy, new HashSet<int>() }
        };

        takenIds[ItemLotCategory.ItemLot_Map] = mapOverrides.SelectMany(s => s.GameStageConfigs).SelectMany(s => s.Value.ItemLotIds).Distinct().ToHashSet();
        takenIds[ItemLotCategory.ItemLot_Enemy] = enemyOverrides.SelectMany(s => s.GameStageConfigs).SelectMany(s => s.Value.ItemLotIds).Distinct().ToHashSet();

        Dictionary<ItemLotCategory, List<ItemLotSettings>> scanned = itemLotScanner.ScanAndCreateItemLotSets(takenIds);
        progressTracker.OverallProgress += 1;

        itemLotGenerator.CreateItemLots(enemyOverrides);
        progressTracker.OverallProgress += 1;
        itemLotGenerator.CreateItemLots(mapOverrides);
        progressTracker.OverallProgress += 1;
        itemLotGenerator.CreateItemLots(scanned[ItemLotCategory.ItemLot_Enemy]);
        progressTracker.OverallProgress += 1;
        itemLotGenerator.CreateItemLots(scanned[ItemLotCategory.ItemLot_Map]);
        progressTracker.OverallProgress += 1;

        dataRepository.VerifyItemLots();
    }

    public async Task ApplyAsync()
    {
        string destinationFile = Path.Combine(this.settings.DeployPath, "regulation.working.bin");

        if (!fileSourceHandler.TryGetFile("regulation.bin", out string regulationFile))
        {
            throw new Exception("Could not find regulation.bin");
        }

        if (!File.Exists(Path.Combine(this.settings.DeployPath, "regulation.pre-dslr.bin")))
        {
            File.Copy(regulationFile, Path.Combine(this.settings.DeployPath, "regulation.pre-dslr.bin"));
        }

        File.Copy(regulationFile, destinationFile, true);

        await this.ApplyChanges(destinationFile, dataRepository);
        progressTracker.OverallProgress += 1;

        File.Copy(destinationFile, destinationFile.Replace(".working.bin", ".bin"), true);

        await this.UpdateMessages(dataRepository.GetParamEdits());
        progressTracker.OverallProgress += 1;
    }

    public async Task ApplyChanges(string regulationFile, ParamEditsRepository repository)
    {
        // write csv file with headers, but only for new things, aka none of the 
        List<ParamEdit> edits = repository.GetParamEdits(ParamOperation.Create);

        IEnumerable<ParamNames> paramNames = edits.Select(d => d.ParamName).Distinct();

        await Parallel.ForEachAsync(paramNames, (paramName, c) => 
        {
            // write csv
            string csvFile = Path.Combine(this.settings.DeployPath, $"{paramName}.csv");

            List<GenericParam> parms = edits.Where(d => d.ParamName == paramName).OrderBy(d => d.ParamObject.ID).Select(d => d.ParamObject).ToList();

            csv.WriteCsv(csvFile, parms.Select(d =>
            {
                GenericParam ret = d?.Clone() as GenericParam ?? throw new Exception($"Encountered null param when writing csv for {paramName}");
                ret.Name = string.Empty;
                return ret;
            }).ToList());

            return ValueTask.CompletedTask;
        });

        await repository.ApplyEditsToRegulationBinAsync(regulationFile);
    }

    public async Task UpdateMessages(List<ParamEdit> paramEdits)
    {
        List<string> gameMsgFiles = [];

        foreach (string fileName in this.settings.MessageFileNames)
        {
            if (fileSourceHandler.TryGetFile(fileName, out string existingPath))
            {
                gameMsgFiles.Add(existingPath);
                continue;
            }

            if (string.IsNullOrEmpty(existingPath))
            {
                throw new Exception($"Could not find message file {fileName}");
            }
        }

        Directory.CreateDirectory(Path.Combine(this.settings.DeployPath, "msg", "engus"));

        await Parallel.ForEachAsync(gameMsgFiles, (gameMsgFile, c) =>
        {
            // delete working file
            // copy msg file to a working file
            // process using working file as base
            string destinationFile = Path.Combine(this.settings.DeployPath, "msg", "engus", Path.GetFileName(gameMsgFile));
            string sourceFile = destinationFile.Replace(".dcx", "pre-dslr.dcx");

            if (!File.Exists(sourceFile))
            {
                File.Copy(gameMsgFile, sourceFile);
            }

            this.logger.LogInformation($"Processing {Path.GetFileName(sourceFile)}");
            BND4 bnd = BND4.Read(sourceFile);

            List<IGrouping<string?, ParamEdit>> categories = paramEdits.Where(d => d.MessageText != null).GroupBy(d => d.MessageText?.Category).ToList();

            foreach (IGrouping<string?, ParamEdit>? category in categories)
            {
                this.logger.LogInformation($"Processing category {Path.GetFileName(sourceFile)}-{category.Key}");
                List<BinderFile> captionFilesToUpdate = bnd.Files.Where(d => d.Name.Contains($"{category.Key}Caption")).ToList();
                List<BinderFile> infoFilesToUpdate = bnd.Files.Where(d => d.Name.Contains($"{category.Key}Info")).ToList();
                List<BinderFile> nameFilesToUpdate = bnd.Files.Where(d => d.Name.Contains($"{category.Key}Name")).ToList();
                List<BinderFile> effectFilesToUpdate = bnd.Files.Where(d => d.Name.Contains($"{category.Key}Effect")).ToList();

                this.logger.LogInformation($"Processing category {Path.GetFileName(sourceFile)}-{category.Key} captions");
                foreach (BinderFile? captionFile in captionFilesToUpdate)
                {
                    FMG fmg = FMG.Read([.. captionFile.Bytes]);
                    fmg.Entries.AddRange(category.Where(d => d.MessageText?.Caption != null).Select(d => new FMG.Entry(d.ParamObject.ID, d.MessageText?.Caption)));
                    captionFile.Bytes = fmg.Write();
                }

                this.logger.LogInformation($"Processing category {Path.GetFileName(sourceFile)}-{category.Key} info");
                foreach (BinderFile? infoFile in infoFilesToUpdate)
                {
                    FMG fmg = FMG.Read([.. infoFile.Bytes]);
                    fmg.Entries.AddRange(category.Where(d => d.MessageText?.Info != null).Select(d => new FMG.Entry(d.ParamObject.ID, d.MessageText?.Info)));
                    infoFile.Bytes = fmg.Write();
                }

                this.logger.LogInformation($"Processing category {Path.GetFileName(sourceFile)}-{category.Key} names");
                foreach (BinderFile? nameFile in nameFilesToUpdate)
                {
                    FMG fmg = FMG.Read([.. nameFile.Bytes]);
                    fmg.Entries.AddRange(category.Where(d => d.MessageText?.Name != null).Select(d => new FMG.Entry(d.ParamObject.ID, d.MessageText?.Name)));
                    nameFile.Bytes = fmg.Write();
                }

                this.logger.LogInformation($"Processing category {Path.GetFileName(sourceFile)}-{category.Key} effects");
                foreach (BinderFile? effectFile in effectFilesToUpdate)
                {
                    FMG fmg = FMG.Read([.. effectFile.Bytes]);
                    fmg.Entries.AddRange(category.Where(d => d.MessageText?.Effect != null).Select(d => new FMG.Entry(d.ParamObject.ID, d.MessageText?.Effect)));
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
