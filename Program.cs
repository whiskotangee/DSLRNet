using DSLRNet;
using DSLRNet.Config;
using DSLRNet.Data;
using DSLRNet.Generators;
using DSLRNet.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mods.Common;
using Serilog;
using System.Diagnostics;

//TODO: dynamically read itemlot_param and don't overwrite existing mapped drops

//string[] csvFiles = Directory.GetFiles("O:\\EldenRingShitpostEdition\\Tools\\DSLRNet\\DefaultData\\ER\\CSVs\\", "*.csv");

//foreach (var csvFile in csvFiles)
//{
//    //CsvFixer.AddNewHeaders(csvFile);
//}

//foreach (string csvFile in csvFiles)
//{
//    CsvFixer.GenerateClassFromCsv(csvFile);
//}

//CsvFixer.UpdateNamesInCSVs();

//var ret = NpcParamFinder.GetNpcIdsByModelId();

//File.WriteAllText("npcmappings.json", JsonConvert.SerializeObject(ret));

Stopwatch overallTimer = Stopwatch.StartNew();

ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();

string[] jsonFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "appsettings*.json");

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
    builder.AddConsole();
});

services.Configure<Configuration>(configuration.GetSection(nameof(Configuration)))
        .Configure<WeaponGeneratorConfig>(configuration.GetSection(nameof(WeaponGeneratorConfig)))
        .Configure<AllowListConfig>(configuration.GetSection(nameof(AllowListConfig)))
        .Configure<LoreConfig>(configuration.GetSection(nameof(LoreConfig)))
        .Configure<AshOfWarConfig>(configuration.GetSection(nameof(AshOfWarConfig)));

services.AddSingleton<RandomNumberGetter>((sp) =>
        {
            return new RandomNumberGetter(sp.GetRequiredService<IOptions<Configuration>>().Value.Settings.RandomSeed);
        })
        .AddSingleton<ArmorLootGenerator>()
        .AddSingleton<WeaponLootGenerator>()
        .AddSingleton<TalismanLootGenerator>()
        .AddSingleton<ItemLotGenerator>()
        .AddSingleton<LoreGenerator>()
        .AddSingleton<AshofWarHandler>()
        .AddSingleton<DamageTypeHandler>()
        .AddSingleton<RarityHandler>()
        .AddSingleton<SpEffectHandler>()
        .AddSingleton<AllowListHandler>()
        .AddSingleton<DataRepository>()
        .AddSingleton<DSLRNetBuilder>()
        .AddSingleton<ProcessRunner>();

ServiceProvider sp = services.BuildServiceProvider();

DSLRNetBuilder dslrBuilder = sp.GetRequiredService<DSLRNetBuilder>();

await dslrBuilder.BuildAndApply();