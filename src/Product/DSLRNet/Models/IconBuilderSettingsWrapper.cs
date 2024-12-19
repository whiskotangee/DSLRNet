
namespace DSLRNet.Models;
using DSLRNet.Core.Config;
using System.ComponentModel.DataAnnotations;

public class IconBuilderSettingsWrapper : BaseModel<IconBuilderSettings>
{
    private readonly IconBuilderSettings _settings;

    public IconBuilderSettingsWrapper(IconBuilderSettings settings)
    {
        _settings = settings;
        OriginalObject = _settings;
        IconSheetSettings = new IconSheetSettingsWrapper(_settings.IconSheetSettings);
    }

    public bool RegenerateIconSheets
    {
        get => _settings.RegenerateIconSheets;
        set
        {
            if (_settings.RegenerateIconSheets != value)
            {
                _settings.RegenerateIconSheets = value;
                OnPropertyChanged();
            }
        }
    }

    public bool GenerateHiDefIcons
    {
        get => _settings.GenerateHiDefIcons;
        set
        {
            if (_settings.GenerateHiDefIcons != value)
            {
                _settings.GenerateHiDefIcons = value;
                OnPropertyChanged();
            }
        }
    }

    public string IconSourcePath
    {
        get => _settings.IconSourcePath;
        set
        {
            if (_settings.IconSourcePath != value)
            {
                _settings.IconSourcePath = value;
                OnPropertyChanged();
            }
        }
    }

    public IconSheetSettingsWrapper IconSheetSettings { get; }
}
