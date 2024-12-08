namespace DSLRNet.Core.Config;
using System.Text.RegularExpressions;

public class LoreConfig
{
    public List<string> Names { get; set; }
    public List<string> Locations { get; set; }

    public LoreTemplates MadLibsConfig { get; set; }

    public UniqueNameConfig UniqueNamesConfig { get; set; }
}

public class LoreTemplates
{
    private string GetTemplatePart(List<string> templateList, List<string> excludedIdentifiers, RandomProvider random)
    {
        List<string> ret = templateList.Where(d => !excludedIdentifiers.Any(s => d.Contains(s))).ToList();

        List<string> withPlaceholders = ret.Where(d => d.Contains("{")).ToList();

        if (ret.Count == 0)
        {
            ret = templateList;
        }

        return random.GetRandomItem(withPlaceholders.Count > 0 && random.PassesPercentCheck(50) ? withPlaceholders : ret);
    }

    public List<string> FindPlaceholdersInString(string input, List<string> placeholders)
    {
        List<string> matches = [];
        foreach (string placeholder in placeholders)
        {
            string pattern = @"\{" + Regex.Escape(placeholder) + @"\}";
            Regex regex = new(pattern);
            if (regex.IsMatch(input))
            {
                matches.Add("{" + placeholder + "}");
            }
        }
        return matches;
    }

    public (string Prefix, string Interfix, string Postfix) GetRandomDescription(RandomProvider random, List<string> possibleSubtitutions)
    {
        Dictionary<int, List<string>> sources = new()
        {
            { 0, this.Prefixes },
            { 1, this.Interfixes },
            { 2, this.PostFixes }
        };

        List<int> randomizedSources = random.GetRandomizedList(sources.Keys);

        int firstKey = randomizedSources[0];
        int secondKey = randomizedSources[1];
        int thirdKey = randomizedSources[2];

        // Randomly pick the first item
        string firstValue = random.GetRandomItem(sources[firstKey]);
        List<string> claimedSubstitutions = this.FindPlaceholdersInString(firstValue, possibleSubtitutions);

        // Assign the first value based on the key
        string prefix = string.Empty, interfix = string.Empty, postfix = string.Empty;

        // Randomly pick the remaining keys
        List<int> remainingKeys = [0, 1, 2];
        remainingKeys.Remove(firstKey);

        string secondValue = this.GetTemplatePart(sources[secondKey], claimedSubstitutions, random);
        claimedSubstitutions.AddRange(this.FindPlaceholdersInString(secondValue, possibleSubtitutions));

        string thirdValue = this.GetTemplatePart(sources[thirdKey], claimedSubstitutions, random);

        switch (firstKey)
        {
            case 0:
                prefix = firstValue;
                break;
            case 1:
                interfix = firstValue;
                break;
            case 2:
                postfix = firstValue;
                break;
        }

        // Assign the remaining values
        switch (secondKey)
        {
            case 0:
                prefix = secondValue;
                break;
            case 1:
                interfix = secondValue;
                break;
            case 2:
                postfix = secondValue;
                break;
        }

        switch (thirdKey)
        {
            case 0:
                prefix = thirdValue;
                break;
            case 1:
                interfix = thirdValue;
                break;
            case 2:
                postfix = thirdValue;
                break;
        }

        return (prefix, interfix, postfix);
    }

    public List<string> Prefixes { get; set; }

    public List<string> Interfixes { get; set; }

    public List<string> PostFixes { get; set; }
}
public class UniqueNameConfig
{
    public List<string> UniqueNameFirstHalf { get; set; }
    public List<string> UniqueNameSecondHalf { get; set; }
    public List<string> UniqueNameSecondHalfShield { get; set; }
    public List<string> UniqueNameFirstWord { get; set; }
    public List<string> UniqueNameSecondWord { get; set; }
}
