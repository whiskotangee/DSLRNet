namespace DSLRNet.Core;

using DSLRNet.Common;
using DSLRNet.Core.DAL;
using DSLRNet.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Collections.ObjectModel;

public class DSLRRunner
{
    // TODO: stats builder for ui progress
    public static async Task Run(Settings settings, ICollection<string>? logWatcher = null)
    {
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

        services.SetupDSLR(configuration, settings);

        ServiceProvider sp = services.BuildServiceProvider();

        var activeSettings = sp.GetRequiredService<IOptions<Settings>>().Value;
        var activeConfig = sp.GetRequiredService<IOptions<Configuration>>().Value;

        // ensure oo2core file is there
        string? existingFile = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "oo2core*dll").FirstOrDefault();
        if (existingFile == null)
        {
            string? oo2GameCore = Directory.GetFiles(activeSettings.GamePath, "oo2core*dll").FirstOrDefault();
            File.Copy(oo2GameCore, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(oo2GameCore)));
        }

        await sp.GetRequiredService<DataAccess>().InitializeDataSourcesAsync();

        IconBuilder iconbuilder = sp.GetRequiredService<IconBuilder>();
        DSLRNetBuilder dslrBuilder = sp.GetRequiredService<DSLRNetBuilder>();

        await iconbuilder.ApplyIcons();

        dslrBuilder.BuildItemLots();

        await dslrBuilder.ApplyAsync();
    }
}
