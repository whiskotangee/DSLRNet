namespace DSLRNet.Data;

public class DataRepository()
{
    private Dictionary<string, List<string>> massEdit = [];
    private Dictionary<string, List<GenericDictionary>> paramEdits = [];

    private List<string> textLines = [];

    public void AddParamEdit(string name, GenericDictionary param)
    {
        if (!paramEdits.TryGetValue(name, out List<GenericDictionary>? list))
        {
            paramEdits[name] = [];
            list = paramEdits[name];
        }

        list.Add(param);
    }

    public List<GenericDictionary> GetAllEditsForParam(string name)
    {
        if (paramEdits.TryGetValue(name, out List<GenericDictionary>? list))
        {
            return [.. list];
        }

        return [];
    }

    public void AddToMassEdit(string name, string line)
    {
        if (!massEdit.TryGetValue(name, out List<string>? list))
        {
            massEdit[name] = [];
        }

        massEdit[name].Add(line);
    }

    public void AddToMassEdit(string name, IEnumerable<string> lines)
    {
        foreach (string line in lines)
        {
            this.AddToMassEdit(name, line);
        }
    }

    public void AddText(List<string> text) 
    { 
        textLines.AddRange(text);
    }

    public Dictionary<string, List<string>> GetMassEditContents() 
    {
        return massEdit;
    }

    public List<string> GetTextLines() 
    {
        return textLines;
    }
}
