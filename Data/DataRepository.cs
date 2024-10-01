namespace DSLRNet.Data;

public class DataRepository()
{
    private Dictionary<string, List<string>> massEdit = [];
    private List<string> textLines = [];

    public void AddToMassEdit(string name, string line)
    {
        if (!massEdit.TryGetValue(name, out var list))
        {
            massEdit[name] = [];
        }

        massEdit[name].Add(line);
    }

    public void AddToMassEdit(string name, IEnumerable<string> lines)
    {
        foreach (var line in lines)
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
