namespace DSLRNet.Data;

public class DataRepository()
{
    private List<string> massEditLines = [];
    private List<string> textLines = [];

    public void AddToMassEdit(string line)
    {
        massEditLines.Add(line);
    }

    public void AddToMassEdit(IEnumerable<string> lines)
    {
        massEditLines.AddRange(lines);
    }

    public void AddText(List<string> text) 
    { 
        textLines.AddRange(text);
    }

    public List<string> GetMassEditContents() 
    {
        return [.. massEditLines];
    }

    public List<string> GetTextLines() 
    {
        return textLines;
    }
}
