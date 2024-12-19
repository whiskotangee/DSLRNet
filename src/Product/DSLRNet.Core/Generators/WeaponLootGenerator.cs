namespace DSLRNet.Core.Generators;

using DSLRNet.Core.Common;
using DSLRNet.Core.Config;
using DSLRNet.Core.Contracts;
using DSLRNet.Core.DAL;
using DSLRNet.Core.Data;
using DSLRNet.Core.Extensions;
using DSLRNet.Core.Handlers;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

public class WeaponLootGenerator : ParamLootGenerator<EquipParamWeapon>
{
    private readonly AshofWarHandler ashofWarHandler;
    private readonly DamageTypeHandler damageTypeHandler;

    public WeaponLootGenerator(
        IOptions<Configuration> configuration,
        IOptions<Settings> settings,
        AshofWarHandler ashofWarHandler,
        RarityHandler rarityHandler,
        SpEffectHandler spEffectHandler,
        RandomProvider random,
        LoreGenerator loreGenerator,
        DamageTypeHandler damageTypeHandler,
        ParamEditsRepository dataRepository,
        DataAccess dataAccess,
        ILogger<ParamLootGenerator<EquipParamWeapon>> logger) : base(rarityHandler, spEffectHandler, loreGenerator, random, configuration, settings, dataRepository, ParamNames.EquipParamWeapon, logger)
    {
        this.CumulativeID = new CumulativeID(logger);
        this.ashofWarHandler = ashofWarHandler;
        this.damageTypeHandler = damageTypeHandler;
        this.DataSource = dataAccess.EquipParamWeapon;
    }

    public EquipParamWeapon GetNewWeapon(WeaponTypes type)
    {
        bool found = false;
        do
        {
            var weaponCandidate = this.GetNewLootItem();
            if (this.GetWeaponType(weaponCandidate.wepmotionCategory) == type)
            {
                return weaponCandidate;
            }
        }
        while (!found);

        // we didn't find something, return whatever comes
        return this.GetNewLootItem();
    }

    public int uniqueWeaponCounter = 0;

    public int CreateWeapon(ItemLotSettings itemLotSettings, int rarityId)
    {
        bool isUniqueWeapon = this.Random.PassesPercentCheck(this.Settings.WeaponGeneratorSettings.UniqueNameChance);

        uniqueWeaponCounter += isUniqueWeapon ? 1 : 0;

        WeaponTypes weaponType = this.Random.NextWeightedValue(itemLotSettings.WeaponWeightsByType);

        EquipParamWeapon newWeapon = this.GetNewWeapon(weaponType);

        newWeapon.ID = (int)this.CumulativeID.GetNext();
        newWeapon.sellValue = this.RarityHandler.GetSellValue(rarityId);
        newWeapon.rarity = this.RarityHandler.GetRarityParamValue(rarityId);
        newWeapon.iconId = this.RarityHandler.GetIconId(newWeapon.iconId, rarityId, isUnique: isUniqueWeapon);

        // 42300 allows scaling from all sources (STR,DEX,INT,FTH,ARC)
        if (weaponType != WeaponTypes.StaffsSeals)
        {
            newWeapon.attackElementCorrectId = 42300;
        }

        newWeapon.gemMountType = (byte)(weaponType == WeaponTypes.StaffsSeals ? 0 : 2);
        newWeapon.disableGemAttr = 1;
        newWeapon.weight = this.RarityHandler.GetRandomizedWeight(newWeapon.weight, rarityId);

        this.ApplyWeaponRequiredStatChanges(newWeapon, rarityId);
        this.SetWeaponOriginParam(newWeapon, newWeapon.ID);

        string affinity = "";

        WeaponModifications modifications = this.ApplyWeaponModifications(
            newWeapon,
            rarityId,
            weaponType);

        this.ApplyWeaponScalingRange(newWeapon, modifications, rarityId);


        this.damageTypeHandler.ApplyDamageTypeWeaponSpEffects(modifications, newWeapon.GenericParam);

        string weaponDesc = string.Join(Environment.NewLine, modifications.SpEffectDescriptions);

        if (weaponType == WeaponTypes.Normal)
        {
            affinity = this.CreateAffinityTitle(modifications);
        }

        if (newWeapon.reinforceTypeId != 2200 && weaponType != WeaponTypes.StaffsSeals)
        {
            this.ashofWarHandler.AssignAshOfWar(newWeapon);
        }

        string weaponOriginalTitle = newWeapon.Name;

        string weaponFinalTitle = this.CreateLootTitle(
            weaponOriginalTitle,
            rarityId,
            affinity,
            modifications.SpEffectTexts);

        string weaponFinalTitleColored = this.CreateLootTitle(
            weaponOriginalTitle,
            rarityId,
            affinity,
            modifications.SpEffectTexts,
            true);

        if (isUniqueWeapon)
        {
            string uniqueName = this.LoreGenerator.CreateRandomUniqueName(weaponType == WeaponTypes.Shields);

            weaponFinalTitleColored = uniqueName.WrapTextWithProperties(color: this.Settings.ItemLotGeneratorSettings.UniqueItemColor);
        }

        //weaponDictionary.SetValue("Name", "DSLR " + weaponFinalTitle);

        this.AddLootDetails(
            newWeapon.GenericParam, 
            LootType.Weapon, 
            weaponFinalTitleColored, 
            weaponDesc + Environment.NewLine + this.LoreGenerator.GenerateDescription(weaponFinalTitle, false));

        return newWeapon.ID;
    }

