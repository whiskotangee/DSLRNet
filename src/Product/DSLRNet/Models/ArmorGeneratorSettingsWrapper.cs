namespace DSLRNet.Models;
using DSLRNet.Core.Config;

public class ArmorGeneratorSettingsWrapper : BaseModel<ArmorGeneratorSettings>
{
    private readonly ArmorGeneratorSettings _settings;

    public ArmorGeneratorSettingsWrapper(ArmorGeneratorSettings settings)
    {
        _settings = settings;
        OriginalObject = _settings;
    }

    public string CutRateDescriptionTemplate
    {
        get => _settings.CutRateDescriptionTemplate;
        set
        {
            if (_settings.CutRateDescriptionTemplate != value)
            {
                _settings.CutRateDescriptionTemplate = value;
                OnPropertyChanged();
            }
        }
    }

    public int ResistParamBuffCount
    {
        get => _settings.ResistParamBuffCount;
        set
        {
            if (_settings.ResistParamBuffCount != value)
            {
                _settings.ResistParamBuffCount = value;
                OnPropertyChanged();
            }
        }
    }

    public int CutRateParamBuffCount
    {
        get => _settings.CutRateParamBuffCount;
        set
        {
            if (_settings.CutRateParamBuffCount != value)
            {
                _settings.CutRateParamBuffCount = value;
                OnPropertyChanged();
            }
        }
    }
}
