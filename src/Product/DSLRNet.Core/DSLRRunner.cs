namespace DSLRNet.Core;

using DSLRNet.Core.DAL;
using DSLRNet.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

public class DSLRRunner
{
    // TODO: stats builder for ui progress
    public static async Task Run(Settings? overrideSettings = null)
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
        });

        services.SetupDSLR(configuration);

        if (overrideSettings != null)
        {
            services.Configure<Settings>(c =>
            {
                c.DeployPath = overrideSettings.DeployPath;
                c.ItemLotGeneratorSettings = overrideSettings.ItemLotGeneratorSettings;
                c.RandomSeed = overrideSettings.RandomSeed;
                c.MessageSourcePaths = overrideSettings.MessageSourcePaths;
                c.GamePath = overrideSettings.GamePath;
                c.MessageFileNames = overrideSettings.MessageFileNames;
                c.ArmorGeneratorSettings = overrideSettings.ArmorGeneratorSettings;
                c.WeaponGeneratorSettings = overrideSettings.WeaponGeneratorSettings;
                c.IconBuilderSettings = overrideSettings.IconBuilderSettings;
            });
        }


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
