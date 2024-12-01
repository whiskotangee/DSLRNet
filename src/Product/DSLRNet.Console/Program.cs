using DSLRNet.Core;
using DSLRNet.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Diagnostics;

// TODO console progress like shitpost edition builder

string[] csvFiles = Directory.GetFiles("DefaultData\\ER\\CSVs\\Params", "*.csv");

//foreach (var csvFile in csvFiles)
//{
//    //CsvFixer.AddNewHeaders(csvFile);
//}

foreach (string csvFile in csvFiles)
{
    DSLRNet.Core.Data.CsvFixer.GenerateClassFromCsv(csvFile);
}

//CsvFixer.UpdateNamesInCSVs();

Stopwatch overallTimer = Stopwatch.StartNew();

ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();

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

DSLRNetBuilder dslrBuilder = sp.GetRequiredService<DSLRNetBuilder>();

await dslrBuilder.BuildAndApply();