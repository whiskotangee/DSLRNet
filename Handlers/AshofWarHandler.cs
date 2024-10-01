namespace DSLRNet.Handlers;

using DSLRNet.Config;
using DSLRNet.Data;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

public class AshofWarHandler(IOptions<Configuration> configuration, DataRepository generatedDataRepository) : BaseHandler(generatedDataRepository)
{
    private const string aowparam = "swordArtsParamId";

    // COMPATIBLE SWORDARTSPARAM IDS SEPARATED BY WEPMOTIONCATEGORY
    private static readonly Dictionary<string, List<int>> compatibleSwordArtsParam = new Dictionary<string, List<int>>
    {
        { "bowcrossbow", new List<int> { 400, 401, 402, 404, 405, 406, 10 } },
        { "shield", new List<int> { 300, 301, 302, 303, 305, 306, 307, 308, 10 } },
        { "staffseal", new List<int> { 10 } },
        { "other", new List<int> { 800, 801, 802, 850, 10, 103, 105, 106, 107, 115, 120, 122, 123, 650, 652, 654, 653, 700, 701, 702 } }
    };

    private static readonly List<string> aowCategories = new List<string> { "bowcrossbow", "shield", "staffseal" };

    private static readonly Dictionary<string, List<int>> sapwmc = new Dictionary<string, List<int>>
    {
        { "bowcrossbow", new List<int> { 51, 44, 45, 46 } },
        { "shield", new List<int> { 48, 49, 47 } },
        { "staffseal", new List<int> { 31, 41 } }
    };
    private readonly Configuration configuration = configuration.Value;

    // AOW ASSIGN FUNCTIONS

    public void AssignAshOfWar(GenericDictionary weaponDict)
    {
        int weaponWmc = weaponDict.GetValue<int>(this.configuration.LootParam.WeaponsWepMotionCategory);
        string aowCat = GetAshOfWarCategoryFromWepMotionCategory(weaponWmc);
        List<int> aowArray = compatibleSwordArtsParam[aowCat];
        int finalId = aowArray[new Random().Next(aowArray.Count)];
        weaponDict.SetValue(aowparam, finalId);
    }

    private string GetAshOfWarCategoryFromWepMotionCategory(int wmc = -1)
    {
        // ITERATE OVER SAPWMC, IF THE ARRAY HAS WMC RETURN THAT KEY, OTHERWISE RETURN OTHER
        foreach (string category in aowCategories)
        {
            if (sapwmc[category].Contains(wmc))
            {
                return category;
            }
        }
        return "other";
    }
}

