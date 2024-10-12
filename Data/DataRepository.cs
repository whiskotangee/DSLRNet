using DSLRNet.Config;
using Microsoft.Extensions.Options;
using SoulsFormats;
using System.Collections.Concurrent;

namespace DSLRNet.Data;

public enum ParamOperation { Create, MassEdit, TextOnly }

public class ParamEdit
{
    public ParamOperation Operation { get; set; }

    public string ParamName { get; set; }

    public LootFMG? MessageText { get; set; }
    public string MassEditString { get; set; }

    public GenericDictionary ParamObject { get; set; }
}

public class DataRepository
{
    private List<ParamEdit> paramEdits = [];

    public void AddParamEdit(string name, ParamOperation operation, string massEditString, LootFMG text,  GenericDictionary? param)
    {
        ParamEdit currentEdit = this.paramEdits.FirstOrDefault(d => d.ParamName.Equals(name) && d.Operation == operation) ?? new ParamEdit() { ParamName = name, Operation = operation };

        paramEdits.Add(new ParamEdit
        {
            Operation = operation,
            ParamName = name,
            MassEditString = massEditString,
            MessageText = text,
            ParamObject = param ?? new GenericDictionary()
        });
    }

    public List<ParamEdit> GetParamEdits(ParamOperation? operation = null, string? paramName = null)
    {
        IEnumerable<ParamEdit> edits = [.. paramEdits];

        if (operation != null)
        {
            edits = edits.Where(d => d.Operation == operation);
        }

        if (paramName != null)
        {
            edits = edits.Where(d => d.Equals(paramName));
        }

        return edits.ToList();
    }
}
