﻿using DSLRNet.Core;
using DSLRNet.Core.Config;
using System.Diagnostics;

try
{
    // parse Settings.ini into an instance of Settings
    var loadedSettings = Settings.CreateFromSettingsIni()
        ?? throw new InvalidOperationException("Could not create item settings from local Settings.ini file");

    // Check for the --rescan argument
    if (args.Contains("--rescan"))
    {
        await DSLRRunner.ScanAsync(loadedSettings);

        // Open the "ScannedLots" folder
        string scannedLotsPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Data", "ItemLots", "Scanned");
        if (Directory.Exists(scannedLotsPath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = scannedLotsPath,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        else
        {
            Console.WriteLine($"The directory '{scannedLotsPath}' does not exist.");
        }
    }
    else
    {
        await DSLRRunner.Run(loadedSettings);
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());
    Process.Start(new ProcessStartInfo
    {
        FileName = "notepad.exe",
        Arguments = "Settings.User.ini"
    });
}
