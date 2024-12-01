using System.Reflection;

namespace DSLRNet.Core.Common;

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

    public int ID { get => this.GetValue<int>("ID"); set => this.SetValue("ID", value); }

    public string Name { get => this.GetValue<string>("Name"); set => this.SetValue("Name", value); }

    public T GetValue<T>(string name)
    {
        if (Properties.TryGetValue(name, out object? value))
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
        }

        throw new Exception($"Param with name {name} not of type {typeof(T)} or not found in dictionary");
    }

    public void SetValue<T>(string name, T? value)
    {
        if (Properties.ContainsKey(name)
            && typeof(T).Name != Properties[name].GetType().Name
            && !typeof(T).Name.Contains("Int32") && !Properties[name].GetType().Name.Contains("Int64")
            && !typeof(T).Name.Contains("Int64") && !Properties[name].GetType().Name.Contains("Int32")
            && !typeof(T).Name.Contains("Single") && !Properties[name].GetType().Name.Contains("Double")
            && !typeof(T).Name.Contains("Double") && !Properties[name].GetType().Name.Contains("Single"))
        {
            throw new Exception("Mismatched type being set");
        }

        Properties[name] = value;
    }

    public bool ContainsKey(string name)
    {
        return Properties.ContainsKey(name);
    }

    public object Clone()
    {
        GenericParam newDictionary = new();

        Dictionary<string, object?> clonedDictionary = new(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, object?> keyValue in Properties)
        {
            clonedDictionary[keyValue.Key] = JsonConvert.DeserializeObject<object?>(JsonConvert.SerializeObject(keyValue.Value));
        }

        newDictionary.Properties = clonedDictionary;

        return newDictionary;
    }
}
