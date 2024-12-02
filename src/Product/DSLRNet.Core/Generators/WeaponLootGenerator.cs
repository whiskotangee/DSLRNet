namespace DSLRNet.Core.Generators;

using DSLRNet.Core.Common;
using DSLRNet.Core.Config;
using DSLRNet.Core.Contracts;
using DSLRNet.Core.Contracts.Params;
using DSLRNet.Core.Data;
using DSLRNet.Core.Extensions;
using DSLRNet.Core.Handlers;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

public class WeaponLootGenerator : ParamLootGenerator<EquipParamWeapon>
{
    // WEAPONTYPES
    private readonly WeaponGeneratorConfig weaponGeneratorConfig;
    private readonly AshofWarHandler ashofWarHandler;
    private readonly DamageTypeHandler damageTypeHandler;

    public WeaponLootGenerator(
        IOptions<Configuration> configuration,
        IOptions<WeaponGeneratorConfig> weaponGeneratorConfig,
        AshofWarHandler ashofWarHandler,
        RarityHandler rarityHandler,
        AllowListHandler whitelistHandler,
        SpEffectHandler spEffectHandler,
        RandomProvider random,
        LoreGenerator loreGenerator,
        DamageTypeHandler damageTypeHandler,
        ParamEditsRepository dataRepository,
        IDataSource<EquipParamWeapon> weaponDataSource) : base(rarityHandler, whitelistHandler, spEffectHandler, loreGenerator, random, configuration, dataRepository, ParamNames.EquipParamWeapon)
    {
        CumulativeID = new CumulativeID();
        this.weaponGeneratorConfig = weaponGeneratorConfig.Value;
        this.ashofWarHandler = ashofWarHandler;
        this.damageTypeHandler = damageTypeHandler;

        DataSource = weaponDataSource;
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

        WeaponTypes goalWeaponType = Random.NextWeightedValue(weaponGeneratorConfig.Types, weaponGeneratorConfig.Weights);

        EquipParamWeapon newWeapon = GetLootDictionaryFromId(WhiteListHandler.GetLootByAllowList(whitelistLootIds, LootType.Weapon));

        string affinity = "";

        WeaponTypes generatedType = GetWeaponType(newWeapon.GenericParam.GetValue<int>(Configuration.LootParam.WeaponsWepMotionCategory));

        WeaponModifications modifications = ApplyWeaponModifications(
            newWeapon.GenericParam,
            rarityId,
            generatedType);

        this.damageTypeHandler.ApplyDamageTypeWeaponSpEffects(modifications, newWeapon.GenericParam);

        string weaponDesc = string.Join(Environment.NewLine, modifications.SpEffectDescriptions);

        if (generatedType == WeaponTypes.Normal)
        {
            affinity = CreateAffinityTitle(modifications);
        }

        if (newWeapon.reinforceTypeId != 2200 && generatedType != WeaponTypes.StaffsSeals)
        {
            this.ashofWarHandler.AssignAshOfWar(newWeapon);
        }

        SetLootSellValue(newWeapon.GenericParam, rarityId, uniqueValueMultiplier);
        SetLootRarityParamValue(newWeapon.GenericParam, rarityId);

        newWeapon.iconId = RarityHandler.GetIconIdForRarity(newWeapon.iconId, rarityId, isUnique: uniqueWeapon);

        RandomizeLootWeightBasedOnRarity(newWeapon.GenericParam, rarityId);
        newWeapon.ID = CumulativeID.GetNext();

        // 42300 LETS WEAPONS TAKE DAMAGE SCALING FROM ALL POSSIBLE SOURCES (STR,DEX,INT,FTH) 
        if (generatedType != WeaponTypes.StaffsSeals)
        {
            newWeapon.attackElementCorrectId = 42300;
        }

        newWeapon.gemMountType = generatedType == WeaponTypes.StaffsSeals ? 0 : 2;
        newWeapon.disableGemAttr = 1;

        ApplyWeaponScalingRange(newWeapon.GenericParam, rarityId);
        SetWeaponOriginParam(newWeapon.GenericParam, newWeapon.ID, replace: true);

        string weaponOriginalTitle = newWeapon.Name;

        string weaponFinalTitle = CreateLootTitle(
            weaponOriginalTitle,
            rarityId,
            affinity,
            modifications.SpEffectTexts);

        string weaponFinalTitleColored = CreateLootTitle(
            weaponOriginalTitle,
            rarityId,
            affinity,
            modifications.SpEffectTexts,
            true);

        if (uniqueWeapon)
        {
            string uniqueName = LoreGenerator.CreateRandomUniqueName(generatedType == WeaponTypes.Shields);
            if (!string.IsNullOrEmpty(uniqueName))
            {
                weaponFinalTitleColored = uniqueName.WrapTextWithProperties(color: this.Configuration.Settings.ItemLotGeneratorSettings.UniqueItemColor, size: 24);
            }
            else
            {
                Log.Logger.Error("GENERATED UNIQUENAME WAS EMPTY!");
            }
        }

        //weaponDictionary.SetValue("Name", "DSLR " + weaponFinalTitle);

        ExportLootGenParamAndTextToOutputs(newWeapon.GenericParam, LootType.Weapon, weaponFinalTitleColored, weaponDesc + Environment.NewLine + GetParamLootLore(weaponFinalTitle, false), "", [], []);

        return newWeapon.ID;
    }

