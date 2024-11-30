namespace DSLRNet.Core.Generators;

using DSLRNet.Core;
using DSLRNet.Core.Common;
using DSLRNet.Core.Config;
using DSLRNet.Core.Contracts;
using DSLRNet.Core.Contracts.Params;
using DSLRNet.Core.Data;
using DSLRNet.Core.Handlers;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

public class WeaponLootGenerator : ParamLootGenerator
{
    // WEAPONTYPES
    private readonly WeaponGeneratorConfig weaponGeneratorConfig;
    private readonly AshofWarHandler ashofWarHandler;

    public WeaponLootGenerator(
        IOptions<Configuration> configuration,
        IOptions<WeaponGeneratorConfig> weaponGeneratorConfig,
        AshofWarHandler ashofWarHandler,
        RarityHandler rarityHandler,
        AllowListHandler whitelistHandler,
        SpEffectHandler spEffectHandler,
        RandomNumberGetter random,
        LoreGenerator loreGenerator,
        DamageTypeHandler damageTypeHandler,
        DataRepository dataRepository) : base(rarityHandler, whitelistHandler, spEffectHandler, damageTypeHandler, loreGenerator, random, configuration, dataRepository, ParamNames.EquipParamWeapon)
    {
        CumulativeID = new CumulativeID();
        this.weaponGeneratorConfig = weaponGeneratorConfig.Value;
        this.ashofWarHandler = ashofWarHandler;

        List<EquipParamWeapon> weaponLoots = Csv.LoadCsv<EquipParamWeapon>("DefaultData\\ER\\CSVs\\EquipParamWeapon.csv");

        LoadedLoot = weaponLoots.Select(GenericDictionary.FromObject).ToList();
    }

