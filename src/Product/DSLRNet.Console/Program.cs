using DSLRNet.Core;
using DSLRNet.Core.Common;
using DSLRNet.Core.Config;
using DSLRNet.Core.Data;
using DSLRNet.Core.Extensions;
using DSLRNet.Core.Generators;
using DSLRNet.Core.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using System.Diagnostics;

// TODO console progress like shitpost edition builder

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

string[] jsonFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "appsettings.Default.*.json");

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

services.InitializeDSLR(configuration);

ServiceProvider sp = services.BuildServiceProvider();

DSLRNetBuilder dslrBuilder = sp.GetRequiredService<DSLRNetBuilder>();

await dslrBuilder.BuildAndApply();