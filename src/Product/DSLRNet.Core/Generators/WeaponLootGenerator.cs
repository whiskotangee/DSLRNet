namespace DSLRNet.Core.Generators;

using DSLRNet.Core.Common;
using DSLRNet.Core.Config;
using DSLRNet.Core.Contracts;
using DSLRNet.Core.DAL;
using DSLRNet.Core.Extensions;
using DSLRNet.Core.Handlers;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public class WeaponLootGenerator : ParamLootGenerator<EquipParamWeapon>
{
    private readonly AshofWarHandler ashofWarHandler;
    private readonly DamageTypeHandler damageTypeHandler;

    private Dictionary<WeaponTypes, List<EquipParamWeapon>> weaponsByWeaponType = [];

    public WeaponLootGenerator(
        IOptions<Configuration> configurationOptions,
        IOptions<Settings> settings,
        AshofWarHandler ashofWarHandler,
        RarityHandler rarityHandler,
        SpEffectHandler spEffectHandler,
        RandomProvider random,
        LoreGenerator loreGenerator,
        DamageTypeHandler damageTypeHandler,
        ParamEditsRepository dataRepository,
        DataAccess dataAccess,
        ILogger<ParamLootGenerator<EquipParamWeapon>> logger) : base(rarityHandler, spEffectHandler, loreGenerator, random, configurationOptions, settings, dataRepository, ParamNames.EquipParamWeapon, logger)
    {
        this.IDGenerator = new IDGenerator()
        {
            StartingID = 80000000,
            Multiplier = 1000,
        };
        this.ashofWarHandler = ashofWarHandler;
        this.damageTypeHandler = damageTypeHandler;
        this.DataSource = dataAccess.EquipParamWeapon;

        var weapons = this.DataSource.GetAll().Where(d => !string.IsNullOrWhiteSpace(d.Name)).ToList();

        foreach (WeaponTypes weaponType in Enum.GetValues<WeaponTypes>())
        {
            this.weaponsByWeaponType[weaponType] = weapons.Where(d => this.GetWeaponType(d.wepmotionCategory) == weaponType).ToList();
        }
    }

    public int uniqueWeaponCounter = 0;

    public int CreateWeapon(ItemLotSettings itemLotSettings, int rarityId)
    {
        bool isUniqueWeapon = this.Random.PassesPercentCheck(this.Settings.WeaponGeneratorSettings.UniqueNameChance);

        uniqueWeaponCounter += isUniqueWeapon ? 1 : 0;

        WeaponTypes weaponType = this.Random.NextWeightedValue(itemLotSettings.WeaponWeightsByType);

        EquipParamWeapon newWeapon = this.Random.GetRandomItem(this.weaponsByWeaponType[weaponType]).Clone();

        newWeapon.ID = (int)this.IDGenerator.GetNext();
        newWeapon.sellValue = this.RarityHandler.GetSellValue(rarityId);
        newWeapon.rarity = this.RarityHandler.GetRarityParamValue(rarityId);
        newWeapon.iconId = this.RarityHandler.GetIconId(newWeapon.iconId, rarityId, isUnique: isUniqueWeapon);
        newWeapon.reinforceTypeId = 0;
        
        if (weaponType != WeaponTypes.StaffsSeals)
        {
            // 42300 allows scaling from all sources (STR,DEX,INT,FTH,ARC)
            newWeapon.attackElementCorrectId = 42300;

            // 0 is for weapon without affinity (heavy, keen, etc) upgrade path
            newWeapon.reinforceTypeId = 0;

            this.ashofWarHandler.AssignAshOfWar(newWeapon);
        }

        // DSLR special upgrades path
        newWeapon.materialSetId = Settings.EquipMtrlParamStartId;

        newWeapon.gemMountType = (byte)(weaponType == WeaponTypes.StaffsSeals ? 0 : 2);
        newWeapon.disableGemAttr = 1;
        newWeapon.weight = this.RarityHandler.GetRandomizedWeight(newWeapon.weight, rarityId);

        this.SetWeaponOriginParam(newWeapon, newWeapon.ID);

        string affinity = "";

        WeaponModifications modifications = this.ApplyWeaponModifications(
            newWeapon,
            rarityId,
            weaponType);

        this.ApplyWeaponScalingRange(newWeapon, modifications, rarityId);
        this.ApplyWeaponRequiredStatChanges(newWeapon, rarityId);

        this.damageTypeHandler.ApplyDamageTypeWeaponSpEffects(modifications, newWeapon.GenericParam);

        string weaponDesc = string.Join(Environment.NewLine, modifications.SpEffectDescriptions);

        if (weaponType == WeaponTypes.Normal)
        {
            affinity = this.CreateAffinityTitle(modifications);
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

            weaponFinalTitleColored = uniqueName.WrapTextWithProperties(color: this.Settings.WeaponGeneratorSettings.UniqueItemNameColor);
        }

        // newWeapon.Name = "DSLR " + weaponFinalTitle;

        this.GeneratedDataRepository.AddParamEdit(
            new ParamEdit
            {
                ParamName = ParamNames.EquipParamWeapon,
                Operation = ParamOperation.Create,
                ItemText = new LootFMG()
                {
                    Category = this.OutputLootRealNames[LootType.Weapon],
                    Name = weaponFinalTitleColored,
                    Caption = weaponDesc + Environment.NewLine + this.LoreGenerator.GenerateDescription(weaponFinalTitle, false)
                },
                ParamObject = newWeapon.GenericParam
            });

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
        var damageParams = newWeapon.GetFieldNamesByFilter("correct", excludeFilter: "Type_").Select(d => new { ParamName = d, Value = newWeapon.GetValue<float>(d) }).ToDictionary(d => d.ParamName);

        List<string> takenParams = [];

        // reset the scalings
        foreach(string scaleParam in damageParams.Keys)
        {
            newWeapon.SetValue(scaleParam, 0);
        }

        IntValueRange statScalingRange = this.RarityHandler.GetWeaponScalingRange(rarityId);

        // set primary scaling 
        string primaryScalingParam = this.Random.GetRandomItem(damageParams.Keys.ToList());

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
        List<string> otherDamageParams = damageParams.Keys.Except(takenParams).ToList();

        foreach (string otherScalingParam in otherDamageParams)
        {
            var currentScaling = damageParams[otherScalingParam];

            float newValue = this.Random.NextInt(this.Settings.WeaponGeneratorSettings.OtherBaseScalingRange);

            newWeapon.SetValue(currentScaling.ParamName, newValue);
        }
    }

    private void ApplyWeaponRequiredStatChanges(EquipParamWeapon newWeapon, int rarityId)
    {
        var orderedCorrectParams = newWeapon.GetFieldNamesByFilter("correct", excludeFilter: "Type_")
            .Select(d => new { ParamName = d, Value = newWeapon.GetValue<float>(d) })
            .OrderByDescending(d => d.Value)
            .ToList();

        List<int> orderedProperParamValues = newWeapon.GetFieldNamesByFilter("proper")
            .Select(d => newWeapon.GetValue<int>(d))
            .OrderByDescending(d => d)
            .ToList();

        IntValueRange additionRange = RarityHandler.GetStatRequiredAdditionRange(rarityId);

        float percentValue = 1.0f - (Settings.WeaponGeneratorSettings.StatReqReductionPercent / 100.0f);

        for (int i = 0; i < orderedCorrectParams.Count; i++)
        {
            var correctParam = orderedCorrectParams[i];
            int properValue = orderedProperParamValues[i];

            var addition = Settings.WeaponGeneratorSettings.ApplyRarityStatReqAddition ? this.Random.NextInt(additionRange) : 0;

            int calculatedValue = Convert.ToInt32((properValue + addition) * percentValue);

            newWeapon.SetValue(
                correctParam.ParamName.Replace("correct", "proper"), 
                calculatedValue);
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
            && mods.PrimaryDamageType.ShieldParam != mods.SecondaryDamageType?.ShieldParam
            && mods.SecondaryDamageType != null)
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

        IEnumerable<SpEffectDetails> nameParts = this.ApplySpEffects(rarityId, [0], weapon.GenericParam, spEffectMultiplier, LootType.Weapon);

        mods.SpEffectTexts = nameParts.ToList();

        return mods;
    }

    private WeaponModifications ApplyStaffDamageChanges(EquipParamWeapon weapon, int rarityId)
    {
        //TODO : randomize sorcery/incantation scaling?
        WeaponModifications modifications = new(new DamageTypeSetup(), null);

        List<int> options = this.SpEffectHandler.GetPossibleWeaponSpeffectTypes(weapon);

        IEnumerable<SpEffectDetails> nameParts = this.ApplySpEffects(rarityId, options, weapon.GenericParam, 1.0f, LootType.Weapon);

        modifications.SpEffectTexts = nameParts.ToList();

        return modifications;
    }

    private WeaponModifications ApplyWeaponDamageChanges(DamageTypeSetup dT1, DamageTypeSetup? dT2, EquipParamWeapon weapon, int rarityId, bool isUniqueWeapon)
    {
        WeaponModifications mods = new(dT1, dT2);

        float uniqueValueMultiplier = isUniqueWeapon ? this.Settings.WeaponGeneratorSettings.UniqueWeaponMultiplier : 1.0f;

        IntValueRange dmgRange = this.RarityHandler.GetDamageAdditionRange(rarityId);

        List<string> dmgParams = weapon.GetFieldNamesByFilter("attackBase", excludeFilter: "attackBaseRepel");

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

        mods.SpEffectDescriptions = effectStrings.Where(d => !string.IsNullOrWhiteSpace(d)).Select(s => $"{this.Configuration.DSLRDescText.Effect} {s}").ToList();

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
            _ => this.ApplyWeaponDamageChanges(primary, secondary, weapon, rarityId, isUniqueWeapon),
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

