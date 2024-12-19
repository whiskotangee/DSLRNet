namespace DSLRNet.Core.Extensions;

using DSLRNet.Core.DAL;
using DSLRNet.Core.Generators;
using DSLRNet.Core.Scan;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection SetupDSLR(this IServiceCollection services, IConfiguration configuration)
    {
        // configurations
        services.Configure<Configuration>(configuration.GetSection(nameof(Configuration)))
                .Configure<Settings>(configuration.GetSection(nameof(Settings)))
                .Configure<LoreConfig>(configuration.GetSection(nameof(LoreConfig)));

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
                .AddSingleton<BossDropScanner>()
                .AddSingleton<GameStageEvaluator>()
                .AddSingleton<MSBProvider>()
                .AddSingleton<DataAccess>()
                .AddSingleton<Csv>()
                .AddSingleton<RegulationBinBank>()
                .AddSingleton<DataSourceFactory>()
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
        var dataSource = factory.CreateDataSource<T>(config);

        return dataSource;
    }
}
