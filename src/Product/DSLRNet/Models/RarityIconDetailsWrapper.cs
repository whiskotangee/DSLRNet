namespace DSLRNet.Models;
using global::DSLRNet.Core.Config;

public class RarityIconDetailsWrapper : BaseModel<RarityIconDetails>
{
    private readonly RarityIconDetails _rarity;

    public RarityIconDetailsWrapper(RarityIconDetails rarity)
    {
        _rarity = rarity;
        OriginalObject = _rarity;
    }

    public string RarityIds
    {
        get => string.Join(",", _rarity.RarityIds);
        set
        {
            if (string.Join(",", _rarity.RarityIds) != value)
            {
                _rarity.RarityIds = value.Split(",").Select(d => int.Parse(d)).ToList();
                OnPropertyChanged();
            }
        }
    }

    public string BackgroundImageName
    {
        get => _rarity.BackgroundImageName;
        set
        {
            if (_rarity.BackgroundImageName != value)
            {
                _rarity.BackgroundImageName = value;
                OnPropertyChanged();
            }
        }
    }
}