    public string CreateAffinityTitle(WeaponModifications modifications)
    {
        List<string> names =
        [
            modifications.PrimaryDamageType.PriName,
            modifications.SecondaryDamageType?.SecName
        ];

        if (string.IsNullOrEmpty(names[0]))
        {
            names[0] = modifications.PrimaryDamageType.SecName != modifications.SecondaryDamageType?.SecName
                        ? modifications.PrimaryDamageType.SecName
                        : string.Empty;
        }

        if (modifications.PrimaryDamageType.SecName == modifications.SecondaryDamageType?.SecName)
        {
            names[1] = string.Empty;
        }

        return string.Join(' ', names.Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    private void SetWeaponOriginParam(EquipParamWeapon weapon, int id, int upgradeCap = 25)
    {
        List<string> originParams =
            Enumerable.Range(1, upgradeCap)
            .Where(d => weapon.GenericParam.ContainsKey($"originEquipWep{d}") &&
                        weapon.GetValue<int>($"originEquipWep{d}") > 0)
            .Select(d => $"originEquipWep{d}").ToList();

        weapon.originEquipWep = id;

        foreach (string param in originParams)
        {
            if (weapon.GetValue<uint>(param) > 0)
            {
                weapon.SetValue(param, id);
            }
        }
    }

    private void ApplyWeaponScalingRange(EquipParamWeapon newWeapon, WeaponModifications modifications, int rarityId)
    {
        var damageParams = newWeapon.GetFieldNamesByFilter("correct").Select(d => new { ParamName = d, Value = newWeapon.GetValue<float>(d) }).ToDictionary(d => d.ParamName);

        List<string> takenParams = [];

        // reset the scalings
        foreach(var scaleParam in damageParams.Keys)
        {
            newWeapon.SetValue(scaleParam, 0);
        }

        var statScalingRange = this.RarityHandler.GetWeaponScalingRange(rarityId);

        // set primary scaling 
        var primaryScalingParam = this.Random.GetRandomItem(damageParams.Keys.ToList());

        takenParams.Add(primaryScalingParam);

        newWeapon.SetValue(primaryScalingParam, this.Random.NextInt(statScalingRange) + this.Random.NextInt(this.Settings.WeaponGeneratorSettings.PrimaryBaseScalingRange));

        // set secondary scaling if applicable
        string? secondaryScalingParam = null;

        if (modifications.SecondaryDamageType != null)
        {
            secondaryScalingParam = this.Random.GetRandomItem(damageParams.Keys.Except([primaryScalingParam]).ToList());

            takenParams.Add(secondaryScalingParam);

            newWeapon.SetValue(secondaryScalingParam, this.Random.NextInt(statScalingRange) + this.Random.NextInt(this.Settings.WeaponGeneratorSettings.SecondaryBaseScalingRange));
        }

        // set other stat scalings
        var otherDamageParams = damageParams.Keys.Except(takenParams).ToList();

        foreach (string otherScalingParam in otherDamageParams)
        {
            var currentScaling = damageParams[otherScalingParam];

            float newValue = this.Random.NextInt(this.Settings.WeaponGeneratorSettings.OtherBaseScalingRange);

            newWeapon.SetValue(currentScaling.ParamName, newValue);
        }
    }

    private void ApplyWeaponRequiredStatChanges(EquipParamWeapon newWeapon, int rarityId)
    {
        var requirementStats = newWeapon.GetFieldNamesByFilter("proper");

        var originalValues = requirementStats.Select(newWeapon.GetValue<int>).ToList();

        if (originalValues.Count < requirementStats.Count)
        {
            originalValues.AddRange(Enumerable.Repeat(0, requirementStats.Count - originalValues.Count));
        }

        var additionRange = RarityHandler.GetStatRequiredAdditionRange(rarityId);

        var randomizedStats = this.Random.GetRandomizedList(originalValues);

        for (int i = 0; i < requirementStats.Count; i++)
        {
            string? req = requirementStats[i];
            newWeapon.SetValue<int>(req, originalValues[i] + this.Random.NextInt(additionRange));
        }
    }

    private WeaponModifications ApplyShieldCutRateChanges(DamageTypeSetup dT1, DamageTypeSetup? dT2, EquipParamWeapon weapon, int rarityId)
    {
        WeaponModifications mods = new(dT1, dT2);

        float spEffectMultiplier = 1.00f;
        float existingValue = weapon.GetValue<float>(mods.PrimaryDamageType.ShieldParam);
        float? existingSecondaryValue = mods.SecondaryDamageType != null ? weapon.GetValue<float>(mods.SecondaryDamageType.ShieldParam) : null;

        float guardRateMultiplier = this.Random.Next(this.RarityHandler.GetShieldGuardRateRange(rarityId));

        float value = Math.Clamp(existingValue * guardRateMultiplier, 0, 100);

        weapon.SetValue(mods.PrimaryDamageType.ShieldParam, value);

        if (existingSecondaryValue != null
            && mods.PrimaryDamageType.ShieldParam != mods.SecondaryDamageType.ShieldParam)
        {
            float secondaryValue = Math.Clamp((float)(existingSecondaryValue + this.Random.Next(new FloatValueRange(.1f, 5.0f))), 0, 100);
            weapon.SetValue(mods.SecondaryDamageType.ShieldParam, secondaryValue);
            mods.SecondaryDamageValue = secondaryValue;

        }
        else if (mods.PrimaryDamageType.SpEffect == mods.SecondaryDamageType?.SpEffect)
        {
            spEffectMultiplier += 0.2f;
        }

        mods.PrimaryDamageValue = value;

        IEnumerable<SpEffectText> nameParts = this.ApplySpEffects(rarityId, [0], weapon.GenericParam, spEffectMultiplier, LootType.Weapon);

        mods.SpEffectTexts = nameParts.ToList();

        return mods;
    }

    private WeaponModifications ApplyStaffDamageChanges(EquipParamWeapon weapon, int rarityId)
    {
        //TODO : randomize sorcery/incantation scaling?
        WeaponModifications modifications = new(new DamageTypeSetup(), null);

        List<int> options = this.SpEffectHandler.GetPossibleWeaponSpeffectTypes(weapon);

        IEnumerable<SpEffectText> nameParts = this.ApplySpEffects(rarityId, options, weapon.GenericParam, 1.0f, LootType.Weapon);

        modifications.SpEffectTexts = nameParts.ToList();

        return modifications;
    }

    private WeaponModifications ApplyNormalDamageChanges(DamageTypeSetup dT1, DamageTypeSetup? dT2, EquipParamWeapon weapon, int rarityId, bool isUniqueWeapon)
    {
        WeaponModifications mods = new(dT1, dT2);

        float uniqueValueMultiplier = isUniqueWeapon ? this.Settings.WeaponGeneratorSettings.UniqueWeaponMultiplier : 1.0f;

        IntValueRange dmgRange = this.RarityHandler.GetDamageAdditionRange(rarityId);

        List<string> dmgParams = weapon.GetFieldNamesByFilter("attackBase");

        long maxOriginalValue = dmgParams.Max(weapon.GetValue<long>);

        // reset all damage
        foreach (string dmgParam in dmgParams)
        {
            weapon.SetValue(dmgParam, 0f);
        }

        float overallMultiplier = mods.PrimaryDamageType.OverallMultiplier;
        if (mods.SecondaryDamageType != null)
        {
            overallMultiplier = Math.Min(mods.PrimaryDamageType.OverallMultiplier, mods.SecondaryDamageType.OverallMultiplier);
        }
        else
        {
            // no secondary damage type, let's amp up the primary damage a bit
            overallMultiplier += .1f;
        }

        if (isUniqueWeapon)
        {
            overallMultiplier *= uniqueValueMultiplier;
        }

        int primaryDamage = (int)(this.Random.NextInt(dmgRange) * overallMultiplier);

        int secondaryDamage = (int)(this.Random.NextInt(dmgRange) * overallMultiplier);

        if (mods.SecondaryDamageType != null)
        {
            weapon.SetValue(mods.SecondaryDamageType.Param, (maxOriginalValue / 4) + secondaryDamage);
        }

        if (mods.SecondaryDamageType == null ||
            mods.PrimaryDamageType.Param == mods.SecondaryDamageType?.Param)
        {
            weapon.SetValue(mods.PrimaryDamageType.Param, maxOriginalValue + primaryDamage);
        }
        else
        {
            weapon.SetValue(mods.PrimaryDamageType.Param, maxOriginalValue + primaryDamage);
        }

        mods.PrimaryDamageValue = primaryDamage;
        mods.SecondaryDamageValue = secondaryDamage;

        List<int> options = this.SpEffectHandler.GetPossibleWeaponSpeffectTypes(weapon);

        this.ApplySpEffects(rarityId, options, weapon.GenericParam, 1.0f, LootType.Weapon);

        List<string> passiveParams = this.GetPassiveSpEffectFieldNames();

        List<string> behSpEffectSlots = this.Configuration.LootParam.WeaponBehSpeffects;

        List<string> weaponEffectTextParams = this.Configuration.LootParam.SpeffectMsg;

        weapon.SetValue(passiveParams[0], mods.PrimaryDamageType.SpEffect);
        weapon.SetValue(behSpEffectSlots[0], mods.PrimaryDamageType.OnHitSpEffect);
        weapon.SetValue(weaponEffectTextParams[0], mods.PrimaryDamageType.Message);

        if (mods.SecondaryDamageType != null)
        {
            weapon.SetValue(passiveParams[1], mods.SecondaryDamageType.SpEffect);
            weapon.SetValue(behSpEffectSlots[1], mods.SecondaryDamageType.OnHitSpEffect);
            weapon.SetValue(weaponEffectTextParams[1], mods.SecondaryDamageType.Message);
        }

        List<string> effectStrings = [];
        if (!string.IsNullOrEmpty(mods.PrimaryDamageType.EffectDescription))
        {
            effectStrings.Add(mods.PrimaryDamageType.EffectDescription);
        }

        if (mods.SecondaryDamageType != null)
        {
            effectStrings.Add(mods.SecondaryDamageType.EffectDescription);
        }

        mods.SpEffectDescriptions = effectStrings.Select(s => $"Effect: {s}").ToList();

        byte hitVfx = mods.PrimaryDamageType.HitEffectCategory;
        if (mods.SecondaryDamageType != null && mods.SecondaryDamageType.HitEffectCategory > 0)
        {
            hitVfx = mods.SecondaryDamageType.HitEffectCategory;
        }

        weapon.spAttribute = hitVfx;

        float critMultiplier = mods.PrimaryDamageType.CriticalMultAddition + mods.SecondaryDamageType?.CriticalMultAddition ?? 0;

        int critValue = 0;

        if (critMultiplier > 0.0)
        {
            critValue = this.Random.NextInt(this.Settings.WeaponGeneratorSettings.CritChanceRange);
        }

        weapon.throwAtkRate = (short)(critValue * (1 + critMultiplier));

        float maxDamage = dmgParams.Max(par => weapon.GetValue<float>(par));

        if (maxDamage > this.Settings.WeaponGeneratorSettings.DamageIncreasesStaminaThreshold)
        {
            weapon.staminaConsumptionRate = (float)MathFunctions.RoundToXDecimalPlaces((float)(weapon.staminaConsumptionRate + this.Random.Next(0.05, 0.3)), 3);
        }

        this.damageTypeHandler.ApplyWeaponVfxFromDamageTypes(weapon.GenericParam, mods);

        return mods;
    }

    private WeaponModifications ApplyWeaponModifications(EquipParamWeapon weapon, int rarityId, WeaponTypes weaponType, bool isUniqueWeapon = false)
    {
        // randomize damage type
        DamageTypeSetup primary = this.damageTypeHandler.ChooseDamageTypeAtRandom(this.Settings.ItemLotGeneratorSettings.ChaosLootEnabled, false);
        DamageTypeSetup? secondary = null;

        if (this.Random.PassesPercentCheck(this.Settings.WeaponGeneratorSettings.SplitDamageTypeChance))
        {
            secondary = this.damageTypeHandler.ChooseDamageTypeAtRandom(this.Settings.ItemLotGeneratorSettings.ChaosLootEnabled, true);
        }

        return weaponType switch
        {
            WeaponTypes.StaffsSeals => this.ApplyStaffDamageChanges(weapon, rarityId),
            WeaponTypes.Shields => this.ApplyShieldCutRateChanges(primary, secondary, weapon, rarityId),
            _ => this.ApplyNormalDamageChanges(primary, secondary, weapon, rarityId, isUniqueWeapon),
        };
    }

    private WeaponTypes GetWeaponType(int motionCategory)
    {
        if (this.Configuration.LootParam.WeaponSpecialMotionCategories.BowsCrossbows.Contains(motionCategory))
        {
            return WeaponTypes.BowsCrossbows;
        }

        if (this.Configuration.LootParam.WeaponSpecialMotionCategories.StaffsSeals.Contains(motionCategory))
        {
            return WeaponTypes.StaffsSeals;
        }

        if (this.Configuration.LootParam.WeaponSpecialMotionCategories.Shields.Contains(motionCategory))
        {
            return WeaponTypes.Shields;
        }

        return WeaponTypes.Normal;
    }
}

