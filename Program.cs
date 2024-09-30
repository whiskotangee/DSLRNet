using DSLRNet;
using DSLRNet.Config;
using DSLRNet.Contracts;
using DSLRNet.Data;
using DSLRNet.Generators;
using DSLRNet.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mods.Common;
using Serilog;
using System.Diagnostics;

Stopwatch overallTimer = Stopwatch.StartNew();

var configurationBuilder = new ConfigurationBuilder();

var jsonFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "appsettings*.json");

// Add each JSON file to the configuration builder
foreach (var jsonFile in jsonFiles)
{
    configurationBuilder.AddJsonFile(jsonFile, optional: false);
}

var configuration = configurationBuilder.Build();

IServiceCollection services = new ServiceCollection();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

services.AddLogging((builder) =>
{
    builder.AddSerilog();
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
        .AddTransient<CumulativeID>();

services.Configure<Configuration>(nameof(Configuration), configuration);

var sp = services.BuildServiceProvider();

var dslrBuilder = sp.GetRequiredService<DSLRNetBuilder>();

dslrBuilder.BuildAndApply();