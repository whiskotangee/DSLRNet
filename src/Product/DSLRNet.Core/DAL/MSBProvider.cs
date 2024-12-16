namespace DSLRNet.Core.DAL;

public class MSBProvider
{
    private readonly Configuration configuration;
    private readonly ILogger<MSBProvider> logger;
    private Dictionary<string, MSBE> msbData = [];

    public MSBProvider(IOptions<Configuration> config, ILogger<MSBProvider> logger)
    {
        this.logger = logger;
        this.configuration = config.Value;
        
        Initialize();
    }

    public MSBE GetMsbFromName(string name)
    {
        return msbData[name];
    }

    public Dictionary<string, MSBE> GetAllMsbs()
    {
        return msbData;
    }

    public bool TryGetMSBForEntity(int entityId, out MSBE msb)
    {
        msb = msbData.Values.FirstOrDefault(d => d.Parts.Enemies.Any(t => t.EntityID == entityId));
        return msb != null;
    }

    private void Initialize()
    {
        logger.LogInformation($"Loading MSB Files...");
        List<string> mapStudioFiles = [.. Directory.GetFiles(Path.Combine(this.configuration.Settings.DeployPath, "map", "mapstudio"), "*.msb.dcx")];
        List<string> additionalMapFiles = [.. Directory.GetFiles(Path.Combine(this.configuration.Settings.GamePath, "map", "mapstudio"), "*.msb.dcx")
            .Where(d => !mapStudioFiles.Any(s => Path.GetFileName(s) == Path.GetFileName(d)))];

        mapStudioFiles.AddRange(additionalMapFiles);

        foreach (string mapFile in mapStudioFiles)
        {
            var name = Path.GetFileName(mapFile);
            name = name[.. name.IndexOf('.')];

            msbData[Path.GetFileName(mapFile)] = MSBE.Read(mapFile);
        }

        logger.LogInformation($"Finished loading MSB Files");
    }
}
