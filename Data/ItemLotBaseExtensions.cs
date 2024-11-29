using DSLRNet.Data.Generated;

namespace DSLRNet.Data
{
    public static class ItemLotBaseExtensions
    {
        public static void SetPropertyByName(this ItemLotBase itemLot, string name, object value)
        {
            itemLot.GetType().GetProperty(name).SetValue(itemLot, value);
        }

        public static T GetValue<T>(this ItemLotBase itemLot, string propertyName)
        {
            return (T)itemLot.GetType().GetProperty(propertyName).GetValue(itemLot);
        }

        public static int GetIndexOfFirstOpenLotItemId(this ItemLotBase itemLot)
        {
            var properties = itemLot.GetType().GetProperties()
                .Where(p => p.Name.StartsWith("lotItemId") && p.PropertyType == typeof(int))
                .OrderBy(p => p.Name);

            foreach (var property in properties)
            {
                if ((int)property.GetValue(itemLot) == 0)
                {
                    return int.Parse(property.Name.Substring(property.Name.Length -2));
                }
            }

            return -1; // or return a default value indicating none found
        }
    }
}
