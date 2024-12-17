namespace DSLRNet.Core.Contracts;

public class ParamBase<T> : ICloneable<T> where T : class
{
    [JsonIgnore]
    internal GenericParam GenericParam { get; } = new GenericParam();

    public TGetType GetValue<TGetType>(string name)
    {
        return GenericParam.GetValue<TGetType>(name);
    }

    public List<string> GetFieldNamesByFilter(string filter, bool endsWith = false, string? excludeFilter = null)
    {
        return GenericParam.GetFieldNamesByFilter(filter, endsWith, excludeFilter);
    }

    public void SetValue<TSetType>(string name, TSetType? value)
    {
        GenericParam.SetValue(name, value);
    }

    public int ID { get => GenericParam.ID; set { GenericParam.ID = value; } }

    public T Clone()
    {
        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(this));
    }
}
