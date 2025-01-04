namespace DSLRNet.Core.Handlers;

using DSLRNet.Core.DAL;

public class DamageTypeHandler : BaseHandler
{
    private Configuration configuration { get; set; }

    private List<DamageTypeSetup> DamageTypes { get; set; }
    
    private readonly RandomProvider random;

    public DamageTypeHandler(
        IOptions<Configuration> configuration,
        RandomProvider random,
        ParamEditsRepository dataRepository,
        DataAccess dataAccess) : base(dataRepository)
    {
        this.configuration = configuration.Value;
        this.random = random;

        this.DamageTypes = dataAccess.DamageTypeSetup.GetAll().ToList();

        this.DamageTypes.Where(d => d.Message >= 1022 && d.Message <= 1100)
            .ToList()
            .ForEach(d =>
            {
                this.GeneratedDataRepository.AddParamEdit(
                    new ParamEdit
                    {
                        ParamName = ParamNames.TextOnly,
                        Operation = ParamOperation.TextOnly,
                        ItemText = new LootFMG()
                        {
                            Category = LootType.Weapon.ToString(),
                            Effect = d.EffectDescription
                        },
                        ParamObject = new GenericParam() { Properties = new Dictionary<string, object?>() { { "ID", d.Message } } }
                    });
            });
    }

    public DamageTypeSetup ChooseDamageTypeAtRandom(bool totallyRandom = false, bool secondaryDamage = false)
    {
        if (totallyRandom)
        {
            return this.random.GetRandomItem(this.DamageTypes);
        }

        List<WeightedValue<int>> weightedValues = this.DamageTypes.Select(d => new WeightedValue<int>
        {
            Weight = secondaryDamage ? d.SecWeight : d.PriWeight,
            Value = d.ID
        }).ToList();

        int chosenValue = this.random.NextWeightedValue(weightedValues);

        return this.DamageTypes.Single(d => d.ID == chosenValue);
    }

    public void ApplyDamageTypeWeaponSpEffects(WeaponModifications mods, GenericParam weaponDict)
    {
        List<string> speffParam = this.configuration.LootParam.Speffects.EquipParamWeapon;
        List<string> behSpeffParam = this.configuration.LootParam.WeaponBehSpeffects;

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

    public void ApplyWeaponVfxFromDamageTypes(GenericParam weapon, WeaponModifications mods)
    {
        List<string> vfxParams = this.configuration.LootParam.WeaponsVfxParam;
        List<string> vfxDummyParams = this.configuration.LootParam.WeaponsVfxDummyParam;
        List<int> vfxDummies = this.configuration.LootParam.WeaponsVfxDummies;

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
}
