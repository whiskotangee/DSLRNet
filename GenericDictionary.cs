using Newtonsoft.Json;
using Serilog;
using System.Reflection;

namespace DSLRNet;

public class GenericDictionary :  ICloneable
{
    public static GenericDictionary FromObject(object obj)
    {
        var dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

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

    public List<T> GetPropertiesByNames<T>(IEnumerable<string> names)
    {
        return names.Select(s => GetValue<T>(s)).ToList();
    }

    public T GetValue<T>(string name)
    {
        if (Properties.TryGetValue(name, out object? value))
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
        }

        throw new Exception($"Param with name {name} not of type {typeof(T)} or not found in dictionary");
    }

    public void SetValue(string name, object? value)
    {
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
        var newDictionary = new GenericDictionary();

        var clonedDictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach(var keyValue in  this.Properties)
        {
            clonedDictionary[keyValue.Key] = JsonConvert.DeserializeObject<object?>(JsonConvert.SerializeObject(keyValue.Value));
        }

        newDictionary.Properties = clonedDictionary;

        return newDictionary;
    }
}
