namespace DSLRNet.Handlers;

using DSLRNet.Config;
using DSLRNet.Data;
using Microsoft.Extensions.Options;
using Mods.Common;
using Serilog;
using System;
using System.Collections.Generic;

public enum AoWCategory
{
    BowCrossbow,
    Shield,
    StaffSeal,
    Other
}

public class AshofWarHandler(RandomNumberGetter random, IOptions<Configuration> configuration, IOptions<AshOfWarConfig> ashofWarConfig, DataRepository generatedDataRepository) : BaseHandler(generatedDataRepository)
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
    private List<EquipParamGem> equipParamGems = Csv.LoadCsv<EquipParamGem>("DefaultData\\ER\\CSVs\\EquipParamGem.csv");
    private readonly AshOfWarConfig ashOfWarConfig = ashofWarConfig.Value;

    // AOW ASSIGN FUNCTIONS

    public void AssignAshOfWar(GenericDictionary weaponDict)
    {
        int weaponWmc = weaponDict.GetValue<int>(this.configuration.LootParam.WeaponsWepMotionCategory);

        // get sword artsId from set of equip gems compatible with this weapon
        var weaponType = weaponDict.GetValue<int>("wepType");
        var boolFlagToCheck = ashOfWarConfig.WeaponTypeToCanMountWepFlags.Single(d => d.Id == weaponType).FlagName;

        var validGems = equipParamGems.Where(d => Convert.ToInt64(d.GetType().GetProperty(boolFlagToCheck).GetValue(d)) == 1).ToList();

        if (validGems.Any())
        {
            var chosenGem = random.GetRandomItem(validGems);
            int finalId = chosenGem.swordArtsParamId;

            weaponDict.SetValue(aowparam, finalId);
            Log.Logger.Debug($"Assigning ash of war {chosenGem.ID} with SwordArtsParamID {chosenGem.swordArtsParamId} to weapon named {weaponDict.GetValue<string>("Name")} of type {weaponType}");
        }
        else
        {
            Log.Logger.Warning($"Weapon Base {weaponType} named {weaponDict.GetValue<string>("Name")} did not have any matching valid gems");
        }
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

