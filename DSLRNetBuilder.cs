using DSLRNet.Config;
using DSLRNet.Contracts;
using DSLRNet.Data;
using DSLRNet.Generators;
using DSLRNet.Handlers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mods.Common;

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

        var generatedData = dataRepository.GetMassEditContents();
        var generatedMessages = dataRepository.GetTextLines();

        generatedData.Keys.ToList().ForEach(d => File.WriteAllLines($"{d}.massedit", generatedData[d]));
        File.WriteAllLines("apply.txt", generatedMessages);

        Directory.CreateDirectory(this.configuration.Settings.DeployPath);

        foreach (var massEdit in generatedData)
        {
            await UpdateItems($"{massEdit.Key}.massedit");
        }
        
        await UpdateMessages(generatedMessages);          
    }

    public async Task UpdateItems(string massEditFile)
    {
        var regulationFile = Path.Combine(this.configuration.Settings.OverrideModLocation, "regulation.bin");
        if (!File.Exists(regulationFile))
        {
            regulationFile = Path.Combine(this.configuration.Settings.GamePath, "regulation.bin");
        }

        var destinationFile = Path.Combine(this.configuration.Settings.DeployPath, "regulation.bin");
        File.Copy(regulationFile, destinationFile, true);

        await this.processRunner.RunProcessAsync(new ProcessRunnerArgs<string>()
        {
            ExePath = this.configuration.Settings.DSMSPortablePath,
            //"%dsms%\DSMSPortable.exe" regulation.bin - G % gametype % -P "%erpath%" - M + "%dsmsmassedit%"
            Arguments = $"\"{destinationFile}\" -G ER -P \"{this.configuration.Settings.GamePath}\" -M+ \"{massEditFile}\"",
            RetryCount = 0
        });
    }

    public async Task UpdateMessages(List<string> strArray)
    {
        Directory.CreateDirectory(this.configuration.Settings.DeployPath);

        var gameMsgFiles = this.configuration.Settings.MessageFileNames.Select(s =>
        {
            var modDir = Path.Combine(this.configuration.Settings.OverrideModLocation, "msg", "enus", s);
            var gameDir = Path.Combine(this.configuration.Settings.GamePath, "msg", "enus", s);

            return File.Exists(modDir) ? modDir : gameDir;
        }).ToList();

        // Add preset baseline

        int splitThreshold = 7000;

        foreach (var gameMsgFile in gameMsgFiles)
        {
            var destinationFile = Path.Combine(this.configuration.Settings.DeployPath, "msg", "enus", Path.GetFileName(gameMsgFile));
            Directory.CreateDirectory(Path.Combine(this.configuration.Settings.DeployPath, "msg", "enus"));

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
                        Arguments = $"-fmgentry \"{destinationFile}\" {currentLine}"
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
        }
    }
}