    public int CreateWeapon(int rarityId, List<int> whitelistLootIds = null)
    {
        whitelistLootIds ??= [];

        if (whitelistLootIds.Count == 0)
        {
            whitelistLootIds.Add(100);
        }

        bool uniqueWeapon = Random.GetRandomBoolByPercent(weaponGeneratorConfig.UniqueNameChance);
        float uniqueValueMultiplier = uniqueWeapon ? weaponGeneratorConfig.UniqueWeaponMultiplier : 1.0f;

        WeaponTypes goalWeaponType = Random.NextWeightedValue(weaponGeneratorConfig.Types, weaponGeneratorConfig.Weights, 1.0f);

        GenericDictionary weaponDictionary = GetLootDictionaryFromId(WhiteListHandler.GetLootByWhiteList(whitelistLootIds, LootType.Weapon));

        string affinity = "";

        WeaponTypes generatedType = GetWeaponType(weaponDictionary.GetValue<int>(Configuration.LootParam.WeaponsWepMotionCategory));

        // randomize damage type
        DamageTypeSetup DT1 = DamageTypeHandler.ChooseDamageTypeAtRandom(Configuration.Settings.ChaosLootEnabled, false);
        DamageTypeSetup DT2 = DamageTypeHandler.ChooseDamageTypeAtRandom(Configuration.Settings.ChaosLootEnabled, true);

        if (!Random.GetRandomBoolByPercent(weaponGeneratorConfig.SplitDamageTypeChance))
        {
            DT2 = DT1;
        }

        DamageTypeAddition DTAdditions = DamageTypeAddition.CreateEmpty();

        ApplyWeaponDamageAdditions(
            DT1,
            DT2,
            DTAdditions,
            weaponDictionary,
            rarityId,
            generatedType);

        DamageTypeHandler.ApplyDamageTypeWeaponSpEffects(DT1, DT2, weaponDictionary);

        string weaponDesc = string.Join(Environment.NewLine, DTAdditions.SpEffectDescriptions);

        if (generatedType == WeaponTypes.Normal)
        {
            affinity = CreateAffinityTitle(DT1, DT2);
        }

        if (weaponDictionary.GetValue<int>(Configuration.LootParam.WeaponReinforceTypeId) != 2200 && generatedType != WeaponTypes.StaffsSeals)
        {
            ashofWarHandler.AssignAshOfWar(weaponDictionary);
        }

        SetLootSellValue(weaponDictionary, rarityId, uniqueValueMultiplier);
        SetLootRarityParamValue(weaponDictionary, rarityId);
        weaponDictionary.SetValue("iconId", RarityHandler.GetIconIdForRarity(weaponDictionary.GetValue<int>("iconId"), rarityId, isUnique: uniqueWeapon));
        RandomizeLootWeightBasedOnRarity(weaponDictionary, rarityId);
        ApplyNextId(weaponDictionary);

        // 42300 LETS WEAPONS TAKE DAMAGE SCALING FROM ALL POSSIBLE SOURCES (STR,DEX,INT,FTH) 
        weaponDictionary.SetValue("attackElementCorrectId", 42300);
        /* SETUP A FEW WEPMOTIONCATEGORIES THAT ABSOLUTELY SHOULD NOT BE GIVEN AOWS LIKE STAFFS/SEALS
		    const bannedfromaows : Array = [31,41]
        */
        weaponDictionary.SetValue("gemMountType", generatedType == WeaponTypes.StaffsSeals ? 0 : 2);
        // PREVENT CHANGING AFFINITY
        weaponDictionary.SetValue(Configuration.LootParam.NoAffinityChange, 1);

        ApplyWeaponScalingRange(weaponDictionary, rarityId);
        SetWeaponOriginParam(weaponDictionary, weaponDictionary.GetValue<int>("ID"), replace: true);

        string weaponOriginalTitle = weaponDictionary.GetValue<string>("Name");

        string weaponFinalTitle = CreateLootTitle(
            weaponOriginalTitle,
            rarityId,
            affinity,
            DTAdditions.SpEffectTexts);

        string weaponFinalTitleColored = CreateLootTitle(
            weaponOriginalTitle,
            rarityId,
            affinity,
            DTAdditions.SpEffectTexts,
            true);

        if (uniqueWeapon)
        {
            string uniqueName = LoreGenerator.CreateRandomUniqueName(generatedType == WeaponTypes.Shields);
            if (!string.IsNullOrEmpty(uniqueName))
            {
                weaponFinalTitleColored = $"<font color=\"#ffa3c5\">{uniqueName}</font>";
            }
            else
            {
                Log.Logger.Error("GENERATED UNIQUENAME WAS EMPTY!");
            }
        }

        //weaponDictionary.SetValue("Name", "DSLR " + weaponFinalTitle);

        ExportLootGenParamAndTextToOutputs(weaponDictionary, LootType.Weapon, weaponFinalTitleColored, weaponDesc + Environment.NewLine + GetParamLootLore(weaponFinalTitle, false), "", [], []);

        return weaponDictionary.GetValue<int>("ID");
    }

    private void SetWeaponOriginParam(GenericDictionary weaponDictionary, int id, int upgradeCap = 25, bool replace = true)
    {
        List<string> originParams =
            Enumerable.Range(1, upgradeCap)
            .Where(d => weaponDictionary.ContainsKey($"{Configuration.LootParam.WeaponOriginParamBase}{d}") &&
                        weaponDictionary.GetValue<int>($"{Configuration.LootParam.WeaponOriginParamBase}{d}") > 0)
            .Select(d => $"{Configuration.LootParam.WeaponOriginParamBase}{d}").ToList();

        weaponDictionary.SetValue(Configuration.LootParam.WeaponOriginParamBase, id);

        if (replace)
        {
            foreach (string? param in originParams)
            {
                if (weaponDictionary.GetValue<int>(param) > 0)
                {
                    weaponDictionary.SetValue(param, id);
                }
            }
        }
        else
        {
            foreach (string? param in originParams)
            {
                weaponDictionary.SetValue(param, id);
            }
        }
    }

