using DSLRNet.Core.Common;
using DSLRNet.Core.Config;
using DSLRNet.Core.Contracts;
using DSLRNet.Core.Data;
using Microsoft.Extensions.Options;

namespace DSLRNet.Core.Handlers;

public class DamageTypeHandler : BaseHandler
{
    private Configuration configuration { get; set; }

    public List<DamageTypeSetup> DamageTypes { get; set; }

    private readonly RandomNumberGetter random;

    public DamageTypeHandler(IOptions<Configuration> configuration, RandomNumberGetter random, DataRepository dataRepository) : base(dataRepository)
    {
        this.configuration = configuration.Value;
        this.random = random;

        DamageTypes = Csv.LoadCsv<DamageTypeSetup>("DefaultData\\ER\\CSVs\\DamageTypeSetup.csv");

        DamageTypes.Where(d => d.Message >= 1022 && d.Message <= 1100)
            .ToList()
            .ForEach(d =>
            {
                GeneratedDataRepository.AddParamEdit(
                    ParamNames.TextOnly,
                    ParamOperation.TextOnly,
                    string.Empty,
                    new LootFMG()
                    {
                        Category = "Weapon",
                        Effect = d.EffectDescription
                    },
                    new GenericDictionary() { Properties = new Dictionary<string, object?>() { { "ID", d.Message } } });
            });
    }

    public DamageTypeSetup ChooseDamageTypeAtRandom(bool totallyRandom = false, bool secondaryDamage = false)
    {
        // Choose weight to get from DamageType dictionary based on secondary damage
        int weightToGet = secondaryDamage ? 11 : 10;

        // If totally random, choose from the available DamageTypes at complete random
        if (totallyRandom)
        {
            return random.GetRandomItem(DamageTypes);
        }

        List<int> weights = [];
        List<int> ids = [];

        // Iterate over all DamageType keys, add their weightsToGet and ID to relevant arrays
        foreach (DamageTypeSetup damageType in DamageTypes)
        {
            weights.Add(secondaryDamage ? damageType.SecWeight : damageType.PriWeight);
            ids.Add(damageType.ID);
        }

        int finalDtId = RngRandomWeighted(ids, weights);

        return DamageTypes.Single(d => d.ID == finalDtId);
    }

    public void ApplyDamageTypeWeaponSpEffects(WeaponModifications mods, GenericDictionary weaponDict)
    {
        List<string> speffParam = configuration.LootParam.Speffects.EquipParamWeapon;
        List<string> behSpeffParam = configuration.LootParam.WeaponBehSpeffects;

        // For weapons, we'll leave the first speff slot free, but the latter ones (slots 2 and 3 in ER) should be used to apply damage type effects
        if (speffParam.Count > 0)
        {
            int dt1SpeffOffset = Math.Clamp(speffParam.Count - 1, 0, 99);
            int dt2SpeffOffset = Math.Clamp(speffParam.Count - 2, 0, 99);

            string speffParam1 = speffParam[dt1SpeffOffset];
            string speffParam2 = speffParam[dt2SpeffOffset];

            if (weaponDict.ContainsKey(speffParam1))
            {
                weaponDict.SetValue(speffParam1, mods.PrimaryDamageType.SpEffect);
            }

            if (weaponDict.ContainsKey(speffParam2) && mods.SecondaryDamageType != null)
            {
                if (!mods.SecondaryDamageType.SpEffect.Equals(mods.PrimaryDamageType.SpEffect) ||
                    mods.SecondaryDamageType.SpEffect.Equals(mods.PrimaryDamageType.SpEffect) && mods.SecondaryDamageType.NoSecondEffect == 0)
                {
                    weaponDict.SetValue(speffParam2, mods.SecondaryDamageType.SpEffect);
                }
            }
        }

        if (behSpeffParam.Count > 0)
        {
            int dt1BehSpeffOffset = Math.Clamp(behSpeffParam.Count - 1, 0, 99);
            int dt2BehSpeffOffset = Math.Clamp(behSpeffParam.Count - 2, 0, 99);

            string behSpeffParam1 = behSpeffParam[dt1BehSpeffOffset];
            string behSpeffParam2 = behSpeffParam[dt2BehSpeffOffset];

            if (weaponDict.ContainsKey(behSpeffParam1))
            {
                weaponDict.SetValue(behSpeffParam1, mods.PrimaryDamageType.OnHitSpEffect);
            }

            if (weaponDict.ContainsKey(behSpeffParam2) && mods.SecondaryDamageType != null)
            {
                if (!mods.SecondaryDamageType.OnHitSpEffect.Equals(mods.PrimaryDamageType.OnHitSpEffect) ||
                    mods.SecondaryDamageType.OnHitSpEffect.Equals(mods.PrimaryDamageType.OnHitSpEffect) && mods.PrimaryDamageType.NoSecondEffect == 0)
                {
                    weaponDict.SetValue(behSpeffParam2, mods.SecondaryDamageType.OnHitSpEffect);
                }
            }
        }
    }

    public void ApplyWeaponVfxFromDamageTypes(GenericDictionary weapon, WeaponModifications mods)
    {
        List<string> vfxParams = configuration.LootParam.WeaponsVfxParam;
        List<string> vfxDummyParams = configuration.LootParam.WeaponsVfxDummyParam;
        List<int> vfxDummies = configuration.LootParam.WeaponsVfxDummies;
        
        int dt1Vfx = mods.PrimaryDamageType.VFXSpEffectID; 
        int? dt2Vfx = mods.SecondaryDamageType?.VFXSpEffectID; 
        
        int primaryVfx = Math.Max(dt1Vfx, dt2Vfx ?? dt1Vfx); 
        int secondaryVfx = dt2Vfx ?? dt1Vfx;

        List<int> damageTypeVfx = [primaryVfx, secondaryVfx];

        for (int x = 0; x < vfxParams.Count; x++)
        {
            if (weapon.ContainsKey(vfxParams[x]) && weapon.ContainsKey(vfxDummyParams[x]))
            {
                weapon.SetValue(vfxParams[x], damageTypeVfx[x]);
                weapon.SetValue(vfxDummyParams[x], vfxDummies[x]);
            }
        }
    }

    private int RngRandomWeighted(List<int> ids, List<int> weights)
    {
        int totalWeight = 0;
        foreach (int weight in weights)
        {
            totalWeight += weight;
        }

        int randomValue = random.NextWeightedValue(ids, weights);
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
