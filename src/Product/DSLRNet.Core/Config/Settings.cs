namespace DSLRNet.Core.Config;

using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

public class Settings
{
    public string DeployPath { get; set; }

    public ItemLotGeneratorSettings ItemLotGeneratorSettings { get; set; }

    public int RandomSeed { get; set; }

    public List<string> MessageSourcePaths { get; set; } = [];

    public string GamePath { get; set; }

    public List<string> MessageFileNames { get; set; }

    public ArmorGeneratorSettings ArmorGeneratorSettings { get; set; }

    public WeaponGeneratorSettings WeaponGeneratorSettings { get; set; }

    public IconBuilderSettings IconBuilderSettings { get; set; }

    public static Settings? CreateFromSettingsIni()
    {
        IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddIniFile("Settings.ini", optional: true);

        IConfigurationRoot configuration = configurationBuilder.Build();

        return configuration.Get<Settings>();
    }
}

