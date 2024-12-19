using DSLRNet.Core;
using DSLRNet.Core.Config;

// parse Settings.ini into an instance of Settings
var loadedSettings = Settings.CreateFromSettingsIni();

await DSLRRunner.Run(loadedSettings);