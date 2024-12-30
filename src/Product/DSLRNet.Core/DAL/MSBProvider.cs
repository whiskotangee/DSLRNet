namespace DSLRNet.Core.DAL;

using System.Collections.Concurrent;

public class MSBProvider(IOptions<Settings> settings, ILogger<MSBProvider> logger, IOperationProgressTracker progressTracker, FileSourceHandler fileSourceHandler)
{
    private readonly Settings settings = settings.Value;
    private readonly ILogger<MSBProvider> logger = logger;
    private readonly IOperationProgressTracker progressTracker = progressTracker;
    private readonly FileSourceHandler fileSourceHandler = fileSourceHandler;
    private readonly ConcurrentDictionary<string, MSBE> msbData = [];

    public Dictionary<string, MSBE> GetAllMsbs()
    {
        return msbData.ToDictionary();
    }

    public async Task InitializeAsync()
    {
        msbData.Clear();

        logger.LogInformation($"Loading MSB Files...");
        List<string> mapStudioFiles = [.. this.fileSourceHandler.ListFilesFromAllModDirectories(Path.Combine("map", "mapstudio"), "*.msb.dcx").Where(d => !d.Contains("_99."))];

        progressTracker.CurrentStageStepCount = mapStudioFiles.Count;
        progressTracker.CurrentStageProgress = 0;

        await Parallel.ForEachAsync(mapStudioFiles, (mapFile, c) =>
        {
            string name = Path.GetFileName(mapFile);
            name = name[..name.IndexOf('.')];

            progressTracker.CurrentStageProgress++;

            msbData.TryAdd(name, MSBE.Read(mapFile));

            return ValueTask.CompletedTask;
        });

        logger.LogInformation($"Finished loading MSB Files");
    }
}
