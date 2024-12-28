namespace DSLRNet.Core.DAL;

using System.Collections.Concurrent;

public class MSBProvider
{
    private readonly Settings settings;
    private readonly ILogger<MSBProvider> logger;
    private readonly IOperationProgressTracker progressTracker;
    private ConcurrentDictionary<string, MSBE> msbData = [];

    public MSBProvider(IOptions<Settings> settings, ILogger<MSBProvider> logger, IOperationProgressTracker progressTracker)
    {
        this.logger = logger;
        this.progressTracker = progressTracker;
        this.settings = settings.Value;
    }

    public Dictionary<string, MSBE> GetAllMsbs()
    {
        return msbData.ToDictionary();
    }

    public async Task InitializeAsync()
    {
        msbData.Clear();

        logger.LogInformation($"Loading MSB Files...");
        List<string> mapStudioFiles = [.. Directory.GetFiles(Path.Combine(this.settings.DeployPath, "map", "mapstudio"), "*.msb.dcx")];
        List<string> additionalMapFiles = [.. Directory.GetFiles(Path.Combine(this.settings.GamePath, "map", "mapstudio"), "*.msb.dcx")
            .Where(d => !mapStudioFiles.Any(s => Path.GetFileName(s) == Path.GetFileName(d)))];

        mapStudioFiles.AddRange(additionalMapFiles);

        progressTracker.CurrentStageStepCount = mapStudioFiles.Count;
        progressTracker.CurrentStageProgress = 0;

        await Parallel.ForEachAsync(mapStudioFiles, (mapFile, c) =>
        {
            var name = Path.GetFileName(mapFile);
            name = name[..name.IndexOf('.')];

            progressTracker.CurrentStageProgress++;

            msbData.TryAdd(name, MSBE.Read(mapFile));

            return ValueTask.CompletedTask;
        });

        logger.LogInformation($"Finished loading MSB Files");
    }
}
