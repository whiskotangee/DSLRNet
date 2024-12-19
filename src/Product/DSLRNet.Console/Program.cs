using DSLRNet.Core;
using DSLRNet.Core.Config;
using Microsoft.Extensions.Configuration;

// parse Settings.ini into an instance of Settings

IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
configurationBuilder.AddIniFile("Settings.ini", optional: true);

IConfigurationRoot configuration = configurationBuilder.Build();

await DSLRRunner.Run(configuration.Get<Settings>());