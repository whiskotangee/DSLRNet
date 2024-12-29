namespace DSLRNet.Core.DAL;

using System.Collections.Concurrent;

public class MSBProvider
{
    private readonly Settings settings;
    private readonly ILogger<MSBProvider> logger;
    private readonly IOperationProgressTracker progressTracker;
    private readonly FileSourceHandler fileSourceHandler;
    private ConcurrentDictionary<string, MSBE> msbData = [];

    public MSBProvider(IOptions<Settings> settings, ILogger<MSBProvider> logger, IOperationProgressTracker progressTracker, FileSourceHandler fileSourceHandler)
    {
        this.logger = logger;
        this.progressTracker = progressTracker;
        this.fileSourceHandler = fileSourceHandler;
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
        List<string> mapStudioFiles = [.. this.fileSourceHandler.ListFilesFromAllModDirectories(Path.Combine("map", "mapstudio"), "*.msb.dcx")];

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
