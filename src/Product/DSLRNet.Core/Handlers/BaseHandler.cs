namespace DSLRNet.Core.Handlers;

using DSLRNet.Core.Common;
using DSLRNet.Core.Contracts;
using DSLRNet.Core.Data;

public class BaseHandler(DataRepository generatedDataRepository)
{
    public string Name { get; set; }

    public DataRepository GeneratedDataRepository { get; set; } = generatedDataRepository;

    public LootFMG CreateFmgLootEntrySet(string category = "Weapons", int id = 1000000, string title = "Dagger", string description = "This is a dagger!", string summary = "")
    {
        return new LootFMG()
        {
            Category = category,
            Name = title,
            Caption = description,
            Info = summary
        };
    }

    public static string CreateMassEditLine(ParamNames paramName, int id = 0, string propName = "Name", string value = "DSLR", string newLine = "\n")
    {
        return $"param {paramName}: id {id}: {propName}: = {value};{newLine}";
    }

    public string CreateMassEditParamFromParamDictionary(GenericParam dict, ParamNames paramName, int id = 0, List<string> additionalFilters = null, List<string> bannedEquals = null, List<string> mandatoryKeys = null, GenericParam? defaultValue = null)
    {
        string finalMassEdit = "";
        List<string> banned = ["[", "]", "|"];
        List<string> finalBanned = new(banned);
        List<string> finalBannedEquals = [];

        if (additionalFilters != null)
        {
            finalBanned.AddRange(additionalFilters);
        }

        if (bannedEquals != null)
        {
            finalBannedEquals.AddRange(bannedEquals);
        }

        foreach (KeyValuePair<string, object?> kvp in dict.Properties.Where(d => !d.Key.Equals("Name", StringComparison.OrdinalIgnoreCase) || !d.Key.Contains("pad", StringComparison.OrdinalIgnoreCase)))
        {
            string newKey = kvp.Key;
            string? newValue = kvp.Value?.ToString();
            bool valueValid = true;

            if (mandatoryKeys == null || !mandatoryKeys.Contains(newKey))
            {
                if (finalBannedEquals.Count > 0 && finalBannedEquals.Contains(newValue))
                {
                    valueValid = false;
                }

                foreach (string bannedItem in finalBanned)
                {
                    if (newValue.Contains(bannedItem))
                    {
                        valueValid = false;
                        break;
                    }
                }

                if (valueValid && defaultValue != null && defaultValue.Properties.TryGetValue(newKey, out object? defaultval) && newValue?.ToString() == defaultval?.ToString())
                {
                    valueValid = false;
                }
            }

            if (valueValid)
            {
                finalMassEdit += CreateMassEditLine(paramName, id, newKey, newValue);
            }
        }

        return finalMassEdit;
    }
}

