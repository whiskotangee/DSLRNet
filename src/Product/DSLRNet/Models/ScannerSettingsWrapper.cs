namespace DSLRNet.Models;
using DSLRNet.Core.Config;

public class ScannerSettingsWrapper : BaseModel<ScannerSettings>
{
    private readonly ScannerSettings _settings;

    public ScannerSettingsWrapper(ScannerSettings settings)
    {
        _settings = settings;
        OriginalObject = _settings;
    }

    public bool Enabled
    {
        get => _settings.Enabled;
        set
        {
            if (_settings.Enabled != value)
            {
                _settings.Enabled = value;
                OnPropertyChanged();
            }
        }
    }

    public int ApplyPercent
    {
        get => _settings.ApplyPercent;
        set
        {
            if (_settings.ApplyPercent != value)
            {
                _settings.ApplyPercent = value;
                OnPropertyChanged();
            }
        }
    }
}
