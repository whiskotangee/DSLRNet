namespace DSLRNet.Core.Extensions;

using DSLRNet.Core.DAL;
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
                .Configure<ArmorGeneratorConfig>(configuration.GetSection(nameof(ArmorGeneratorConfig)))
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
                .AddSingleton<RegulationBinReader>()
                .AddSingleton<DataSourceFactory>()
                .AddSingleton((sp) =>
                {
                    return new RandomProvider(sp.GetRequiredService<IOptions<Configuration>>().Value.Settings.RandomSeed);
                });

        // Data Sources
        services.AddSingleton(sp => CreateDataSource<EquipParamWeapon>(sp, DataSourceNames.EquipParamWeapon))
                .AddSingleton(sp => CreateDataSource<EquipParamAccessory>(sp, DataSourceNames.EquipParamAccessory))
                .AddSingleton(sp => CreateDataSource<EquipParamGem>(sp, DataSourceNames.EquipParamGem))
                .AddSingleton(sp => CreateDataSource<EquipParamProtector>(sp, DataSourceNames.EquipParamProtector))
                .AddSingleton(sp => CreateDataSource<SpEffectParam>(sp, DataSourceNames.SpEffectParam))
                .AddSingleton(sp => CreateDataSource<ItemLotParam_enemy>(sp, DataSourceNames.ItemLotParam_enemy))
                .AddSingleton(sp => CreateDataSource<ItemLotParam_map>(sp, DataSourceNames.ItemLotParam_map))
                .AddSingleton(sp => CreateDataSource<NpcParam>(sp, DataSourceNames.NpcParam))
                .AddSingleton(sp => CreateDataSource<RaritySetup>(sp, DataSourceNames.RaritySetup))
                .AddSingleton(sp => CreateDataSource<ItemLotBase>(sp, DataSourceNames.ItemLotBase))
                .AddSingleton(sp => CreateDataSource<DamageTypeSetup>(sp, DataSourceNames.DamageTypeSetup))
                .AddSingleton(sp => CreateDataSource<TalismanConfig>(sp, DataSourceNames.TalismanConfig))
                .AddSingleton(sp => CreateDataSource<SpEffectConfig>(sp, DataSourceNames.SpEffectConfig));

        return services;
    }

    public static IDataSource<T> CreateDataSource<T>(IServiceProvider provider, DataSourceNames configName)
        where T : ParamBase<T>, ICloneable<T>, new()
    {
        Settings configSettings = provider.GetRequiredService<IOptions<Configuration>>().Value.Settings;

        DataSourceConfig config = configSettings.DataSourceConfigs.Single(d => d.Name == configName);

        var factory = provider.GetRequiredService<DataSourceFactory>();

        return factory.CreateDataSource<T>(config);
    }
}
