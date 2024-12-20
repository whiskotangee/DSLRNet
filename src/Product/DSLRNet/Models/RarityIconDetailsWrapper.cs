namespace DSLRNet.Models;

using System.Collections.Generic;
using System.IO;
using global::DSLRNet.Core.Config;

public class RarityIconDetailsWrapper : BaseModel<RarityIconDetails>
{
    private readonly RarityIconDetails _rarity;

    public RarityIconDetailsWrapper(RarityIconDetails rarity)
    {
        _rarity = rarity;
        OriginalObject = _rarity;
    }

    public List<int> RarityIds
    {
        get => _rarity.RarityIds;
        set
        {
            if (_rarity.RarityIds != value)
            {
                _rarity.RarityIds = value;
                OnPropertyChanged();
            }
        }
    }

    public string BackgroundImageName
    {
        get => Path.Combine("Assets", "LootIcons", _rarity.BackgroundImageName);
        set
        {
            if (_rarity.BackgroundImageName != value)
            {
                _rarity.BackgroundImageName = Path.GetFileNameWithoutExtension(value);
                OnPropertyChanged();
            }
        }
    }
}
