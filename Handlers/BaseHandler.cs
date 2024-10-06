namespace DSLRNet.Handlers;

using DSLRNet.Data;
using DSLRNet.Generators;
using Serilog;
using System.Runtime.CompilerServices;

public class BaseHandler(DataRepository generatedDataRepository)
{
    public string Name { get; set; }

    // FMG ENUM
    public enum FMG { DescriptionWeapons, DescriptionArmor, DescriptionRings, SummaryWeapons, SummaryArmor, SummaryRings, TitleWeapons, TitleArmor, TitleRings }

    // FMG VARIABLES
    public string FMGTemplate = "";

    public DataRepository GeneratedDataRepository { get; set; } = generatedDataRepository;

    public string ConvertArrayToStringWithDelim(List<int> array, string delim = ",", string newline = "\n")
    {
        string finalString = "";
        for (int x = 0; x < array.Count; x++)
        {
            string addition = x == array.Count - 1 ? delim : newline;
            finalString += array[x] + addition;
        }
        //Log.Logger.Error(finalString);
        return finalString;
    }

    public string ConvertDictionaryToStringUsingHeaderArray(GenericDictionary dict, List<string> headersArray, string delim = ",", string lineEnd = "")
    {
        string finalString = "";
        // USING THE HEADERSARRAY VALUE TO SORT THE DICT ITEMS INTO THE CORRECT ORDER, COMPILE THEM ALL TOGETHER, USING THE DELIM AND LINEEND NEEDED
        for (int x = 0; x < headersArray.Count; x++)
        {
            // USE LINEEND INSTEAD OF DELIM IF WE'RE ON THE LAST ENTRY OF THE HEADERSARRAY
            string nextDelim = x == headersArray.Count - 1 ? lineEnd : delim;
            finalString += dict.GetValue<string>(headersArray[x]) + nextDelim;
        }
        return finalString;
    }

    public LootFMG CreateFmgLootEntrySet(string category = "Weapons", int id = 1000000, string title = "Dagger",string description = "This is a dagger!", string summary = "")
    {
        return new LootFMG()
        {
            Category = category,
            Name = title,
            Caption = description,
            Info = summary
        };
    }

    public void SetFmgJsonTemplate()
    {
        // Load custom FMG template, get its text and return the result
        FMGTemplate = File.ReadAllText("DefaultData/ER/FMGBase/DSMSFMGTemplate.txt");
    }

    public static string CreateMassEditLine(string paramName = "EquipParamWeapon", int id = 0, string propName = "Name", string value = "DSLR", string newLine = "\n")
    {
        return $"param {paramName}: id {id}: {propName}: = {value};{newLine}";
    }

    public string CreateMassEditParamFromParamDictionary(GenericDictionary dict, string paramName = "EquipParamWeapon", int id = 0, List<string> additionalFilters = null, List<string> bannedEquals = null, List<string> mandatoryKeys = null)
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

        foreach (KeyValuePair<string, object?> kvp in dict.Properties.Where(d => !d.Key.Contains("pad", StringComparison.OrdinalIgnoreCase)))
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
            }

            if (valueValid)
            {
                finalMassEdit += CreateMassEditLine(paramName, id, newKey, newValue);
            }
        }

        return finalMassEdit;
    }
}

public class GlobalOutput
{
    private static GlobalOutput _instance;
    public static GlobalOutput Instance => _instance ??= new GlobalOutput();
    public string GlobalMassEdit { get; set; } = "";
}
public static class StringExtensions
{
    private const string Letters = "abcdefghijklmnopqrstuvwxyz";
    private const string CapitalLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Numbers = "0123456789";

    public static string CreateShortenedString(string original, int maximumCharacters = 12)
    {
        if (original.Length <= maximumCharacters)
        {
            return original;
        }
        else
        {
            return original.Substring(0, maximumCharacters);
        }
    }

    public static bool StringHasNumbersOnly(string stringToCheck)
    {
        // First, check for capital letters and immediately return false if found
        foreach (char c in CapitalLetters)
        {
            if (stringToCheck.Contains(c))
            {
                return false;
            }
        }

        // Do the same for lowercase letters if we've cleared that
        foreach (char c in Letters)
        {
            if (stringToCheck.Contains(c))
            {
                return false;
            }
        }

        // If we've cleared those, establish whether there are any numbers in the string and return true if so
        foreach (char c in Numbers)
        {
            if (stringToCheck.Contains(c))
            {
                return true;
            }
        }

        // If we're somehow still going, return false but mark when this happens
        // Console.WriteLine($"No letters or numbers found in {stringToCheck}, returning false.");
        return false;
    }

    public static bool StringContainsPeriod(string stringToCheck)
    {
        // This is used to determine whether we need to convert a string to an int or a float
        return stringToCheck.Contains(".");
    }

    public static bool StringContainsBrackets(string stringToCheck)
    {
        // This is used to stop us overwriting arrays
        return stringToCheck.Contains("[") || stringToCheck.Contains("]") ||
               stringToCheck.Contains("{") || stringToCheck.Contains("}") ||
               stringToCheck.Contains("(") || stringToCheck.Contains(")");
    }

    public static void CorrectArrayValuesFromStringToNumericalValue(List<string> arrayToCheck)
    {
        // Iterate over each value in an array, check if it only has numbers, if so check if it has a period, then convert
        // to either int or float respectively
        for (int i = 0; i < arrayToCheck.Count; i++)
        {
            // Check for square brackets to avoid overwriting arrays
            if (StringHasNumbersOnly(arrayToCheck[i]) && !StringContainsBrackets(arrayToCheck[i]))
            {
                // First check to make sure we're not overwriting an array
                if (StringContainsPeriod(arrayToCheck[i]))
                {
                    arrayToCheck[i] = float.Parse(arrayToCheck[i]).ToString();
                }
                else
                {
                    arrayToCheck[i] = int.Parse(arrayToCheck[i]).ToString();
                }
            }
        }
    }
}