    private void ApplyWeaponScalingRange(GenericDictionary weaponDictionary, int rarityId)
    {
        var currentScalings = Configuration.LootParam.WeaponsScaling.Select(d => new { ParamName = d, Value = weaponDictionary.GetValue<float>(d) }).ToDictionary(d => d.ParamName);
        List<int> scalingRange = RarityHandler.GetRarityDamageAdditionRange(rarityId);

        foreach (string scaling in currentScalings.Keys)
        {
            var currentScaling = currentScalings[scaling];

            float newValue = (float)Math.Max(currentScaling.Value, Random.NextDouble(15.0, 25.0));

            weaponDictionary.SetValue(currentScaling.ParamName, newValue);
        }

        double primaryAddition = MathFunctions.RoundToXDecimalPlaces((float)Random.NextDouble(scalingRange[0], scalingRange[1]) * 0.6f, 2);

        var maxParam = currentScalings.MaxBy(d => d.Value.Value).Value;

        weaponDictionary.SetValue(maxParam.ParamName, maxParam.Value + primaryAddition);

        // randomly choose secondary stat and apply
        string randomScalingKey = Random.GetRandomItem(currentScalings.Keys.Except([maxParam.ParamName]).ToList());

        var randomScaling = currentScalings[randomScalingKey];

        weaponDictionary.SetValue(randomScaling.ParamName, Math.Clamp(Random.NextDouble(scalingRange[0], scalingRange[1]) * .5f, 0, 130));
    }

    private void ApplyShieldCutRateChanges(DamageTypeSetup dT1, DamageTypeSetup dT2, DamageTypeAddition dTAdditions, GenericDictionary weaponDictionary, int rarityId)
    {
        float spEffectMultiplier = 1.0f;
        float existingValue = weaponDictionary.GetValue<float>(dT1.ShieldParam);

        if (dT1.SpEffect == existingValue)
        {
            spEffectMultiplier += 0.2f / 2;
        }
        if (dT1 == dT2)
        {
            spEffectMultiplier += 0.2f;
        }

        float value = Math.Clamp(existingValue * spEffectMultiplier, 0, 100);

        weaponDictionary.SetValue(dT1.ShieldParam, value);
        weaponDictionary.SetValue(dT2.ShieldParam, value);

        dTAdditions.PrimaryDamageType.Value = value;
        dTAdditions.SecondaryDamageType.Value = value;

        IEnumerable<SpEffectText> nameParts = ApplySpEffects(rarityId, [0], weaponDictionary, spEffectMultiplier, true);

        dTAdditions.SpEffectTexts = nameParts.ToList();
    }

    private void ApplyStaffDamageChanges(DamageTypeSetup dT1, DamageTypeSetup dT2, DamageTypeAddition dTAdditions, GenericDictionary weaponDictionary, int rarityId)
    {
        List<int> options = SpEffectHandler.GetPossibleWeaponSpeffectTypes(weaponDictionary);

        IEnumerable<SpEffectText> nameParts = ApplySpEffects(rarityId, options, weaponDictionary, 1.0f, true);

        dTAdditions.SpEffectTexts = nameParts.ToList();
    }

