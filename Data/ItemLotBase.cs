
using Newtonsoft.Json;

namespace DSLRNet.Data;

public class ItemLotBase
{
    public int ID { get; set; }
    public string Name { get; set; }
    public int lotItemId01 { get; set; }
    public int lotItemId02 { get; set; }
    public int lotItemId03 { get; set; }
    public int lotItemId04 { get; set; }
    public int lotItemId05 { get; set; }
    public int lotItemId06 { get; set; }
    public int lotItemId07 { get; set; }
    public int lotItemId08 { get; set; }
    public int lotItemCategory01 { get; set; }
    public int lotItemCategory02 { get; set; }
    public int lotItemCategory03 { get; set; }
    public int lotItemCategory04 { get; set; }
    public int lotItemCategory05 { get; set; }
    public int lotItemCategory06 { get; set; }
    public int lotItemCategory07 { get; set; }
    public int lotItemCategory08 { get; set; }
    public int lotItemBasePoint01 { get; set; }
    public int lotItemBasePoint02 { get; set; }
    public int lotItemBasePoint03 { get; set; }
    public int lotItemBasePoint04 { get; set; }
    public int lotItemBasePoint05 { get; set; }
    public int lotItemBasePoint06 { get; set; }
    public int lotItemBasePoint07 { get; set; }
    public int lotItemBasePoint08 { get; set; }
    public int cumulateLotPoint01 { get; set; }
    public int cumulateLotPoint02 { get; set; }
    public int cumulateLotPoint03 { get; set; }
    public int cumulateLotPoint04 { get; set; }
    public int cumulateLotPoint05 { get; set; }
    public int cumulateLotPoint06 { get; set; }
    public int cumulateLotPoint07 { get; set; }
    public int cumulateLotPoint08 { get; set; }
    public int getItemFlagId01 { get; set; }
    public int getItemFlagId02 { get; set; }
    public int getItemFlagId03 { get; set; }
    public int getItemFlagId04 { get; set; }
    public int getItemFlagId05 { get; set; }
    public int getItemFlagId06 { get; set; }
    public int getItemFlagId07 { get; set; }
    public int getItemFlagId08 { get; set; }
    public int getItemFlagId { get; set; }
    public int cumulateNumFlagId { get; set; }
    public int cumulateNumMax { get; set; }
    public int lotItem_Rarity { get; set; }
    public int lotItemNum01 { get; set; }
    public int lotItemNum02 { get; set; }
    public int lotItemNum03 { get; set; }
    public int lotItemNum04 { get; set; }
    public int lotItemNum05 { get; set; }
    public int lotItemNum06 { get; set; }
    public int lotItemNum07 { get; set; }
    public int lotItemNum08 { get; set; }
    public int enableLuck01 { get; set; }
    public int enableLuck02 { get; set; }
    public int enableLuck03 { get; set; }
    public int enableLuck04 { get; set; }
    public int enableLuck05 { get; set; }
    public int enableLuck06 { get; set; }
    public int enableLuck07 { get; set; }
    public int enableLuck08 { get; set; }
    public int cumulateReset01 { get; set; }
    public int cumulateReset02 { get; set; }
    public int cumulateReset03 { get; set; }
    public int cumulateReset04 { get; set; }
    public int cumulateReset05 { get; set; }
    public int cumulateReset06 { get; set; }
    public int cumulateReset07 { get; set; }
    public int cumulateReset08 { get; set; }
    public int GameClearOffset { get; set; }
    public int canExecByFriendlyGhost { get; set; }
    public int canExecByHostileGhost { get; set; }
    public int PAD1 { get; set; }
    public int PAD2 { get; set; }

    public void SetPropertyByName(string name, object value)
    {
        this.GetType().GetProperty(name).SetValue(this, value);
    }

    public T GetValue<T>(string propertyName)
    {
        return (T)this.GetType().GetProperty(propertyName).GetValue(this);
    }
}