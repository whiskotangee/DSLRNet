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

        // TODO: Loading ini is failing due to [] in values
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

        itemLotGenerator.CreateItemLots(enemyItemLotsSetups.Union(mapItemLotsSetups));
        //itemLotGenerator.CreateItemLots(enemyItemLotsSetups);

        string regulationFile = Path.Combine(this.configuration.Settings.OverrideModLocation, "regulation.bin");
        if (!File.Exists(regulationFile))
        {
            regulationFile = Path.Combine(this.configuration.Settings.GamePath, "regulation.bin");
        }

        string destinationFile = Path.Combine(this.configuration.Settings.DeployPath, "regulation.bin");
        File.Copy(regulationFile, destinationFile, true);
        File.Copy("DefaultData\\ER\\MassEdit\\postfix.massedit", Path.Combine(this.configuration.Settings.DeployPath, "postfix.massedit"), true);

        List<ParamEdit> generatedData = dataRepository.GetParamEdits(ParamOperation.MassEdit);

        List<string> generatedMessages = dataRepository.GetParamEdits().SelectMany(s => s.MessageText).ToList();

        IEnumerable<IGrouping<string, ParamEdit>> groups = generatedData.GroupBy(d => d.ParamName);

        foreach(IGrouping<string, ParamEdit> group in groups)
        {
            File.WriteAllLines(Path.Combine(this.configuration.Settings.DeployPath, $"{group.Key}.massedit"), group.Select(s => s.MassEditString));
            await this.ApplyMassEdit(Path.Combine(this.configuration.Settings.DeployPath, $"{group.Key}.massedit"), destinationFile);
        }

        await this.ApplyCreates(destinationFile, dataRepository);

        await UpdateMessages(generatedMessages);
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

    public async Task UpdateMessages(List<string> strArray)
    {
        List<string> gameMsgFiles = this.configuration.Settings.MessageFileNames.Select(s =>
        {
            string modDir = Path.Combine(this.configuration.Settings.OverrideModLocation, "msg", "engus", s);
            string gameDir = Path.Combine(this.configuration.Settings.GamePath, "msg", "engus", s);

            return File.Exists(modDir) ? modDir : gameDir;
        }).ToList();

        // Add preset baseline

        int splitThreshold = 7000;

        await Parallel.ForEachAsync(gameMsgFiles, async (gameMsgFile, c) =>
        {
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
        });
    }
}
