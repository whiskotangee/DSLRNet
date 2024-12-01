namespace DSLRNet.Core.Contracts.Params;

public class ParamBase
{
    [JsonIgnore]
    public GenericParam GenericParam { get; set; } = new GenericParam();
}
