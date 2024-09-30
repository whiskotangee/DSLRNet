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

    public int GetFmgType(string whichtext = "Title", string whichloottype = "Weapons")
    {
        int fmgtoreturn = 0;
        string fmgquery = whichtext + whichloottype;

        if (Enum.TryParse<FMG>(fmgquery, out FMG fmg))
        {
            // TODO
            //fmgtoreturn = GetNode<GlobalVariables>("/root/GlobalVariables").FMGTypes[GetGameId()][FMG.Get(fmgquery)];
        }

        if (fmgtoreturn == 0)
        {

            Log.Logger.Error($"{this.Name}'s request for FMGID has returned 0 - this may not work!");
        }
        else
        {
            Log.Logger.Error($"{this.Name} requesting FMGID for {fmgquery} has returned {fmgtoreturn}");
        }

        return fmgtoreturn;
    }

    public string CreateFmgTextEntry(string category = "Weapons", int id = 1000000, string text = "Dagger")
    {
        // Patch a specific bug with these characters sometimes replacing '
        // Replace any quotes in text with double quotes - thanks Mountlover!
        text = text.Replace("\"", "\"\"");

        return $"\"{category}:{id}:{text}\"";
    }

    public List<string> CreateFmgLootEntrySet(string category = "Weapons", int id = 1000000, string title = "Dagger",
                                        string description = "This is a dagger!", string summary = "", bool multiname = false)
    {
        List<string> final = new List<string>();

        // Add Title Entry
        if (title != "")
        {
            // If multiname add name multiple times - trying to fix armor names not being included for some reason
            int times = !multiname ? 1 : 2;

            for (int x = 0; x < times; x++)
            {
                final.Add(CreateFmgTextEntry($"{category}Name", id, title));
            }
        }

        // Add Description Entry
        if (description != "")
        {
            final.Add(CreateFmgTextEntry($"{category}Caption", id, description));
        }

        // Add Summary Entry
        if (summary != "")
        {
            final.Add(CreateFmgTextEntry($"{category}Info", id, summary));
        }

        // Log.Logger.Error(finalarray);
        return final;
    }

    public void SetFmgJsonTemplate()
    {
        // Load custom FMG template, get its text and return the result
        FMGTemplate = File.ReadAllText("DefaultData/ER/FMGBase/DSMSFMGTemplate.txt");
    }

    private static void CorrectArrayValuesFromStringToNumericalValue(List<string> arrayToCheck)
    {
        for (int i = 0; i < arrayToCheck.Count; i++)
        {
            if (StringExtensions.StringHasNumbersOnly(arrayToCheck[i]) && !StringExtensions.StringContainsBrackets(arrayToCheck[i]))
            {
                if (StringExtensions.StringContainsPeriod(arrayToCheck[i]))
                {
                    arrayToCheck[i] = double.Parse(arrayToCheck[i]).ToString();
                }
                else
                {
                    arrayToCheck[i] = int.Parse(arrayToCheck[i]).ToString();
                }
            }
        }
    }

    private static Dictionary<string, object> CreateParamDictionaryUsingHeaderArray(List<string> headersArray, List<string> valuesArray)
    {
        var dict = new Dictionary<string, object>();

        for (int i = 0; i < headersArray.Count; i++)
        {
            dict[headersArray[i]] = valuesArray[i];
        }

        return dict;
    }

    public static string CreateMassEditLine(string paramName = "EquipParamWeapon", int id = 0, string propName = "Name", string value = "DSLR", string newLine = "\n")
    {
        return $"param {paramName}: id {id}: {propName}: = {value};{newLine}";
    }

    public static string CreateMassEditParamFromParamDictionary(GenericDictionary dict, string paramName = "EquipParamWeapon", int id = 0, List<string> additionalFilters = null, List<string> bannedEquals = null, List<string> mandatoryKeys = null)
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

        foreach (var kvp in dict.Properties)
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

public static class GlobalParamCompilationFunctions
{
    public static Dictionary<string, object> CreateParamDictionaryUsingHeaderArray(List<string> headersArray, List<object> valuesArray, bool warn = true)
    {
        var dict = new Dictionary<string, object>();

        // Catch any blank headers
        headersArray.RemoveAll(header => string.IsNullOrEmpty(header));

        // Catch when we don't have enough values for our headers
        if (headersArray.Count > valuesArray.Count)
        {
            var noParams = new List<string>();
            for (int x = 0; x < headersArray.Count; x++)
            {
                if (x >= valuesArray.Count)
                {
                    noParams.Add(headersArray[x]);
                }
            }

            string message = $"{valuesArray[0]} HAS TOO FEW VALUES TO FIT ALL HEADERS - MISSING HEADERS ARE {string.Join(", ", noParams)}. THEY WILL BE GIVEN -1 AS A DEFAULT VALUE.";
            if (warn)
            {
                Log.Logger.Warning(message); // Replace with OS.alert(message) if using a specific alert system
            }
            Log.Logger.Information(message);

            for (int x = 0; x < headersArray.Count - valuesArray.Count; x++)
            {
                valuesArray.Add(-1);
            }
        }

        for (int x = 0; x < headersArray.Count; x++)
        {
            dict[headersArray[x]] = valuesArray[x];
        }

        return dict;
    }
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
        // This is used to determine whether we need to convert a string to an int or a double
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
        // to either int or double respectively
        for (int i = 0; i < arrayToCheck.Count; i++)
        {
            // Check for square brackets to avoid overwriting arrays
            if (StringHasNumbersOnly(arrayToCheck[i]) && !StringContainsBrackets(arrayToCheck[i]))
            {
                // First check to make sure we're not overwriting an array
                if (StringContainsPeriod(arrayToCheck[i]))
                {
                    arrayToCheck[i] = double.Parse(arrayToCheck[i]).ToString();
                }
                else
                {
                    arrayToCheck[i] = int.Parse(arrayToCheck[i]).ToString();
                }
            }
        }
    }
}

