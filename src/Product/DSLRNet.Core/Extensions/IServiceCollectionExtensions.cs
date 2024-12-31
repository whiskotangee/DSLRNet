namespace DSLRNet.Core.Extensions;

using DSLRNet.Core.DAL;
using DSLRNet.Core.Generators;
using DSLRNet.Core.Scan;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection SetupDSLR(this IServiceCollection services, IConfiguration configuration, Settings settings, IOperationProgressTracker? progressTracker)
    {
        // configurations
        services.Configure<Configuration>(configuration.GetSection(nameof(Configuration)))
                .Configure<Settings>(c =>
                {
                    c.DeployPath = settings.DeployPath;
                    c.ItemLotGeneratorSettings = settings.ItemLotGeneratorSettings;
                    c.RandomSeed = settings.RandomSeed;
                    c.GamePath = settings.GamePath;
                    c.MessageFileNames = settings.MessageFileNames;
                    c.ArmorGeneratorSettings = settings.ArmorGeneratorSettings;
                    c.WeaponGeneratorSettings = settings.WeaponGeneratorSettings;
                    c.IconBuilderSettings = settings.IconBuilderSettings;
                    c.OrderedModPaths = settings.OrderedModPaths;
                });

        // Services
        services.AddSingleton<ArmorLootGenerator>()
                .AddSingleton<WeaponLootGenerator>()
                .AddSingleton<TalismanLootGenerator>()
                .AddSingleton<ItemLotGenerator>()
                .AddSingleton<LoreGenerator>()
                .AddSingleton<AshofWarHandler>()
                .AddSingleton<DamageTypeHandler>()
                .AddSingleton<RarityHandler>()
                .AddSingleton<SpEffectHandler>()
                .AddSingleton<ParamEditsRepository>()
                .AddSingleton<DSLRNetBuilder>()
                .AddSingleton<ItemLotScanner>()
                .AddSingleton<IconBuilder>()
                .AddSingleton<BossDropScannerV2>()
                .AddSingleton<GameStageEvaluator>()
                .AddSingleton<MSBProvider>()
                .AddSingleton<DataAccess>()
                .AddSingleton<Csv>()
                .AddSingleton<RegulationBinBank>()
                .AddSingleton<DataSourceFactory>()
                .AddSingleton<FileSourceHandler>()
                .AddSingleton(sp => LoreConfig.LoadConfig())
                .AddSingleton((sp) => progressTracker ?? new DefaultProgressTracker())
                .AddSingleton((sp) =>
                {
                    return new RandomProvider(sp.GetRequiredService<IOptions<Settings>>().Value.RandomSeed);
                });

        return services;
    }

    public static IDataSource<T> CreateDataSource<T>(DataSourceFactory factory, DataSourceNames configName, Configuration configSettings)
        where T : ParamBase<T>, ICloneable<T>, new()
    {
        DataSourceConfig config = configSettings.DataSourceConfigs.Single(d => d.Name == configName);
        IDataSource<T> dataSource = factory.CreateDataSource<T>(config);

        return dataSource;
    }
}
