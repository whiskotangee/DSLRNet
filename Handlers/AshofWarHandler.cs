namespace DSLRNet.Handlers;

using DSLRNet.Config;
using DSLRNet.Data;
using Microsoft.Extensions.Options;
using Mods.Common;
using System;
using System.Collections.Generic;

public enum AoWCategory
{
    BowCrossbow,
    Shield,
    StaffSeal,
    Other
}

public class AshofWarHandler(RandomNumberGetter random, IOptions<Configuration> configuration, DataRepository generatedDataRepository) : BaseHandler(generatedDataRepository)
{
    private const string aowparam = "swordArtsParamId";

    // COMPATIBLE SWORDARTSPARAM IDS SEPARATED BY WEPMOTIONCATEGORY
    private static readonly Dictionary<AoWCategory, List<int>> compatibleSwordArtsParam = new Dictionary<AoWCategory, List<int>>
    {
        { AoWCategory.BowCrossbow, new List<int> { 400, 401, 402, 404, 405, 406, 10 } },
        { AoWCategory.Shield, new List<int> { 300, 301, 302, 303, 305, 306, 307, 308, 10 } },
        { AoWCategory.StaffSeal, new List<int> { 10 } },
        { AoWCategory.Other, new List<int> { 800, 801, 802, 850, 10, 103, 105, 106, 107, 115, 120, 122, 123, 650, 652, 654, 653, 700, 701, 702 } }
    };

    private static readonly Dictionary<AoWCategory, List<int>> swordArtsWeaponMotionCategories = new()
    {
        { AoWCategory.BowCrossbow, new List<int> { 51, 44, 45, 46 } },
        { AoWCategory.Shield, new List<int> { 48, 49, 47 } },
        { AoWCategory.StaffSeal, new List<int> { 31, 41 } }
    };

    private readonly Configuration configuration = configuration.Value;

    // AOW ASSIGN FUNCTIONS

    public void AssignAshOfWar(GenericDictionary weaponDict)
    {
        int weaponWmc = weaponDict.GetValue<int>(this.configuration.LootParam.WeaponsWepMotionCategory);
        AoWCategory aowCat = GetAshOfWarCategoryFromWepMotionCategory(weaponWmc);
        int finalId = random.GetRandomItem(compatibleSwordArtsParam[aowCat]);
        weaponDict.SetValue(aowparam, finalId);
    }

    private AoWCategory GetAshOfWarCategoryFromWepMotionCategory(int wmc = -1)
    {
        // ITERATE OVER SAPWMC, IF THE ARRAY HAS WMC RETURN THAT KEY, OTHERWISE RETURN OTHER
        foreach (AoWCategory category in swordArtsWeaponMotionCategories.Keys)
        {
            if (swordArtsWeaponMotionCategories[category].Contains(wmc))
            {
                return category;
            }
        }
        return AoWCategory.Other;
    }
}