    private void ApplyNormalDamageChanges(DamageTypeSetup dT1, DamageTypeSetup dT2, DamageTypeAddition dTAdditions, GenericDictionary weaponDictionary, int rarityId)
    {
        List<int> dmgRange = RarityHandler.GetRarityDamageAdditionRange(rarityId);

        List<string> dmgParams = Configuration.LootParam.WeaponsDamageParam;

        long maxValue = 0;

        GenericDictionary originalValues = weaponDictionary.Clone() as GenericDictionary;
        // reset all damage
        foreach (string dmgParam in dmgParams)
        {
            weaponDictionary.SetValue<long>(dmgParam, 0);
            maxValue = Math.Max(originalValues.GetValue<long>(dmgParam), maxValue);
        }

        float overallMultiplier = Math.Min(dT1.OverallMultiplier, dT2.OverallMultiplier);

        int primaryDamage = (int)(Random.NextInt(dmgRange[0], dmgRange[1]) * overallMultiplier);

        int secondaryDamage = (int)(Random.NextInt(dmgRange[0], dmgRange[1]) * overallMultiplier);

        if (dT1.Param == dT2.Param)
        {
            weaponDictionary.SetValue(dT1.Param, (long)1.2 * maxValue + primaryDamage);
        }
        else
        {
            weaponDictionary.SetValue(dT2.Param, maxValue / 2 + secondaryDamage);
        }

        dTAdditions.PrimaryDamageType.Value = primaryDamage;
        dTAdditions.SecondaryDamageType.Value = secondaryDamage;

        List<int> options = SpEffectHandler.GetPossibleWeaponSpeffectTypes(weaponDictionary);

        ApplySpEffects(rarityId, options, weaponDictionary);

        List<string> passiveParams = GetPassiveSpEffectSlotArrayFromOutputParamName();

        List<string> behSpEffectSlots = Configuration.LootParam.WeaponBehSpeffects;

        List<string> weaponEffectTextParams = Configuration.LootParam.SpeffectMsg;

        weaponDictionary.SetValue(passiveParams[0], dT1.SpEffect);
        weaponDictionary.SetValue(behSpEffectSlots[0], dT1.OnHitSpEffect);
        weaponDictionary.SetValue(weaponEffectTextParams[0], dT1.Message);

        if (dT1 != dT2)
        {
            weaponDictionary.SetValue(passiveParams[1], dT2.SpEffect);
            weaponDictionary.SetValue(behSpEffectSlots[1], dT2.OnHitSpEffect);
            weaponDictionary.SetValue<long>(weaponEffectTextParams[1], dT2.Message);
        }

        List<string> effectStrings = [];
        if (!string.IsNullOrEmpty(dT1.EffectDescription))
        {
            effectStrings.Add(dT1.EffectDescription);
        }
        if (dT1 != dT2 && !string.IsNullOrEmpty(dT2.EffectDescription))
        {
            effectStrings.Add(dT2.EffectDescription);
        }

        dTAdditions.SpEffectDescriptions = effectStrings.Select(s => $"Effect: {s}").ToList();

        string hitVfxParam = Configuration.LootParam.WeaponsHitVfx;
        int hitVfx = dT1.HitEffectCategory != 0 ? dT1.HitEffectCategory : dT2.HitEffectCategory;

        weaponDictionary.SetValue<long>(hitVfxParam, hitVfx);

        string critParam = Configuration.LootParam.WeaponsThrowDamageParam;
        float critMultiplier = dT1.CriticalMultAddition + dT2.CriticalMultAddition;

        int critValue = 0;

        if (critMultiplier > 0.0)
        {
            critValue = Random.NextInt(5, 20);
        }

        weaponDictionary.SetValue(critParam, (int)(critValue * (1 + critMultiplier)));

        string staminaParam = Configuration.LootParam.WeaponsStaminaRate;
        int maxDamage = Configuration.LootParam.WeaponsDamageParam.Max(par => weaponDictionary.GetValue<int>(par));

        if (weaponDictionary.ContainsKey(staminaParam) && maxDamage > 170)
        {
            weaponDictionary.SetValue(staminaParam, MathFunctions.RoundToXDecimalPlaces((float)(weaponDictionary.GetValue<float>(staminaParam) + Random.NextDouble(0.05, 0.3)), 3));
        }

        DamageTypeHandler.ApplyWeaponVfxFromDamageTypes(weaponDictionary, dT1, dT2);
    }

    private void ApplyWeaponDamageAdditions(DamageTypeSetup dT1, DamageTypeSetup dT2, DamageTypeAddition dTAdditions, GenericDictionary weaponDictionary, int rarityId, WeaponTypes weaponType)
    {
        switch (weaponType)
        {
            case WeaponTypes.Normal:
            case WeaponTypes.BowsCrossbows:
                ApplyNormalDamageChanges(dT1, dT2, dTAdditions, weaponDictionary, rarityId);
                break;
            case WeaponTypes.StaffsSeals:
                ApplyStaffDamageChanges(dT1, dT2, dTAdditions, weaponDictionary, rarityId);
                break;
            case WeaponTypes.Shields:
                ApplyShieldCutRateChanges(dT1, dT2, dTAdditions, weaponDictionary, rarityId);
                break;
        }
    }

    private WeaponTypes GetWeaponType(int motionCategory)
    {
        if (Configuration.LootParam.WeaponSpecialMotionCategories.BowsCrossbows.Contains(motionCategory))
        {
            return WeaponTypes.BowsCrossbows;
        }

        if (Configuration.LootParam.WeaponSpecialMotionCategories.StaffsSeals.Contains(motionCategory))
        {
            return WeaponTypes.StaffsSeals;
        }

        if (Configuration.LootParam.WeaponSpecialMotionCategories.Shields.Contains(motionCategory))
        {
            return WeaponTypes.Shields;
        }

        return WeaponTypes.Normal;
    }
}

