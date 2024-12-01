using System.Diagnostics;

namespace DSLRNet.Core.Contracts;

public class ParamBase<T> : ICloneable
{
    [JsonIgnore]
    internal GenericParam GenericParam { get; } = new GenericParam();

    public object Clone()
    {
        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(this));
    }
}