    private void SetWeaponOriginParam(GenericParam weaponDictionary, int id, int upgradeCap = 25, bool replace = true)
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

    private void ApplyWeaponScalingRange(GenericParam weaponDictionary, int rarityId)
    {
        var currentScalings = Configuration.LootParam.WeaponsScaling.Select(d => new { ParamName = d, Value = weaponDictionary.GetValue<float>(d) }).ToDictionary(d => d.ParamName);
        var scalingRange = RarityHandler.GetRarityDamageAdditionRange(rarityId);

        foreach (string scaling in currentScalings.Keys)
        {
            var currentScaling = currentScalings[scaling];

            float newValue = (float)Math.Max(currentScaling.Value, Random.NextDouble(15.0, 25.0));

            weaponDictionary.SetValue(currentScaling.ParamName, newValue);
        }

        double primaryAddition = MathFunctions.RoundToXDecimalPlaces((float)Random.NextInt(scalingRange) * 0.6f, 2);

        var maxParam = currentScalings.MaxBy(d => d.Value.Value).Value;

        weaponDictionary.SetValue(maxParam.ParamName, maxParam.Value + primaryAddition);

        // randomly choose secondary stat and apply
        string randomScalingKey = Random.GetRandomItem(currentScalings.Keys.Except([maxParam.ParamName]).ToList());

        var randomScaling = currentScalings[randomScalingKey];

        weaponDictionary.SetValue(randomScaling.ParamName, Math.Clamp(Random.NextInt(scalingRange) * .5f, 0, 130));
    }

    private WeaponModifications ApplyShieldCutRateChanges(DamageTypeSetup dT1, DamageTypeSetup? dT2, GenericParam weaponDictionary, int rarityId)
    {
        WeaponModifications mods = new(dT1, dT2);

        float spEffectMultiplier = 1.00f;
        float existingValue = weaponDictionary.GetValue<float>(mods.PrimaryDamageType.ShieldParam);
        float? existingSecondaryValue = mods.SecondaryDamageType != null ? weaponDictionary.GetValue<float>(mods.SecondaryDamageType.ShieldParam) : null;

        var guardRateMultiplier = this.Random.Next(this.RarityHandler.GetRarityShieldGuardRateRange(rarityId));

        float value = Math.Clamp(existingValue * guardRateMultiplier, 0, 100);

        weaponDictionary.SetValue(mods.PrimaryDamageType.ShieldParam, value);

        if (existingSecondaryValue != null
            && mods.PrimaryDamageType.ShieldParam != mods.SecondaryDamageType.ShieldParam)
        {
            float secondaryValue = Math.Clamp((float)(existingSecondaryValue + this.Random.Next(new Range<float>(.1f, 5.0f))), 0, 100);
            weaponDictionary.SetValue(mods.SecondaryDamageType.ShieldParam, secondaryValue);
            mods.SecondaryDamageValue = secondaryValue;
            
        }
        else if (mods.PrimaryDamageType.SpEffect == mods.SecondaryDamageType?.SpEffect)
        {
            spEffectMultiplier += 0.2f;
        }

        mods.PrimaryDamageValue = value;

        IEnumerable<SpEffectText> nameParts = ApplySpEffects(rarityId, [0], weaponDictionary, spEffectMultiplier, true);

        mods.SpEffectTexts = nameParts.ToList();

        return mods;
    }

    private WeaponModifications ApplyStaffDamageChanges(GenericParam weaponDictionary, int rarityId)
    {
        //TODO : randomize sorcery/incantation scaling?
        WeaponModifications modifications = new(new DamageTypeSetup(), null);

        List<int> options = SpEffectHandler.GetPossibleWeaponSpeffectTypes(weaponDictionary);

        IEnumerable<SpEffectText> nameParts = ApplySpEffects(rarityId, options, weaponDictionary, 1.0f, true);

        modifications.SpEffectTexts = nameParts.ToList();

        return modifications;
    }

