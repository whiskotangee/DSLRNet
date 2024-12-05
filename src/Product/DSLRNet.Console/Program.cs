using DSLRNet.Core;
using DSLRNet.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Diagnostics;

//string[] paramCsvFiles = Directory.GetFiles("DefaultData\\ER\\CSVs\\Params", "*.csv");
//string[] otherCsvFiles = Directory.GetFiles("DefaultData\\ER\\CSVs", "*.csv");
////foreach (var csvFile in csvFiles)
////{
////    //CsvFixer.AddNewHeaders(csvFile);
////}

//foreach (string paramCsvFile in paramCsvFiles)
//{
//    DSLRNet.Core.Data.CsvFixer.GenerateClassFromCsv(paramCsvFile, true);
//}

//foreach (string otherCsvFile in otherCsvFiles)
//{
//    DSLRNet.Core.Data.CsvFixer.GenerateClassFromCsv(otherCsvFile, false);
//}

//CsvFixer.UpdateNamesInCSVs();

Stopwatch overallTimer = Stopwatch.StartNew();

ConfigurationBuilder configurationBuilder = new();

string[] jsonFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "appsettings.Default*.json");

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

IconBuilder iconbuilder = sp.GetRequiredService<IconBuilder>();
DSLRNetBuilder dslrBuilder = sp.GetRequiredService<DSLRNetBuilder>();

await iconbuilder.ApplyIcons();

await dslrBuilder.BuildAndApply();