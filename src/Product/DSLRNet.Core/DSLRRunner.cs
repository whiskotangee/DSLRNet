namespace DSLRNet.Core;

using DSLRNet.Core.DAL;
using DSLRNet.Core.Extensions;
using DSLRNet.Core.Scan;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

//todo - armor not getting descriptions
//todo - wolves in limgrave dropping godslaying things.  Should probably limit itemlot to lowest game stage available - look into this
public class DSLRRunner
{
    public static async Task ScanAsync(Settings settings)
    {
        IServiceProvider sp = await DSLRCommonSetupAsync(settings, true);

        var scanner = sp.GetRequiredService<ItemLotScanner>();

        scanner.ScanAndCreateItemLotSets();
    }

    public static async Task Run(Settings settings, ICollection<string>? logWatcher = null, IOperationProgressTracker? progressTracker = null)
    {
        progressTracker ??= new DefaultProgressTracker();
        progressTracker.OverallStepCount = 14;

        var sp = await DSLRCommonSetupAsync(settings, false, logWatcher, progressTracker);

        progressTracker.OverallStepCount++;

        ILogger<DSLRRunner> logger = sp.GetRequiredService<ILogger<DSLRRunner>>();

        try
        {
            IOperationProgressTracker progress = sp.GetRequiredService<IOperationProgressTracker>();
            Configuration activeConfig = sp.GetRequiredService<IOptions<Configuration>>().Value;

            progress.OverallProgress += 1;

            IconBuilder iconbuilder = sp.GetRequiredService<IconBuilder>();
            DSLRNetBuilder dslrBuilder = sp.GetRequiredService<DSLRNetBuilder>();

            await iconbuilder.ApplyIcons();
            progress.OverallProgress += 1;

            dslrBuilder.BuildItemLots();
            progress.OverallProgress += 1;

            await dslrBuilder.ApplyAsync();
            progress.OverallProgress += 1;
        }
        catch (Exception ex)
        {
            logger.LogError($"An error occurred during execution {ex}");
            throw;
        }
    }

    private static async Task<IServiceProvider> DSLRCommonSetupAsync(Settings settings, bool initializeMSB = false, ICollection<string>? logWatcher = null, IOperationProgressTracker? progressTracker = null)
    {
        settings.ValidatePaths();

        if (settings.RandomSeed == 0)
        {
            settings.RandomSeed = new Random().Next();
        }

        ConfigurationBuilder configurationBuilder = new();

        string[] jsonFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "appsettings.Default*.json");

        // Add each JSON file to the configuration builder
        foreach (string jsonFile in jsonFiles)
        {
            configurationBuilder.AddJsonFile(jsonFile, optional: false);
        }

        IConfigurationRoot configuration = configurationBuilder.Build();

        IServiceCollection services = new ServiceCollection();

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File($"log\\log_{timestamp}.txt")
            .CreateLogger();

        services.AddLogging((builder) =>
        {
            builder.AddSerilog();
            if (logWatcher != null)
            {
                builder.AddProvider(new CollectionLoggerProvider(logWatcher));
            }
        });

        services.SetupDSLR(configuration, settings, progressTracker);

        ServiceProvider sp = services.BuildServiceProvider();

        // ensure oo2core file is there
        string? existingFile = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "oo2core*dll").FirstOrDefault();
        if (existingFile == null)
        {
            string? oo2GameCore = Directory.GetFiles(settings.GamePath, "oo2core*dll").FirstOrDefault() ?? throw new InvalidOperationException("Could not find oo2core file in directory or game directory");
            File.Copy(oo2GameCore, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(oo2GameCore)));
        }

        List<Task> initializeTasks = [];
        initializeTasks.Add(sp.GetRequiredService<DataAccess>().InitializeDataSourcesAsync());
        if (initializeMSB)
        {
            initializeTasks.Add(sp.GetRequiredService<MSBProvider>().InitializeAsync());
        }

        await Task.WhenAll(initializeTasks);

        return sp;
    }
}
