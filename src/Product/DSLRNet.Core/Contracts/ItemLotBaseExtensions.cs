namespace DSLRNet.Core.Contracts;

using DSLRNet.Core.Contracts.Params;

public static class ItemLotBaseExtensions
{
    public static ItemLotBase CloneToBase(this ItemLotParam_enemy enemyItemLot)
    {
        return JsonConvert.DeserializeObject<ItemLotBase>(JsonConvert.SerializeObject(enemyItemLot))
            ?? throw new Exception("Could not clone enemy item lot to base item lot");
    }

    public static int GetIndexOfFirstOpenLotItemId(this ItemLotBase itemLot)
    {
        var itemIdFieldNames = itemLot.GetFieldNamesByFilter("lotItemId0");
        var itemDropChanceFieldNames = itemLot.GetFieldNamesByFilter("lotItemBasePoint0");

        for (int i = 0; i < itemIdFieldNames.Count; i++)
        {
            if (itemLot.GetValue<int>(itemIdFieldNames[i]) == 0 && itemLot.GetValue<int>(itemDropChanceFieldNames[i]) == 0)
            {
                return i + 1;
            }
        }

        return -1;
    }
}
