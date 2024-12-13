namespace DSLRNet.Core.Common;
using System.Reflection;

public class GenericParam : ICloneable
{
    public static GenericParam FromObject(object obj)
    {
        Dictionary<string, object?> dictionary = new(StringComparer.OrdinalIgnoreCase);

        foreach (PropertyInfo property in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            dictionary[property.Name] = property.GetValue(obj, null);
        }

        return new GenericParam()
        {
            Properties = dictionary
        };
    }

    public Dictionary<string, object?> Properties { get; set; } = [];

    public int ID { get => this.GetValue<int>("ID"); set { this.SetValue("ID", value); } }

    public string Name { get => this.GetValue<string>("Name"); set => this.SetValue("Name", value); }

    public List<string> GetFieldNamesByFilter(string filter, bool endsWith = false, string? excludeFilter = null)
    {
        var ret = Properties.Keys
            .Where(d => endsWith ? d.EndsWith(filter, StringComparison.OrdinalIgnoreCase) : d.StartsWith(filter, StringComparison.OrdinalIgnoreCase));
        if (excludeFilter != null)
        {
            ret = ret.Where(d => !d.Contains(excludeFilter));
        }

        return ret.ToList();
    }

    public T GetValue<T>(string name)
    {
        if (this.Properties.TryGetValue(name, out object? value))
        {
            if (value is T castValue) 
            { 
                return castValue; 
            }

            if (value is byte[] data && typeof(T) == typeof(byte[])) 
            { 
                return (T)(object)data; 
            }

            if (value is string str && (str.Contains('|') || str.Contains("[")))
            {
                return (T)Convert.ChangeType(str.Split("|").Select(s => Convert.ToByte(s.Trim('[').Trim(']'))).ToArray(), typeof(T));
            }

            try 
            { 
                return (T)Convert.ChangeType(value, typeof(T)); 
            } 
            catch (InvalidCastException) 
            { 
                throw new Exception($"Param with name {name} not of type {typeof(T)} or not found in dictionary"); 
            }
        }

        throw new Exception($"Param with name {name} not of type {typeof(T)} or not found in dictionary");
    }

    public void SetValue<T>(string name, T? value)
    {
        if (this.Properties.ContainsKey(name)
            && typeof(T).Name != this.Properties[name].GetType().Name
            && !typeof(T).Name.Contains("Int32") && !this.Properties[name].GetType().Name.Contains("Int64")
            && !typeof(T).Name.Contains("Int64") && !this.Properties[name].GetType().Name.Contains("Int32")
            && !typeof(T).Name.Contains("Single") && !this.Properties[name].GetType().Name.Contains("Double")
            && !typeof(T).Name.Contains("Double") && !this.Properties[name].GetType().Name.Contains("Single"))
        {
            throw new Exception("Mismatched type being set");
        }

        this.Properties[name] = value;
    }

    public bool ContainsKey(string name)
    {
        return this.Properties.ContainsKey(name);
    }

    public object Clone()
    {
        GenericParam newDictionary = new();

        Dictionary<string, object?> clonedDictionary = new(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, object?> keyValue in this.Properties)
        {
            if (keyValue.Value is byte[])
            {
                clonedDictionary[keyValue.Key] = keyValue.Value;
            }
            else
            {
                clonedDictionary[keyValue.Key] = JsonConvert.DeserializeObject<object?>(JsonConvert.SerializeObject(keyValue.Value));
            }
        }

        newDictionary.Properties = clonedDictionary;

        return newDictionary;
    }
}
