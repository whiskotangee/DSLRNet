namespace DSLRNet.Core.Extensions;

using DSLRNet.Core.DAL;
using DSLRNet.Core.Generators;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public static class IServiceCollectionExtensions
{

    public static async Task<IServiceCollection> SetupDSLRAsync(this IServiceCollection services, IConfiguration configuration)
    {
        IServiceCollection serviceCollection = new ServiceCollection();

        serviceCollection.Configure<Configuration>(configuration.GetSection(nameof(Configuration)));

        serviceCollection.AddLogging((builder) =>
        {
            builder.AddSerilog();
        });
        serviceCollection.AddSingleton<Csv>();
        serviceCollection.AddSingleton<RegulationBinReader>();
        serviceCollection.AddSingleton<DataSourceFactory>()
                            .AddSingleton((sp) =>
                            {
                                return new RandomProvider(sp.GetRequiredService<IOptions<Configuration>>().Value.Settings.RandomSeed);
                            });

        var factoryServiceProvider = serviceCollection.BuildServiceProvider();
        var csv = factoryServiceProvider.GetRequiredService<Csv>();
        var regulationBinReader = factoryServiceProvider.GetRequiredService<Csv>();
        var randomProvider = factoryServiceProvider.GetRequiredService<RandomProvider>();
        var factory = factoryServiceProvider.GetRequiredService<DataSourceFactory>();
        var configSettings = factoryServiceProvider.GetRequiredService<IOptions<Configuration>>().Value;


        // Initialize data sources with the factory
        var equipParamWeapon = CreateDataSource<EquipParamWeapon>(factory, DataSourceNames.EquipParamWeapon, configSettings);
        var equipParamCustomWeapon = CreateDataSource<EquipParamCustomWeapon>(factory, DataSourceNames.EquipParamCustomWeapon, configSettings);
        var equipParamAccessory = CreateDataSource<EquipParamAccessory>(factory, DataSourceNames.EquipParamAccessory, configSettings);
        var equipParamGem = CreateDataSource<EquipParamGem>(factory, DataSourceNames.EquipParamGem, configSettings);
        var equipParamProtector = CreateDataSource<EquipParamProtector>(factory, DataSourceNames.EquipParamProtector, configSettings);
        var spEffectParam = CreateDataSource<SpEffectParam>(factory, DataSourceNames.SpEffectParam, configSettings);
        var spEffectParamNew = CreateDataSource<SpEffectParamNew>(factory, DataSourceNames.SpEffectParamNew, configSettings);
        var itemLotParamEnemy = CreateDataSource<ItemLotParam_enemy>(factory, DataSourceNames.ItemLotParam_enemy, configSettings);
        var itemLotParamMap = CreateDataSource<ItemLotParam_map>(factory, DataSourceNames.ItemLotParam_map, configSettings);
        var npcParam = CreateDataSource<NpcParam>(factory, DataSourceNames.NpcParam, configSettings);
        var raritySetup = CreateDataSource<RaritySetup>(factory, DataSourceNames.RaritySetup, configSettings);
        var itemLotBase = CreateDataSource<ItemLotBase>(factory, DataSourceNames.ItemLotBase, configSettings);
        var damageTypeSetup = CreateDataSource<DamageTypeSetup>(factory, DataSourceNames.DamageTypeSetup, configSettings);
        var talismanConfig = CreateDataSource<TalismanConfig>(factory, DataSourceNames.TalismanConfig, configSettings);
        var spEffectConfig = CreateDataSource<SpEffectConfig>(factory, DataSourceNames.SpEffectConfig, configSettings);

        // List to hold all initialization tasks
        List<Task> tasks =
        [
            equipParamCustomWeapon.InitializeDataAsync(),
            equipParamAccessory.InitializeDataAsync(),
            equipParamGem.InitializeDataAsync(), 
            equipParamProtector.InitializeDataAsync(), 
            spEffectParam.InitializeDataAsync(),
            spEffectParamNew.InitializeDataAsync(),
            itemLotParamEnemy.InitializeDataAsync(), 
            itemLotParamMap.InitializeDataAsync(), 
            npcParam.InitializeDataAsync(), 
            raritySetup.InitializeDataAsync(), 
            itemLotBase.InitializeDataAsync(), 
            damageTypeSetup.InitializeDataAsync(), 
            talismanConfig.InitializeDataAsync(), 
            spEffectConfig.InitializeDataAsync() 
        ]; 

        // Await all tasks
        await Task.WhenAll(tasks);

        await equipParamWeapon.InitializeDataAsync(equipParamCustomWeapon.GetAll().Select(d => d.baseWepId).Distinct());

        // Add initialized data sources to service collection
        services.AddSingleton(equipParamWeapon)
            .AddSingleton(equipParamCustomWeapon)
            .AddSingleton(equipParamAccessory)
            .AddSingleton(equipParamGem)
            .AddSingleton(equipParamProtector)
            .AddSingleton(spEffectParam)
            .AddSingleton(spEffectParamNew)
            .AddSingleton(itemLotParamEnemy)
            .AddSingleton(itemLotParamMap)
            .AddSingleton(npcParam)
            .AddSingleton(raritySetup)
            .AddSingleton(itemLotBase)
            .AddSingleton(damageTypeSetup)
            .AddSingleton(talismanConfig)
            .AddSingleton(spEffectConfig)
            .AddSingleton(csv)
            .AddSingleton(regulationBinReader)
            .AddSingleton(randomProvider);

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
                .AddSingleton<IconBuilder>();

        return services;
    }

    public static IDataSource<T> CreateDataSource<T>(DataSourceFactory factory, DataSourceNames configName, Configuration configSettings)
        where T : ParamBase<T>, ICloneable<T>, new()
    {
        DataSourceConfig config = configSettings.Settings.DataSourceConfigs.Single(d => d.Name == configName);
        var dataSource = factory.CreateDataSource<T>(config);

        return dataSource;
    }
}
