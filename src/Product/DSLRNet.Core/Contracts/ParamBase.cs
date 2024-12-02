using System.Diagnostics;

namespace DSLRNet.Core.Contracts;

public class ParamBase<T> : ICloneable<T> where T : class
{
    [JsonIgnore]
    internal GenericParam GenericParam { get; } = new GenericParam();

    public T Clone()
    {
        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(this));
    }
}
