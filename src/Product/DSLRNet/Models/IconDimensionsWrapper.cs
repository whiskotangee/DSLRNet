namespace DSLRNet.Models;
using DSLRNet.Core.Config;

public class IconDimensionsWrapper : BaseModel<IconDimensions>
{
    private readonly IconDimensions _dimensions;

    public IconDimensionsWrapper(IconDimensions dimensions)
    {
        _dimensions = dimensions;
        OriginalObject = _dimensions;
    }

    public int IconSize
    {
        get => _dimensions.IconSize;
        set
        {
            if (_dimensions.IconSize != value)
            {
                _dimensions.IconSize = value;
                OnPropertyChanged();
            }
        }
    }

    public int Padding
    {
        get => _dimensions.Padding;
        set
        {
            if (_dimensions.Padding != value)
            {
                _dimensions.Padding = value;
                OnPropertyChanged();
            }
        }
    }
}
