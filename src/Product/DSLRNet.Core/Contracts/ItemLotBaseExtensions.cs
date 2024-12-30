namespace DSLRNet.Core.Contracts;

public static class ItemLotBaseExtensions
{
    public static ItemLotBase CloneToBase(this ItemLotParam_enemy enemyItemLot)
    {
        return JsonConvert.DeserializeObject<ItemLotBase>(JsonConvert.SerializeObject(enemyItemLot))
            ?? throw new Exception("Could not clone enemy item lot to base item lot");
    }

    public static int GetIndexOfFirstOpenLotItemId(this ItemLotBase itemLot)
    {
        IOrderedEnumerable<System.Reflection.PropertyInfo> properties = itemLot.GetType().GetProperties()
            .Where(p => p.Name.StartsWith("lotItemId") && p.PropertyType == typeof(int))
            .OrderBy(p => p.Name);

        foreach (System.Reflection.PropertyInfo? property in properties)
        {
            if (property != null && (int)(property.GetValue(itemLot) ?? -1) == 0)
            {
                return int.Parse(property.Name[^2..]);
            }
        }

        return -1; // or return a default value indicating none found
    }
}
