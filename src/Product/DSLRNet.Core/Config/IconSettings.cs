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
        if (IconDimensions == null)
        {
            return;
        }

        int width = iconsPerRow * IconDimensions.IconSize + (iconsPerRow - 1) * IconDimensions.Padding;
        int height = (width / 2) * rowsPerSheet;

        IconSheetSize = new Size(width, height);
    }
}

public class IconDimensions
{
    public int IconSize{ get; set; }

    public int Padding { get; set; }
}
