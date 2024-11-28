using Newtonsoft.Json;
using System.Reflection;

namespace DSLRNet;

public class GenericDictionary :  ICloneable
{
    public static GenericDictionary FromObject(object obj)
    {
        Dictionary<string, object> dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (PropertyInfo property in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            dictionary[property.Name] = property.GetValue(obj, null);
        }

        return new GenericDictionary()
        {
            Properties = dictionary
        };
    }

    public Dictionary<string, object?> Properties { get; set; } = [];

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
        if (typeof(T).Name != Properties[name].GetType().Name 
            && !typeof(T).Name.Contains("Int32") && !Properties[name].GetType().Name.Contains("Int64")
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

    public T ToClassObject<T>()
        where T : class
    {
        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(this.Properties));
    }

    public object Clone()
    {
        GenericDictionary newDictionary = new GenericDictionary();

        Dictionary<string, object?> clonedDictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach(KeyValuePair<string, object?> keyValue in  this.Properties)
        {
            clonedDictionary[keyValue.Key] = JsonConvert.DeserializeObject<object?>(JsonConvert.SerializeObject(keyValue.Value));
        }

        newDictionary.Properties = clonedDictionary;

        return newDictionary;
    }
}
