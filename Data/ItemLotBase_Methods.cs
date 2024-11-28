namespace DSLRNet.Data
{
    public partial class ItemLotBase
    {
        public void SetPropertyByName(string name, object value)
        {
            this.GetType().GetProperty(name).SetValue(this, value);
        }

        public T GetValue<T>(string propertyName)
        {
            return (T)this.GetType().GetProperty(propertyName).GetValue(this);
        }

        public int GetIndexOfFirstOpenLotItemId()
        {
            var properties = this.GetType().GetProperties()
                .Where(p => p.Name.StartsWith("lotItemId") && p.PropertyType == typeof(int))
                .OrderBy(p => p.Name);

            foreach (var property in properties)
            {
                if ((int)property.GetValue(this) == 0)
                {
                    return int.Parse(property.Name.Substring(property.Name.Length -2));
                }
            }

            return -1; // or return a default value indicating none found
        }
    }
}
