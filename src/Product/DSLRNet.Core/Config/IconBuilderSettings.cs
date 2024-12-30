namespace DSLRNet.Core.Config;

public class IconBuilderSettings
{
    public bool RegenerateIconSheets { get; set; }

    /// <summary>
    /// Generating hi def icons and adding them to 00_solo results in great game instability.
    /// The file goes from 1.4gb to 4.8gb and the game will continually crash.
    /// Unless a whole new approach to icon backgrounds is made the compromise is that hidef icons are not available.
    /// </summary>
    public bool GenerateHiDefIcons { get; set; } = false;

    public required IconSheetSettings IconSheetSettings { get; set; }
}

public class IconSheetSettings
{
    public int GoalIconsPerSheet { get; set; }

    public IconDimensions IconDimensions { get; set; } = new IconDimensions();

    public int StartAt { get; set; }
    public List<RarityIconDetails> Rarities { get; set; } = [];
}

public class IconDimensions
{
    public int IconSize { get; set; }

    public int Padding { get; set; }
}

public class RarityIconDetails
{
    public List<int> RarityIds { get; set; } = [];

    public string BackgroundImageName { get; set; } = string.Empty;
}
