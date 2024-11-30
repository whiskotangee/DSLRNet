namespace DSLRNet.Core.Handlers;

using DSLRNet.Core;
using DSLRNet.Core.Common;
using DSLRNet.Core.Config;
using DSLRNet.Core.Contracts.Params;
using DSLRNet.Core.Data;
using Microsoft.Extensions.Options;
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
    private readonly Configuration configuration = configuration.Value;
    private List<EquipParamGem> equipParamGems = Csv.LoadCsv<EquipParamGem>("DefaultData\\ER\\CSVs\\EquipParamGem.csv");
    private readonly AshOfWarConfig ashOfWarConfig = ashofWarConfig.Value;

    // AOW ASSIGN FUNCTIONS

    public void AssignAshOfWar(GenericDictionary weaponDict)
    {
        int weaponWmc = weaponDict.GetValue<int>(configuration.LootParam.WeaponsWepMotionCategory);

        // get sword artsId from set of equip gems compatible with this weapon
        var weaponType = weaponDict.GetValue<int>("wepType");
        var boolFlagToCheck = ashOfWarConfig.WeaponTypeToCanMountWepFlags.Single(d => d.Id == weaponType).FlagName;

        var validGems = equipParamGems.Where(d => Convert.ToInt64(d.GetType().GetProperty(boolFlagToCheck).GetValue(d)) == 1).ToList();

        if (validGems.Any())
        {
            var chosenGem = random.GetRandomItem(validGems);
            int finalId = chosenGem.swordArtsParamId;

            weaponDict.SetValue(aowparam, finalId);
        }
        else
        {
            Log.Logger.Warning($"Weapon Base {weaponType} named {weaponDict.GetValue<string>("Name")} did not have any matching valid gems");
        }
    }
}

