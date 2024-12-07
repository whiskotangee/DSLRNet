namespace DSLRNet.Core.Extensions;
using DSLRNet.Core.Generators;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection InitializeDSLR(this IServiceCollection services, IConfiguration configuration)
    {
        // configurations
        services.Configure<Configuration>(configuration.GetSection(nameof(Configuration)))
                .Configure<WeaponGeneratorConfig>(configuration.GetSection(nameof(WeaponGeneratorConfig)))
                .Configure<AllowListConfig>(configuration.GetSection(nameof(AllowListConfig)))
                .Configure<LoreConfig>(configuration.GetSection(nameof(LoreConfig)))
                .Configure<AshOfWarConfig>(configuration.GetSection(nameof(AshOfWarConfig)))
                .Configure<IconBuilderSettings>(configuration.GetSection(nameof(IconBuilderSettings)));

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
                .AddSingleton<AllowListHandler>()
                .AddSingleton<ParamEditsRepository>()
                .AddSingleton<DSLRNetBuilder>()
                .AddSingleton<ProcessRunner>()
                .AddSingleton<ItemLotScanner>()
                .AddSingleton<IconBuilder>()
                .AddSingleton<Csv>()
                .AddSingleton((sp) =>
                {
                    return new RandomProvider(sp.GetRequiredService<IOptions<Configuration>>().Value.Settings.RandomSeed);
                });

        // Data Sources
        services.AddSingleton(sp => CreateDataSource<EquipParamWeapon>(sp, DataSourceNames.EquipParamWeapon, sp.GetRequiredService<Csv>()))
                .AddSingleton(sp => CreateDataSource<EquipParamAccessory>(sp, DataSourceNames.EquipParamAccessory, sp.GetRequiredService<Csv>()))
                .AddSingleton(sp => CreateDataSource<EquipParamGem>(sp, DataSourceNames.EquipParamGem, sp.GetRequiredService<Csv>()))
                .AddSingleton(sp => CreateDataSource<EquipParamProtector>(sp, DataSourceNames.EquipParamProtector, sp.GetRequiredService<Csv>()))
                .AddSingleton(sp => CreateDataSource<SpEffectParam>(sp, DataSourceNames.SpEffectParam, sp.GetRequiredService<Csv>()))
                .AddSingleton(sp => CreateDataSource<ItemLotParam_enemy>(sp, DataSourceNames.ItemLotParam_enemy, sp.GetRequiredService<Csv>()))
                .AddSingleton(sp => CreateDataSource<ItemLotParam_map>(sp, DataSourceNames.ItemLotParam_map, sp.GetRequiredService<Csv>()))
                .AddSingleton(sp => CreateDataSource<NpcParam>(sp, DataSourceNames.NpcParam, sp.GetRequiredService<Csv>()))
                .AddSingleton(sp => CreateDataSource<RaritySetup>(sp, DataSourceNames.RaritySetup, sp.GetRequiredService<Csv>()))
                .AddSingleton(sp => CreateDataSource<ItemLotBase>(sp, DataSourceNames.ItemLotBase, sp.GetRequiredService<Csv>()))
                .AddSingleton(sp => CreateDataSource<DamageTypeSetup>(sp, DataSourceNames.DamageTypeSetup, sp.GetRequiredService<Csv>()))
                .AddSingleton(sp => CreateDataSource<TalismanConfig>(sp, DataSourceNames.TalismanConfig, sp.GetRequiredService<Csv>()))
                .AddSingleton(sp => CreateDataSource<SpEffectConfig>(sp, DataSourceNames.SpEffectConfig, sp.GetRequiredService<Csv>()));

        return services;
    }

    public static IDataSource<T> CreateDataSource<T>(IServiceProvider provider, DataSourceNames configName, Csv csv)
        where T : class, ICloneable<T>, new()
    {
        Settings configSettings = provider.GetRequiredService<IOptions<Configuration>>().Value.Settings;

        DataSourceConfig config = configSettings.DataSourceConfigs.Single(d => d.Name == configName);

        return DataSourceFactory.CreateDataSource<T>(config, provider.GetRequiredService<RandomProvider>(), csv);
    }
}
