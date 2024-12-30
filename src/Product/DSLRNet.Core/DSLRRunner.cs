﻿namespace DSLRNet.Core;

using DSLRNet.Core.DAL;
using DSLRNet.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

public class DSLRRunner
{
    public static async Task Run(Settings settings, ICollection<string>? logWatcher = null, IOperationProgressTracker? progressTracker = null)
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

        ILogger<DSLRRunner> logger = sp.GetRequiredService<ILogger<DSLRRunner>>();

        try
        {
            IOperationProgressTracker progress = sp.GetRequiredService<IOperationProgressTracker>();
            Settings activeSettings = sp.GetRequiredService<IOptions<Settings>>().Value;
            Configuration activeConfig = sp.GetRequiredService<IOptions<Configuration>>().Value;
            MSBProvider msbLoader = sp.GetRequiredService<MSBProvider>();

            progress.OverallStepCount = 14;

            // ensure oo2core file is there
            string? existingFile = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "oo2core*dll").FirstOrDefault();
            if (existingFile == null)
            {
                string? oo2GameCore = Directory.GetFiles(activeSettings.GamePath, "oo2core*dll").FirstOrDefault() ?? throw new InvalidOperationException("Could not find oo2core file in directory or game directory");
                File.Copy(oo2GameCore, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(oo2GameCore)));
            }

            List<Task> initializeTasks = [];
            initializeTasks.Add(sp.GetRequiredService<DataAccess>().InitializeDataSourcesAsync());
            initializeTasks.Add(msbLoader.InitializeAsync());

            await Task.WhenAll(initializeTasks);

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
        }
    }
}
