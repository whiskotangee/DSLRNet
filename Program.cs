using DotNext.Collections.Generic;
using DSLRNet;
using DSLRNet.Config;
using DSLRNet.Contracts;
using DSLRNet.Data;
using DSLRNet.Generators;
using DSLRNet.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mods.Common;
using Newtonsoft.Json;
using Serilog;
using SoulsFormats;
using System.Diagnostics;

// TODO: dynamically read itemlot_param and don't overwrite existing mapped drops

string[] csvFiles = Directory.GetFiles("O:\\EldenRingShitpostEdition\\Tools\\DSLRNet\\DefaultData\\ER\\CSVs\\", "*.csv");

foreach (var csvFile in csvFiles)
{
    CsvFixer.AddNewHeaders(csvFile);
}

foreach (string csvFile in csvFiles)
{
    CsvFixer.GenerateClassFromCsv(csvFile);
}

CsvFixer.UpdateNamesInCSVs();

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
        .Configure<WhiteListConfig>(configuration.GetSection(nameof(WhiteListConfig)))
        .Configure<LoreConfig>(configuration.GetSection(nameof(LoreConfig)));

services.AddSingleton<RandomNumberGetter>((sp) =>
        {
            return new RandomNumberGetter(sp.GetRequiredService<IOptions<Configuration>>().Value.Settings.RandomSeed);
        })
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
        .AddSingleton<ProcessRunner>();

ServiceProvider sp = services.BuildServiceProvider();

DSLRNetBuilder dslrBuilder = sp.GetRequiredService<DSLRNetBuilder>();

await dslrBuilder.BuildAndApply();


public class NpcParamFinder
{
    public static Dictionary<int, List<int>> GetNpcIdsByModelId()
    {
        var gameDir = "O:\\Steam\\SteamApps\\Common\\Elden Ring\\Game\\map\\mapstudio";
        var workDir = "O:\\EldenRingShitpostEdition\\work\\npcfinder";

        Directory.CreateDirectory(workDir);

        Directory.GetFiles(gameDir, "*.msb.dcx")
            .ToList()
            .ForEach(d => File.Copy(d, Path.Combine(workDir, Path.GetFileName(d)), true));

        var returnDictionary = new Dictionary<int, List<int>>();

        var mapStudioFiles = Directory.GetFiles(workDir, "*.msb.dcx");

        foreach(var mapFile in mapStudioFiles)
        {
            var bnd = DCX.Decompress(mapFile);

            MSBE msb = MSBE.Read(bnd.Span.ToArray());
            if (msb.Parts.Enemies.Any())
            {
                foreach(var enemy in msb.Parts.Enemies)
                {
                    int modelNumber = int.Parse(enemy.ModelName.Substring(1));

                    if (modelNumber >= 2000 && modelNumber <= 6000 && enemy.NPCParamID > 100)
                    {
                        if (!returnDictionary.TryGetValue(modelNumber, out List<int> ids))
                        {
                            returnDictionary[modelNumber] = [enemy.NPCParamID];
                        }
                        else
                        {
                            if (!ids.Contains(enemy.NPCParamID))
                            {
                                ids.Add(enemy.NPCParamID);
                            }
                        }
                    }
                }
            }
        }

        return returnDictionary;
    }
}