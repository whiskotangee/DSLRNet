using DSLRNet.Config;
using DSLRNet.Contracts;
using DSLRNet.Data;
using DSLRNet.Generators;
using DSLRNet.Handlers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mods.Common;
using System.Globalization;

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
        //var csvFiles = Directory.GetFiles("DefaultData\\ER\\CSVs", "*.csv");

        //foreach (var csvFile in csvFiles)
        //{
        //    GenerateClassFromCsv(csvFile);
        //}

        // get all queue entries

        // TODO: Loading ini is failing due to [] in values
        var enemyItemLotsSetups = Directory.GetFiles("DefaultData\\ER\\ItemLots\\Enemies", "*.ini", SearchOption.AllDirectories)
            .Select(s => ItemLotQueueEntry.Create(s, this.configuration.Itemlots.ParamCategories[0]));

        var mapItemLotsSetups = Directory.GetFiles("DefaultData\\ER\\ItemLots\\Map", "*.ini", SearchOption.AllDirectories)
            .Select(s => ItemLotQueueEntry.Create(s, this.configuration.Itemlots.ParamCategories[1]));

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

        var generatedData = dataRepository.GetMassEditContents();
        var generatedMessages = dataRepository.GetTextLines();

        generatedData.Keys.ToList().ForEach(d => File.WriteAllLines(Path.Combine(this.configuration.Settings.DeployPath, $"{d}.massedit"), generatedData[d]));

        Directory.CreateDirectory(this.configuration.Settings.DeployPath);

        var regulationFile = Path.Combine(this.configuration.Settings.OverrideModLocation, "regulation.bin");
        if (!File.Exists(regulationFile))
        {
            regulationFile = Path.Combine(this.configuration.Settings.GamePath, "regulation.bin");
        }

        var destinationFile = Path.Combine(this.configuration.Settings.DeployPath, "regulation.bin");
        File.Copy(regulationFile, destinationFile, true);
        File.Copy("DefaultData\\ER\\MassEdit\\postfix.massedit", Path.Combine(this.configuration.Settings.DeployPath, "postfix.massedit"), true);

        foreach (var massEdit in generatedData)
        {
            await this.ApplyMassEdit(Path.Combine(this.configuration.Settings.DeployPath, $"{massEdit.Key}.massedit"), destinationFile);
        }

        await this.ApplyMassEdit(Path.Combine(this.configuration.Settings.DeployPath, Path.Combine(this.configuration.Settings.DeployPath, "postfix.massedit")), destinationFile);

        await UpdateMessages(generatedMessages);
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
    public static void GenerateClassFromCsv(string csvFilePath)
    {
        var lines = File.ReadAllLines(csvFilePath);
        if (lines.Length < 2)
        {
            throw new InvalidOperationException("CSV file must contain at least two lines (header and one data row).");
        }

        var headers = lines[0].Split(',');
        var dataRows = lines.Skip(1).Select(line => line.Split(',')).ToArray();

        var properties = new List<string>();

        for (int i = 0; i < headers.Length; i++)
        {
            var header = headers[i];
            var columnValues = dataRows.Select(row => row[i]);
            var type = DetermineType(columnValues);
            properties.Add($"public {type} {header} {{ get; set; }}");
        }

        var classDefinition = $@"
namespace DSLRNet.Data;

public class {Path.GetFileNameWithoutExtension(csvFilePath)}
{{
    {string.Join(Environment.NewLine + "    ", properties)}
}}";

        File.WriteAllText($"{Path.GetFileNameWithoutExtension(csvFilePath)}.cs", classDefinition);

        Console.WriteLine(classDefinition);
    }

    private static string DetermineType(IEnumerable<string> values)
    {
        if (values.All(d => int.TryParse(d, out _)))
        {
            return "int";
        }

        if (values.All(d => float.TryParse(d, NumberStyles.Float, CultureInfo.InvariantCulture, out _)))
        {
            return "float";
        }

        return "string";
    }

    public async Task UpdateMessages(List<string> strArray)
    {
        Directory.CreateDirectory(this.configuration.Settings.DeployPath);

        var gameMsgFiles = this.configuration.Settings.MessageFileNames.Select(s =>
        {
            var modDir = Path.Combine(this.configuration.Settings.OverrideModLocation, "msg", "engus", s);
            var gameDir = Path.Combine(this.configuration.Settings.GamePath, "msg", "engus", s);

            return File.Exists(modDir) ? modDir : gameDir;
        }).ToList();

        // Add preset baseline

        int splitThreshold = 7000;

        await Parallel.ForEachAsync(gameMsgFiles, async (gameMsgFile, c) =>
        {
            var destinationFile = Path.Combine(this.configuration.Settings.DeployPath, "msg", "engus", Path.GetFileName(gameMsgFile));
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