namespace DSLRNet.Core.Config;

using IniParser.Model;
using System;

public class IconBuilderSettings
{
    public bool RegenerateIconSheets { get; set; }

    /// <summary>
    /// Generating hi def icons and adding them to 00_solo results in great game instability.
    /// The file goes from 1.4gb to 4.8gb and the game will continually crash.
    /// Unless a whole new approach to icon backgrounds is made the compromise is that hidef icons are not available.
    /// maybe some kind of modifications to the menu gfx files that allows for a rarity layer to be rendered underneath the normal icons.
    /// </summary>
    public bool GenerateHiDefIcons { get; set; } = false;

    public IconSheetSettings IconSheetSettings { get; set; } = new IconSheetSettings();
    public void Initialize(IniData data)
    {
        var section = "Settings.IconBuilderSettings";
        if (data.Sections.ContainsSection(section))
        {
            var iconBuilderSection = data[section];
            RegenerateIconSheets = iconBuilderSection.ContainsKey("RegenerateIconSheets") && bool.Parse(iconBuilderSection["RegenerateIconSheets"]);

            IconSheetSettings = new IconSheetSettings();
            IconSheetSettings.Initialize(data);
        }
    }
}

public class IconSheetSettings
{
    public int GoalIconsPerSheet { get; set; }

    public IconDimensions IconDimensions { get; set; } = new IconDimensions();

    public int StartAt { get; set; }
    public List<RarityIconDetails> Rarities { get; set; } = [];

    public void Initialize(IniData data)
    {
        var section = "Settings.IconBuilderSettings.IconSheetSettings";
        if (data.Sections.ContainsSection(section))
        {
            var iconSheetSection = data[section];
            GoalIconsPerSheet = iconSheetSection.ContainsKey("GoalIconsPerSheet") ? int.Parse(iconSheetSection["GoalIconsPerSheet"]) : 0;
            StartAt = iconSheetSection.ContainsKey("StartAt") ? int.Parse(iconSheetSection["StartAt"]) : 0;

            IconDimensions = new IconDimensions();
            IconDimensions.Initialize(data);

            Rarities = new List<RarityIconDetails>();
            for (int i = 1; i <= 5; i++)
            {
                var raritySection = $"{section}.Rarities.Rarity{i}";
                if (data.Sections.ContainsSection(raritySection))
                {
                    var rarity = new RarityIconDetails
                    {
                        Name = data[raritySection].ContainsKey("Name") ? data[raritySection]["Name"] : string.Empty,
                        RarityIds = data[raritySection].ContainsKey("RarityIds") ? data[raritySection]["RarityIds"].Split(',').Select(int.Parse).ToList() : new List<int>(),
                        BackgroundImageName = data[raritySection].ContainsKey("BackgroundImageName") ? data[raritySection]["BackgroundImageName"] : string.Empty
                    };
                    Rarities.Add(rarity);
                }
            }
        }
    }
}

public class IconDimensions
{
    public int IconSize { get; set; }

    public int Padding { get; set; }

    public void Initialize(IniData data)
    {
        var section = "Settings.IconBuilderSettings.IconSheetSettings.IconDimensions";
        if (data.Sections.ContainsSection(section))
        {
            var iconDimSection = data[section];
            IconSize = iconDimSection.ContainsKey("IconSize") ? int.Parse(iconDimSection["IconSize"]) : 0;
            Padding = iconDimSection.ContainsKey("Padding") ? int.Parse(iconDimSection["Padding"]) : 0;
        }
    }
}

public class RarityIconDetails
{
    public string Name { get; set; } = string.Empty;

    public List<int> RarityIds { get; set; } = [];

    public string BackgroundImageName { get; set; } = string.Empty;
}
