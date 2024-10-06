using DSLRNet;
using DSLRNet.Config;
using DSLRNet.Contracts;
using DSLRNet.Data;
using DSLRNet.Generators;
using DSLRNet.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mods.Common;
using Serilog;
using System.Diagnostics;

//string[] csvFiles = Directory.GetFiles("O:\\EldenRingShitpostEdition\\Tools\\DSLRNet\\DefaultData\\ER\\CSVs\\", "*.csv");

////foreach (var csvFile in csvFiles)
////{
////    CsvFixer.AddNewHeaders(csvFile);
////}

//foreach (string csvFile in csvFiles)
//{
//    CsvFixer.GenerateClassFromCsv(csvFile);
//}

//CsvFixer.UpdateNamesInCSVs();

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
        .Configure<WhiteListConfig>(configuration.GetSection(nameof(WhiteListConfig)))
        .Configure<LoreConfig>(configuration.GetSection(nameof(LoreConfig)));

services.AddSingleton<RandomNumberGetter>()
        .AddSingleton<ArmorLootGenerator>()
        .AddSingleton<WeaponLootGenerator>()
        .AddSingleton<TalismanLootGenerator>()
        .AddSingleton<ItemLotGenerator>()
        .AddSingleton<LoreGenerator>()
        .AddSingleton<AcquisitionFlagHandler>()
        .AddSingleton<AshofWarHandler>()
        .AddSingleton<DamageTypeHandler>()
        .AddSingleton<RarityHandler>()
        .AddSingleton<SpEffectHandler>()
        .AddSingleton<WhiteListHandler>()
        .AddSingleton<DataRepository>()
        .AddSingleton<DSLRNetBuilder>()
        .AddSingleton<ProcessRunner>()
        .AddTransient<CumulativeID>();

ServiceProvider sp = services.BuildServiceProvider();

DSLRNetBuilder dslrBuilder = sp.GetRequiredService<DSLRNetBuilder>();

await dslrBuilder.BuildAndApply();