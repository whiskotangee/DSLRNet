namespace DSLRNet.Core.Handlers;

using DSLRNet.Core.Common;
using DSLRNet.Core.Contracts;
using DSLRNet.Core.DAL;

public class BaseHandler(ParamEditsRepository generatedDataRepository)
{
    public ParamEditsRepository GeneratedDataRepository { get; set; } = generatedDataRepository;

    public static string CreateMassEditLine(ParamNames paramName, int id, string propName, string value)
    {
        return $"param {paramName}: id {id}: {propName}: = {value};{Environment.NewLine}";
    }
}

