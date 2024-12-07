namespace DSLRNet.Core.Config;

using SixLabors.ImageSharp;

public class IconBuilderSettings
{
    public bool RegenerateIconSheets { get; set; }

    public bool GenerateHiDefIcons { get; set; }

    public string IconSourcePath { get; set; }

    public string ModSourcePath { get; set; }

    public IconDimensions HiDefIconDimensions { get; set; }

    public required IconSheetSettings IconSheetSettings { get; set; }
}

public class IconSheetSettings
{
    private int rowsPerSheet;
    private int iconsPerRow;
    private IconDimensions iconDimensions;

    public int RowsPerSheet
    {
        get => rowsPerSheet;
        set
        {
            rowsPerSheet = value;
            CalculateIconSheetSize();
        }
    }

    public int IconsPerRow
    {
        get => iconsPerRow;
        set
        {
            iconsPerRow = value;
            CalculateIconSheetSize();
        }
    }

    public Size IconSheetSize { get; private set; }

    public IconDimensions IconDimensions
    {
        get => iconDimensions;
        set
        {
            iconDimensions = value;
            CalculateIconSheetSize();
        }
    }

    private void CalculateIconSheetSize()
    {
        if (IconDimensions == null) { return; }
        int width = iconsPerRow * IconDimensions.IconSize + (iconsPerRow - 1) * IconDimensions.Padding;
        int height = rowsPerSheet * IconDimensions.IconSize;
        // Adjust to the nearest power of two
        width = NextPowerOfTwo(width);
        height = NextPowerOfTwo(height);
        IconSheetSize = new Size(width, height);
    }
    private int NextPowerOfTwo(int value)
    {
        if (value < 1) 
            return 1; 
        value--; 
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        return value + 1;
    }

    public int StartAt { get; set; }
    public List<RarityIconDetails> Rarities { get; set; }

    public List<int> WeaponIcons { get; set; }
    public List<int> ArmorIcons { get; set; }

    public List<int> TalismanIcons { get; set; }
}

public class IconDimensions
{
    public int IconSize { get; set; }

    public int Padding { get; set; }
}

public class RarityIconDetails
{
    public List<int> RarityIds { get; set; }

    public string BackgroundImageName { get; set; }
}
