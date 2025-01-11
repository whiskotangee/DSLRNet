namespace DSLRNet.Models;
using DSLRNet.Core.Config;

public class WeaponGeneratorSettingsWrapper : BaseModel<WeaponGeneratorSettings>
{
    private readonly WeaponGeneratorSettings _settings;

    public WeaponGeneratorSettingsWrapper(WeaponGeneratorSettings settings)
    {
        _settings = settings;
        OriginalObject = _settings;
        CritChanceRange = new IntValueRangeWrapper(_settings.CritChanceRange);
        PrimaryBaseScalingRange = new IntValueRangeWrapper(_settings.PrimaryBaseScalingRange);
        SecondaryBaseScalingRange = new IntValueRangeWrapper(_settings.SecondaryBaseScalingRange);
        OtherBaseScalingRange = new IntValueRangeWrapper(_settings.OtherBaseScalingRange);
    }

    public int UniqueNameChance
    {
        get => _settings.UniqueNameChance;
        set
        {
            if (_settings.UniqueNameChance != value)
            {
                _settings.UniqueNameChance = value;
                OnPropertyChanged();
            }
        }
    }

    public float UniqueWeaponMultiplier
    {
        get => _settings.UniqueWeaponMultiplier;
        set
        {
            if (_settings.UniqueWeaponMultiplier != value)
            {
                _settings.UniqueWeaponMultiplier = value;
                OnPropertyChanged();
            }
        }
    }
    public string UniqueItemNameColor
    {
        get => _settings.UniqueItemNameColor;
        set
        {
            if (_settings.UniqueItemNameColor != value)
            {
                _settings.UniqueItemNameColor = value;
                OnPropertyChanged();
            }
        }
    }
    public int SplitDamageTypeChance
    {
        get => _settings.SplitDamageTypeChance;
        set
        {
            if (_settings.SplitDamageTypeChance != value)
            {
                _settings.SplitDamageTypeChance = value;
                OnPropertyChanged();
            }
        }
    }

    public int DamageIncreasesStaminaThreshold
    {
        get => _settings.DamageIncreasesStaminaThreshold;
        set
        {
            if (_settings.DamageIncreasesStaminaThreshold != value)
            {
                _settings.DamageIncreasesStaminaThreshold = value;
                OnPropertyChanged();
            }
        }
    }

    public int StatReqReductionPercent
    {
        get => _settings.StatReqReductionPercent;
        set
        {
            if (_settings.StatReqReductionPercent != value)
            {
                _settings.StatReqReductionPercent = value;
                OnPropertyChanged();
            }
        }
    }

    public bool ApplyRarityStatReqAddition
    {
        get => _settings.ApplyRarityStatReqAddition;
        set
        {
            if (_settings.ApplyRarityStatReqAddition != value)
            {
                _settings.ApplyRarityStatReqAddition = value;
                OnPropertyChanged();
            }
        }
    }

    public IntValueRangeWrapper CritChanceRange { get; }

    public IntValueRangeWrapper PrimaryBaseScalingRange { get; }

    public IntValueRangeWrapper SecondaryBaseScalingRange { get; }

    public IntValueRangeWrapper OtherBaseScalingRange { get; }
}

