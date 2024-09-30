using DSLRNet.Config;
using DSLRNet.Data;
using Microsoft.Extensions.Options;
using Mods.Common;
using Serilog;

namespace DSLRNet.Handlers;

public class DamageTypeHandler : BaseHandler
{
    private Configuration configuration { get; set; }

    public List<DamageType> DamageTypes { get; set; }

    private const int ShieldParamOffset = 6;

    private readonly RandomNumberGetter random;

    public DamageTypeHandler(IOptions<Configuration> configuration, RandomNumberGetter random, DataRepository dataRepository) : base(dataRepository)
    {
        this.configuration = configuration.Value;
        this.random = random;

        this.DamageTypes = CsvLoader.LoadCsv<DamageType>("DefaultData\\ER\\CSVs\\DamageTypeSetup.csv");
    }

    // DTH SELECTION FUNCTIONS

    public DamageType ChooseDamageTypeAtRandom(bool totallyRandom = false, bool secondaryDamage = false)
    {
        // Choose weight to get from DamageType dictionary based on secondary damage
        int weightToGet = secondaryDamage ? 11 : 10;

        // If totally random, choose from the available DamageTypes at complete random
        if (totallyRandom)
        {
            var randomIndex = this.random.NextInt(0, this.DamageTypes.Count);
            return DamageTypes[randomIndex];
        }

        var weights = new List<int>();
        var ids = new List<int>();

        // Iterate over all DamageType keys, add their weightsToGet and ID to relevant arrays
        foreach (var damageType in this.DamageTypes)
        {
            weights.Add(secondaryDamage ? damageType.SecWeight : damageType.PriWeight);
            ids.Add(damageType.ID);
        }

        int finalDtId = RngRandomWeighted(ids, weights);

        return DamageTypes.Single(d => d.ID == finalDtId);
    }

    public bool DamageTypesAffectSameParam(DamageType dt1, DamageType dt2)
    {
        if (string.IsNullOrEmpty(dt1.Param))
        {
            // Console.WriteLine("DT1 DOESN'T HAVE 'PARAM'! RETURNING FALSE!");
            return false;
        }
        if (string.IsNullOrEmpty(dt2.Param))
        {
            Log.Logger.Debug("DT2 DOESN'T HAVE 'PARAM'! RETURNING FALSE!");
            return false;
        }
        return dt1.Param.Equals(dt2.Param);
    }

    public void ApplyDamageTypeWeaponSpEffects(DamageType dt1, DamageType dt2, GenericDictionary weaponDict)
    {
        var speffParam = this.configuration.LootParam.Speffects.EquipParamWeapon;
        var behSpeffParam = this.configuration.LootParam.WeaponBehSpeffects;

        // For weapons, we'll leave the first speff slot free, but the latter ones (slots 2 and 3 in ER) should be used to apply damage type effects
        if (speffParam.Count > 0)
        {
            int dt1SpeffOffset = Math.Clamp(speffParam.Count - 1, 0, 99);
            int dt2SpeffOffset = Math.Clamp(speffParam.Count - 2, 0, 99);

            string speffParam1 = speffParam[dt1SpeffOffset];
            string speffParam2 = speffParam[dt2SpeffOffset];

            if (weaponDict.ContainsKey(speffParam1))
            {
                weaponDict.SetValue(speffParam1,dt1.SpEffect);
            }

            if (weaponDict.ContainsKey(speffParam2))
            {
                if (!dt2.SpEffect.Equals(dt1.SpEffect) ||
                    (dt2.SpEffect.Equals(dt1.SpEffect) && !dt2.NoSecondEffect))
                {
                    weaponDict.SetValue(speffParam2, dt2.SpEffect);
                }
            }
        }

        // Now apply behSpeffects
        if (behSpeffParam.Count > 0)
        {
            int dt1BehSpeffOffset = Math.Clamp(behSpeffParam.Count - 1, 0, 99);
            int dt2BehSpeffOffset = Math.Clamp(behSpeffParam.Count - 2, 0, 99);

            string behSpeffParam1 = behSpeffParam[dt1BehSpeffOffset];
            string behSpeffParam2 = behSpeffParam[dt2BehSpeffOffset];

            if (weaponDict.ContainsKey(behSpeffParam1))
            {
                weaponDict.SetValue(behSpeffParam1, dt1.OnHitSpEffect);
            }
            if (weaponDict.ContainsKey(behSpeffParam2))
            {
                if (!dt2.OnHitSpEffect.Equals(dt1.OnHitSpEffect) ||
                    (dt2.OnHitSpEffect.Equals(dt1.OnHitSpEffect) && !dt2.NoSecondEffect))
                {
                    weaponDict.SetValue(behSpeffParam2, dt2.OnHitSpEffect);
                }
            }
        }
    }
    public double GetTotalThrowDamageModifier(DamageType dt1, DamageType dt2)
    {
        if (dt1.CriticalMultAddition < 0 || dt2.CriticalMultAddition < 0)
        {
            return 1.0f;
        }

        double finalThrowDamage = dt1.CriticalMultAddition + dt2.CriticalMultAddition;
        return MathFunctions.RoundToXDecimalPlaces((double)(1.0f + finalThrowDamage), 2);
    }

    public void ApplyWeaponVfxFromDamageTypes(GenericDictionary weapDict, DamageType dt1, DamageType dt2)
    {
        var vfxParams = this.configuration.LootParam.WeaponsVfxParam;
        var vfxDummyParams = this.configuration.LootParam.WeaponsVfxDummyParam;
        var vfxDummies = this.configuration.LootParam.WeaponsVfxDummies;
        int dt1Vfx = dt1.VFXSpEffectID;
        int dt2Vfx = dt2.VFXSpEffectID;
        var dtArray = new int[] { dt1Vfx, dt2Vfx };

        if (dt1Vfx != dt2Vfx)
        {
            if (dt1Vfx == -1 && dt2Vfx != -1)
            {
                dtArray = [dt2Vfx, dt2Vfx];
            }
            else if (dt1Vfx != -1 && dt2Vfx == -1)
            {
                dtArray = [dt1Vfx, dt1Vfx];
            }
        }

        for (int x = 0; x < vfxParams.Count; x++)
        {
            if (weapDict.ContainsKey(vfxParams[x]) && weapDict.ContainsKey(vfxDummyParams[x]))
            {
                weapDict.SetValue(vfxParams[x], dtArray[x]);
                weapDict.SetValue(vfxDummyParams[x],vfxDummies[x]);
            }
        }
    }

    private int RngRandomWeighted(List<int> ids, List<int> weights)
    {
        int totalWeight = 0;
        foreach (var weight in weights)
        {
            totalWeight += weight;
        }

        int randomValue = this.random.NextWeightedValue(ids, weights, 1.0f);
        for (int i = 0; i < weights.Count; i++)
        {
            if (randomValue < weights[i])
            {
                return ids[i];
            }
            randomValue -= weights[i];
        }

        return ids[0]; // Fallback in case of an error
    }
}
