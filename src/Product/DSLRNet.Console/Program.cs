using DSLRNet.Core;
using DSLRNet.Core.Config;

// parse Settings.ini into an instance of Settings
var loadedSettings = Settings.CreateFromSettingsIni()
    ?? throw new InvalidOperationException("Could not create item settings from local Settings.ini file");

await DSLRRunner.Run(loadedSettings);