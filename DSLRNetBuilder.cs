using CsvHelper.Configuration;
using CsvHelper;
using DSLRNet.Config;
using DSLRNet.Contracts;
using DSLRNet.Data;
using DSLRNet.Generators;
using DSLRNet.Handlers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mods.Common;
using SoulsFormats;
using System.Data.SqlTypes;
using System.Globalization;
using System.Security.Cryptography;
using System.Xml.Linq;
using Newtonsoft.Json;
using Microsoft.Extensions.Primitives;
using Serilog;

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

    public async Task BuildAndApply()
    {
        Directory.CreateDirectory(this.configuration.Settings.DeployPath);

        // get all queue entries

        IEnumerable<ItemLotQueueEntry> enemyItemLotsSetups = Directory.GetFiles("DefaultData\\ER\\ItemLots\\Enemies", "*.ini", SearchOption.AllDirectories)
            .Select(s => ItemLotQueueEntry.Create(s, this.configuration.Itemlots.Categories[0]));

        IEnumerable<ItemLotQueueEntry> mapItemLotsSetups = Directory.GetFiles("DefaultData\\ER\\ItemLots\\Map", "*.ini", SearchOption.AllDirectories)
            .Select(s => ItemLotQueueEntry.Create(s, this.configuration.Itemlots.Categories[1]));

        // ItemLotGenerator
        // do enemies
        // do map finds?
        // generate itemlots for each type
        // armor
        // weapon
        // talismans
        // Get/Generator massedit
        // dsms apply

        itemLotGenerator.CreateItemLots(enemyItemLotsSetups);
        itemLotGenerator.CreateItemLots(mapItemLotsSetups);

        string regulationFile = Path.Combine(this.configuration.Settings.DeployPath, "regulation.bin");
        string destinationFile = Path.Combine(this.configuration.Settings.DeployPath, "regulation.working.bin");

        if (!File.Exists(regulationFile))
        {
            regulationFile = Path.Combine(this.configuration.Settings.GamePath, "regulation.bin");
        }
        
        File.Copy(regulationFile, destinationFile, true);
        
        List<ParamEdit> generatedData = dataRepository.GetParamEdits(ParamOperation.MassEdit);

        IEnumerable<IGrouping<string, ParamEdit>> groups = generatedData.GroupBy(d => d.ParamName);

        var massEditFiles = Directory.GetFiles("DefaultData\\ER\\MassEdit\\", "*.massedit")
            .ToList();

        foreach (var massEdit in massEditFiles)
        {
            await this.ApplyMassEdit(massEdit, destinationFile);
        }

        foreach (IGrouping<string, ParamEdit> group in groups)
        {
            File.WriteAllLines(Path.Combine(this.configuration.Settings.DeployPath, $"{group.Key}.massedit"), group.Select(s => s.MassEditString));
            await this.ApplyMassEdit(Path.Combine(this.configuration.Settings.DeployPath, $"{group.Key}.massedit"), destinationFile);
        }

        await this.ApplyCreates(destinationFile, dataRepository);

        File.Copy(destinationFile.Replace("working.", ""), destinationFile.Replace("working.", "pre-dslr."), true);
        File.Copy(destinationFile, destinationFile.Replace(".working.bin", ".bin"), true);

        await UpdateMessages(dataRepository.GetParamEdits());
    }

    public async Task ApplyCreates(string regulationFile, DataRepository repository)
    {
        // write csv file with headers, but only for new things, aka none of the 
        List<ParamEdit> edits = repository.GetParamEdits(ParamOperation.Create);

        IEnumerable<string> paramNames = edits.Select(d => d.ParamName).Distinct();

        foreach(string? paramName in paramNames)
        {
            // write csv
            string csvFile = Path.Combine(this.configuration.Settings.DeployPath, $"{paramName}.csv");

            List<GenericDictionary> parms = edits.Where(d => d.ParamName == paramName).OrderBy(d => d.ParamObject.GetValue<int>("ID")).Select(d => d.ParamObject).ToList();
            Csv.WriteCsv(csvFile, parms);

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
                    fmg.Entries.AddRange(category.Where(d => d.MessageText.Caption != null).Select(d => new FMG.Entry((int)d.ParamObject.Properties["ID"], d.MessageText.Caption)));
                    captionFile.Bytes = fmg.Write();
                }

                Log.Logger.Information($"Processing category {Path.GetFileName(sourceFile)}-{category.Key} info");
                foreach (var infoFile in infoFilesToUpdate)
                {
                    FMG fmg = FMG.Read(infoFile.Bytes.ToArray());
                    fmg.Entries.AddRange(category.Where(d => d.MessageText.Info != null).Select(d => new FMG.Entry((int)d.ParamObject.Properties["ID"], d.MessageText.Info)));
                    infoFile.Bytes = fmg.Write();
                }

                Log.Logger.Information($"Processing category {Path.GetFileName(sourceFile)}-{category.Key} names");
                foreach (var nameFile in nameFilesToUpdate)
                {
                    FMG fmg = FMG.Read(nameFile.Bytes.ToArray());
                    fmg.Entries.AddRange(category.Where(d => d.MessageText.Name != null).Select(d => new FMG.Entry((int)d.ParamObject.Properties["ID"], d.MessageText.Name)));
                    nameFile.Bytes = fmg.Write();
                }

                Log.Logger.Information($"Processing category {Path.GetFileName(sourceFile)}-{category.Key} effects");
                foreach (var effectFile in effectFilesToUpdate)
                {
                    FMG fmg = FMG.Read(effectFile.Bytes.ToArray());
                    fmg.Entries.AddRange(category.Where(d => d.MessageText.Effect != null).Select(d => new FMG.Entry((int)d.ParamObject.Properties["ID"], d.MessageText.Effect)));
                    effectFile.Bytes = fmg.Write();
                }

                Log.Logger.Information($"Finished Processing category {Path.GetFileName(sourceFile)}-{category.Key}");
            }

            bnd.Write(destinationFile);

            Log.Logger.Information($"Finished Processing {Path.GetFileName(sourceFile)}");

            return ValueTask.CompletedTask;


            /*
            string destinationFile = Path.Combine(this.configuration.Settings.DeployPath, "msg", "engus", Path.GetFileName(gameMsgFile));
            Directory.CreateDirectory(Path.Combine(this.configuration.Settings.DeployPath, "msg", "engus"));

            File.Copy(gameMsgFile, destinationFile, true);

            // Note 07/05/23 - We need to split this up into separate calls!! CMD calls can be 8191 characters at most, let's have
            // it so that we have a "currentLine" we'll keep adding to until strArray[x].Length + currentLine.Length is over 7600 (to give us some leeway), then split things off into 
            // a new call
            string currentLine = "";
            int maxInt = 0;
            for (int x = 0; x < strArray.Count; x++)
            {
                maxInt = x;
                if (currentLine.Length + strArray[x].Length > splitThreshold)
                {
                    await this.processRunner.RunProcessAsync(new ProcessRunnerArgs<string>()
                    {
                        ExePath = this.configuration.Settings.DSMSPortablePath,
                        Arguments = $"--fmgentry \"{destinationFile}\" {currentLine}"
                    });
                    currentLine = "";
                }

                currentLine += strArray[x];
                // Add a space after this if we're not dealing with the last entry in the array
                if (x != strArray.Count - 1)
                {
                    currentLine += " ";
                }
            }
            */
        });
    }
}
