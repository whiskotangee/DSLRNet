namespace DSLRNet.Models;

using System.Collections.Generic;
using DSLRNet.Core.Config;

public class IconSheetSettingsWrapper : BaseModel<IconSheetSettings>
{
    private readonly IconSheetSettings _settings;

    public IconSheetSettingsWrapper(IconSheetSettings settings)
    {
        _settings = settings;
        OriginalObject = _settings;
        IconDimensions = new IconDimensionsWrapper(_settings.IconDimensions);
        Rarities = new List<RarityIconDetailsWrapper>();
        foreach (var rarity in _settings.Rarities)
        {
            Rarities.Add(new RarityIconDetailsWrapper(rarity));
        }
    }

    public int GoalIconsPerSheet
    {
        get => _settings.GoalIconsPerSheet;
        set
        {
            if (_settings.GoalIconsPerSheet != value)
            {
                _settings.GoalIconsPerSheet = value;
                OnPropertyChanged();
            }
        }
    }

    public IconDimensionsWrapper IconDimensions { get; }

    public int StartAt
    {
        get => _settings.StartAt;
        set
        {
            if (_settings.StartAt != value)
            {
                _settings.StartAt = value;
                OnPropertyChanged();
            }
        }
    }

    public List<RarityIconDetailsWrapper> Rarities { get; }
}