    private WeaponModifications ApplyNormalDamageChanges(DamageTypeSetup dT1, DamageTypeSetup? dT2, GenericParam weaponDictionary, int rarityId)
    {
        WeaponModifications mods = new(dT1, dT2);

        var dmgRange = RarityHandler.GetRarityDamageAdditionRange(rarityId);

        List<string> dmgParams = Configuration.LootParam.WeaponsDamageParam;

        long maxValue = 0;

        GenericParam originalValues = weaponDictionary.Clone() as GenericParam;

        // reset all damage
        foreach (string dmgParam in dmgParams)
        {
            weaponDictionary.SetValue(dmgParam, 0f);
            maxValue = Math.Max(originalValues.GetValue<long>(dmgParam), maxValue);
        }

        float overallMultiplier = mods.PrimaryDamageType.OverallMultiplier;
        if (mods.SecondaryDamageType != null)
        {
            overallMultiplier = Math.Min(mods.PrimaryDamageType.OverallMultiplier, mods.SecondaryDamageType.OverallMultiplier);
        }

        int primaryDamage = (int)(Random.NextInt(dmgRange) * overallMultiplier);

        int secondaryDamage = (int)(Random.NextInt(dmgRange) * overallMultiplier);

        if (mods.SecondaryDamageType != null)
        {
            weaponDictionary.SetValue(mods.SecondaryDamageType.Param, (maxValue / 2) + secondaryDamage);
        }

        if (mods.SecondaryDamageType == null ||
            mods.PrimaryDamageType.Param == mods.SecondaryDamageType?.Param)
        {
            weaponDictionary.SetValue(mods.PrimaryDamageType.Param, (long)1.2 * maxValue + primaryDamage);
        }
        else
        {
            weaponDictionary.SetValue(mods.PrimaryDamageType.Param, maxValue + primaryDamage);
        }

        mods.PrimaryDamageValue = primaryDamage;
        mods.SecondaryDamageValue = secondaryDamage;

        List<int> options = SpEffectHandler.GetPossibleWeaponSpeffectTypes(weaponDictionary);

        ApplySpEffects(rarityId, options, weaponDictionary);

        List<string> passiveParams = GetPassiveSpEffectSlotArrayFromOutputParamName();

        List<string> behSpEffectSlots = Configuration.LootParam.WeaponBehSpeffects;

        List<string> weaponEffectTextParams = Configuration.LootParam.SpeffectMsg;

        weaponDictionary.SetValue(passiveParams[0], mods.PrimaryDamageType.SpEffect);
        weaponDictionary.SetValue(behSpEffectSlots[0], mods.PrimaryDamageType.OnHitSpEffect);
        weaponDictionary.SetValue(weaponEffectTextParams[0], mods.PrimaryDamageType.Message);

        if (mods.SecondaryDamageType != null)
        {
            weaponDictionary.SetValue(passiveParams[1], mods.SecondaryDamageType.SpEffect);
            weaponDictionary.SetValue(behSpEffectSlots[1], mods.SecondaryDamageType.OnHitSpEffect);
            weaponDictionary.SetValue(weaponEffectTextParams[1], mods.SecondaryDamageType.Message);
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

        string hitVfxParam = Configuration.LootParam.WeaponsHitVfx;

        int hitVfx = mods.PrimaryDamageType.HitEffectCategory;
        if (mods.SecondaryDamageType != null && mods.SecondaryDamageType.HitEffectCategory > 0)
        {
            hitVfx = mods.SecondaryDamageType.HitEffectCategory;
        }

        weaponDictionary.SetValue(hitVfxParam, hitVfx);

        string critParam = Configuration.LootParam.WeaponsThrowDamageParam;
        float critMultiplier = mods.PrimaryDamageType.CriticalMultAddition + mods.SecondaryDamageType?.CriticalMultAddition ?? 0;

        int critValue = 0;

        if (critMultiplier > 0.0)
        {
            critValue = Random.NextInt(5, 20);
        }

        weaponDictionary.SetValue(critParam, (int)(critValue * (1 + critMultiplier)));

        string staminaParam = Configuration.LootParam.WeaponsStaminaRate;
        float maxDamage = Configuration.LootParam.WeaponsDamageParam.Max(par => weaponDictionary.GetValue<float>(par));

        if (weaponDictionary.ContainsKey(staminaParam) && maxDamage > 170)
        {
            weaponDictionary.SetValue(staminaParam, MathFunctions.RoundToXDecimalPlaces((float)(weaponDictionary.GetValue<float>(staminaParam) + Random.NextDouble(0.05, 0.3)), 3));
        }

        this.damageTypeHandler.ApplyWeaponVfxFromDamageTypes(weaponDictionary, mods);

        return mods;
    }

    private WeaponModifications ApplyWeaponModifications(GenericParam weaponDictionary, int rarityId, WeaponTypes weaponType)
    {
        // randomize damage type
        DamageTypeSetup primary = this.damageTypeHandler.ChooseDamageTypeAtRandom(Configuration.Settings.ItemLotGeneratorSettings.ChaosLootEnabled, false);
        DamageTypeSetup? secondary = null;

        if (Random.GetRandomBoolByPercent(weaponGeneratorConfig.SplitDamageTypeChance))
        {
            secondary = this.damageTypeHandler.ChooseDamageTypeAtRandom(Configuration.Settings.ItemLotGeneratorSettings.ChaosLootEnabled, true);
        }

        return weaponType switch
        {
            WeaponTypes.StaffsSeals => ApplyStaffDamageChanges(weaponDictionary, rarityId),
            WeaponTypes.Shields => ApplyShieldCutRateChanges(primary, secondary, weaponDictionary, rarityId),
            _ => ApplyNormalDamageChanges(primary, secondary, weaponDictionary, rarityId),
        };
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

