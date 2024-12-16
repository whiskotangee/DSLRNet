namespace DSLRNet.Core.DAL;

using DSLRNet.Core.Contracts;

public class DataAccess
{
    private readonly ILogger<DataAccess> logger;
    private readonly IOptionsMonitor<Configuration> config;
    private readonly DataSourceFactory dataSourceFactory;

    public DataAccess(
        ILogger<DataAccess> logger,
        IOptionsMonitor<Configuration> config,
        DataSourceFactory dataSourceFactory)
    {
        this.logger = logger;
        this.config = config;
        this.dataSourceFactory = dataSourceFactory;

        logger.LogInformation($"Creating data sources");

        // Initialize data sources with the factory
        EquipParamWeapon = CreateDataSource<EquipParamWeapon>(DataSourceNames.EquipParamWeapon);
        EquipParamCustomWeapon = CreateDataSource<EquipParamCustomWeapon>(DataSourceNames.EquipParamCustomWeapon);
        EquipParamAccessory = CreateDataSource<EquipParamAccessory>(DataSourceNames.EquipParamAccessory);
        EquipParamGem = CreateDataSource<EquipParamGem>(DataSourceNames.EquipParamGem);
        EquipParamProtector = CreateDataSource<EquipParamProtector>(DataSourceNames.EquipParamProtector);
        SpEffectParam = CreateDataSource<SpEffectParam>(DataSourceNames.SpEffectParam);
        SpEffectParamNew = CreateDataSource<SpEffectParamNew>(DataSourceNames.SpEffectParamNew);
        ItemLotParamEnemy = CreateDataSource<ItemLotParam_enemy>(DataSourceNames.ItemLotParam_enemy);
        ItemLotParamMap = CreateDataSource<ItemLotParam_map>(DataSourceNames.ItemLotParam_map);
        NpcParam = CreateDataSource<NpcParam>(DataSourceNames.NpcParam);
        RaritySetup = CreateDataSource<RaritySetup>(DataSourceNames.RaritySetup);
        ItemLotBase = CreateDataSource<ItemLotBase>(DataSourceNames.ItemLotBase);
        DamageTypeSetup = CreateDataSource<DamageTypeSetup>(DataSourceNames.DamageTypeSetup);
        TalismanConfig = CreateDataSource<TalismanConfig>(DataSourceNames.TalismanConfig);
        SpEffectConfig = CreateDataSource<SpEffectConfig>(DataSourceNames.SpEffectConfig);
    }

    public IDataSource<EquipParamWeapon> EquipParamWeapon { get; }
    public IDataSource<EquipParamCustomWeapon> EquipParamCustomWeapon { get; }
    public IDataSource<EquipParamAccessory> EquipParamAccessory { get; }
    public IDataSource<EquipParamGem> EquipParamGem { get; }
    public IDataSource<EquipParamProtector> EquipParamProtector { get; }
    public IDataSource<SpEffectParam> SpEffectParam { get; }
    public IDataSource<SpEffectParamNew> SpEffectParamNew { get; }
    public IDataSource<ItemLotParam_enemy> ItemLotParamEnemy { get; }
    public IDataSource<ItemLotParam_map> ItemLotParamMap { get; }
    public IDataSource<NpcParam> NpcParam { get; }
    public IDataSource<RaritySetup> RaritySetup { get; }
    public IDataSource<ItemLotBase> ItemLotBase { get; }
    public IDataSource<DamageTypeSetup> DamageTypeSetup { get; }
    public IDataSource<TalismanConfig> TalismanConfig { get; }
    public IDataSource<SpEffectConfig> SpEffectConfig { get; }

    public async Task InitializeDataSourcesAsync()
    {
        logger.LogInformation($"Initializing data sources");

        await EquipParamCustomWeapon.InitializeDataAsync();

        List<Task> tasks =
        [
            EquipParamWeapon.InitializeDataAsync(EquipParamCustomWeapon.GetAll().Select(d => d.baseWepId).Distinct()),
            EquipParamAccessory.InitializeDataAsync(),
            EquipParamGem.InitializeDataAsync(),
            EquipParamProtector.InitializeDataAsync(),
            SpEffectParam.InitializeDataAsync(),
            SpEffectParamNew.InitializeDataAsync(),
            ItemLotParamEnemy.InitializeDataAsync(),
            ItemLotParamMap.InitializeDataAsync(),
            NpcParam.InitializeDataAsync(),
            RaritySetup.InitializeDataAsync(),
            ItemLotBase.InitializeDataAsync(),
            DamageTypeSetup.InitializeDataAsync(),
            TalismanConfig.InitializeDataAsync(),
            SpEffectConfig.InitializeDataAsync()
        ];

        await Task.WhenAll(tasks);
    }

    private IDataSource<T> CreateDataSource<T>(DataSourceNames configName)
        where T : ParamBase<T>, ICloneable<T>, new()
    {
        DataSourceConfig config = this.config.CurrentValue.Settings.DataSourceConfigs.Single(d => d.Name == configName);
        var dataSource = this.dataSourceFactory.CreateDataSource<T>(config);

        return dataSource;
    }
}
