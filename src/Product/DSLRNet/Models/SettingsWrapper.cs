namespace DSLRNet.Models;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DSLRNet.Core.Config;

public class SettingsWrapper : BaseModel<Settings>
{
    private readonly Settings _settings;

    public SettingsWrapper(Settings settings)
    {
        _settings = settings;
        OriginalObject = _settings;
        ItemLotGeneratorSettings = new ItemLotGeneratorSettingsWrapper(_settings.ItemLotGeneratorSettings);
        ArmorGeneratorSettings = new ArmorGeneratorSettingsWrapper(_settings.ArmorGeneratorSettings);
        WeaponGeneratorSettings = new WeaponGeneratorSettingsWrapper(_settings.WeaponGeneratorSettings);
        IconBuilderSettings = new IconBuilderSettingsWrapper(_settings.IconBuilderSettings);
    }

    [Required(ErrorMessage = "Mod Path is required.")]
    public string DeployPath
    {
        get => _settings.DeployPath;
        set
        {
            if (_settings.DeployPath != value)
            {
                _settings.DeployPath = value;
                OnPropertyChanged();
            }
        }
    }

    public ItemLotGeneratorSettingsWrapper ItemLotGeneratorSettings { get; }

    public int RandomSeed
    {
        get => _settings.RandomSeed;
        set
        {
            if (_settings.RandomSeed != value)
            {
                _settings.RandomSeed = value;
                OnPropertyChanged();
            }
        }
    }

    [Required(ErrorMessage = "Game Path is required.")]
    public string GamePath
    {
        get => _settings.GamePath;
        set
        {
            if (_settings.GamePath != value)
            {
                _settings.GamePath = value;
                OnPropertyChanged();
            }
        }
    }

    public List<string> MessageFileNames
    {
        get => _settings.MessageFileNames;
        set
        {
            if (_settings.MessageFileNames != value)
            {
                _settings.MessageFileNames = value;
                OnPropertyChanged();
            }
        }
    }

    public ArmorGeneratorSettingsWrapper ArmorGeneratorSettings { get; }

    public WeaponGeneratorSettingsWrapper WeaponGeneratorSettings { get; }

    public IconBuilderSettingsWrapper IconBuilderSettings { get; }
}
