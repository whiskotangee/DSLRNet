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
        this.CumulativeID = new CumulativeID();
        this.weaponGeneratorConfig = weaponGeneratorConfig.Value;
        this.ashofWarHandler = ashofWarHandler;
        this.damageTypeHandler = damageTypeHandler;

        this.DataSource = weaponDataSource;
    }

    public int CreateWeapon(int rarityId, List<int> whitelistLootIds = null)
    {
        whitelistLootIds ??= [];

        if (whitelistLootIds.Count == 0)
        {
            whitelistLootIds.Add(100);
        }

        bool uniqueWeapon = this.Random.GetRandomBoolByPercent(this.weaponGeneratorConfig.UniqueNameChance);
        float uniqueValueMultiplier = uniqueWeapon ? this.weaponGeneratorConfig.UniqueWeaponMultiplier : 1.0f;

        WeaponTypes goalWeaponType = this.Random.NextWeightedValue(this.weaponGeneratorConfig.Types, this.weaponGeneratorConfig.Weights);

        EquipParamWeapon newWeapon = this.GetNewLootItem(this.WhiteListHandler.GetLootByAllowList(whitelistLootIds, LootType.Weapon));

        WeaponTypes generatedType = this.GetWeaponType(newWeapon.wepmotionCategory);

        newWeapon.ID = this.CumulativeID.GetNext();
        newWeapon.sellValue = this.RarityHandler.GetRaritySellValue(rarityId);
        newWeapon.rarity = this.RarityHandler.GetRarityParamValue(rarityId);
        newWeapon.iconId = this.RarityHandler.GetIconIdForRarity(newWeapon.iconId, rarityId, isUnique: uniqueWeapon);

        // 42300 allows scaling from all sources (STR,DEX,INT,FTH)
        if (generatedType != WeaponTypes.StaffsSeals)
        {
            newWeapon.attackElementCorrectId = 42300;
        }

        newWeapon.gemMountType = generatedType == WeaponTypes.StaffsSeals ? 0 : 2;
        newWeapon.disableGemAttr = 1;
        newWeapon.weight = this.RarityHandler.GetRandomizedWeightForRarity(rarityId);

        string affinity = "";

        WeaponModifications modifications = this.ApplyWeaponModifications(
            newWeapon.GenericParam,
            rarityId,
            generatedType);

        this.damageTypeHandler.ApplyDamageTypeWeaponSpEffects(modifications, newWeapon.GenericParam);

        string weaponDesc = string.Join(Environment.NewLine, modifications.SpEffectDescriptions);

        if (generatedType == WeaponTypes.Normal)
        {
            affinity = this.CreateAffinityTitle(modifications);
        }

        if (newWeapon.reinforceTypeId != 2200 && generatedType != WeaponTypes.StaffsSeals)
        {
            this.ashofWarHandler.AssignAshOfWar(newWeapon);
        }

        this.ApplyWeaponScalingRange(newWeapon.GenericParam, rarityId);
        this.SetWeaponOriginParam(newWeapon.GenericParam, newWeapon.ID, replace: true);

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

        if (uniqueWeapon)
        {
            string uniqueName = this.LoreGenerator.CreateRandomUniqueName(generatedType == WeaponTypes.Shields);
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

        this.ExportLootDetails(newWeapon.GenericParam, LootType.Weapon, weaponFinalTitleColored, weaponDesc + Environment.NewLine + this.LoreGenerator.GenerateDescription(weaponFinalTitle, false), "", [], []);

        return newWeapon.ID;
    }

    private void SetWeaponOriginParam(GenericParam weaponDictionary, int id, int upgradeCap = 25, bool replace = true)
    {
        List<string> originParams =
            Enumerable.Range(1, upgradeCap)
            .Where(d => weaponDictionary.ContainsKey($"{this.Configuration.LootParam.WeaponOriginParamBase}{d}") &&
                        weaponDictionary.GetValue<int>($"{this.Configuration.LootParam.WeaponOriginParamBase}{d}") > 0)
            .Select(d => $"{this.Configuration.LootParam.WeaponOriginParamBase}{d}").ToList();

        weaponDictionary.SetValue(this.Configuration.LootParam.WeaponOriginParamBase, id);

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
        var currentScalings = this.Configuration.LootParam.WeaponsScaling.Select(d => new { ParamName = d, Value = weaponDictionary.GetValue<float>(d) }).ToDictionary(d => d.ParamName);
        Range<int> scalingRange = this.RarityHandler.GetRarityDamageAdditionRange(rarityId);

        foreach (string scaling in currentScalings.Keys)
        {
            var currentScaling = currentScalings[scaling];

            // TODO: more configuration about number of scaling values per rarity Id?
            float newValue = (float)Math.Max(currentScaling.Value, this.Random.NextDouble(15.0, 25.0));

            weaponDictionary.SetValue(currentScaling.ParamName, newValue);
        }

        double primaryAddition = MathFunctions.RoundToXDecimalPlaces(this.Random.NextInt(scalingRange) * 0.6f, 2);

        var maxParam = currentScalings.MaxBy(d => d.Value.Value).Value;

        weaponDictionary.SetValue(maxParam.ParamName, maxParam.Value + primaryAddition);

        // randomly choose secondary stat and apply
        string randomScalingKey = this.Random.GetRandomItem(currentScalings.Keys.Except([maxParam.ParamName]).ToList());

        var randomScaling = currentScalings[randomScalingKey];

        weaponDictionary.SetValue(randomScaling.ParamName, Math.Clamp(this.Random.NextInt(scalingRange) * .5f, 0, 130));
    }

    private WeaponModifications ApplyShieldCutRateChanges(DamageTypeSetup dT1, DamageTypeSetup? dT2, GenericParam weaponDictionary, int rarityId)
    {
        WeaponModifications mods = new(dT1, dT2);

        float spEffectMultiplier = 1.00f;
        float existingValue = weaponDictionary.GetValue<float>(mods.PrimaryDamageType.ShieldParam);
        float? existingSecondaryValue = mods.SecondaryDamageType != null ? weaponDictionary.GetValue<float>(mods.SecondaryDamageType.ShieldParam) : null;

        float guardRateMultiplier = this.Random.Next(this.RarityHandler.GetRarityShieldGuardRateRange(rarityId));

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

        IEnumerable<SpEffectText> nameParts = this.ApplySpEffects(rarityId, [0], weaponDictionary, spEffectMultiplier, true);

        mods.SpEffectTexts = nameParts.ToList();

        return mods;
    }

    private WeaponModifications ApplyStaffDamageChanges(GenericParam weaponDictionary, int rarityId)
    {
        //TODO : randomize sorcery/incantation scaling?
        WeaponModifications modifications = new(new DamageTypeSetup(), null);

        List<int> options = this.SpEffectHandler.GetPossibleWeaponSpeffectTypes(weaponDictionary);

        IEnumerable<SpEffectText> nameParts = this.ApplySpEffects(rarityId, options, weaponDictionary, 1.0f, true);

        modifications.SpEffectTexts = nameParts.ToList();

        return modifications;
    }

    private WeaponModifications ApplyNormalDamageChanges(DamageTypeSetup dT1, DamageTypeSetup? dT2, GenericParam weaponDictionary, int rarityId)
    {
        WeaponModifications mods = new(dT1, dT2);

        Range<int> dmgRange = this.RarityHandler.GetRarityDamageAdditionRange(rarityId);

        List<string> dmgParams = this.Configuration.LootParam.WeaponsDamageParam;

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

        int primaryDamage = (int)(this.Random.NextInt(dmgRange) * overallMultiplier);

        int secondaryDamage = (int)(this.Random.NextInt(dmgRange) * overallMultiplier);

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

        List<int> options = this.SpEffectHandler.GetPossibleWeaponSpeffectTypes(weaponDictionary);

        this.ApplySpEffects(rarityId, options, weaponDictionary);

        List<string> passiveParams = this.GetPassiveSpEffectSlotArrayFromOutputParamName();

        List<string> behSpEffectSlots = this.Configuration.LootParam.WeaponBehSpeffects;

        List<string> weaponEffectTextParams = this.Configuration.LootParam.SpeffectMsg;

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

        string hitVfxParam = this.Configuration.LootParam.WeaponsHitVfx;

        int hitVfx = mods.PrimaryDamageType.HitEffectCategory;
        if (mods.SecondaryDamageType != null && mods.SecondaryDamageType.HitEffectCategory > 0)
        {
            hitVfx = mods.SecondaryDamageType.HitEffectCategory;
        }

        weaponDictionary.SetValue(hitVfxParam, hitVfx);

        string critParam = this.Configuration.LootParam.WeaponsThrowDamageParam;
        float critMultiplier = mods.PrimaryDamageType.CriticalMultAddition + mods.SecondaryDamageType?.CriticalMultAddition ?? 0;

        int critValue = 0;

        if (critMultiplier > 0.0)
        {
            critValue = this.Random.NextInt(5, 20);
        }

        weaponDictionary.SetValue(critParam, (int)(critValue * (1 + critMultiplier)));

        string staminaParam = this.Configuration.LootParam.WeaponsStaminaRate;
        float maxDamage = this.Configuration.LootParam.WeaponsDamageParam.Max(par => weaponDictionary.GetValue<float>(par));

        if (weaponDictionary.ContainsKey(staminaParam) && maxDamage > 170)
        {
            weaponDictionary.SetValue(staminaParam, MathFunctions.RoundToXDecimalPlaces((float)(weaponDictionary.GetValue<float>(staminaParam) + this.Random.NextDouble(0.05, 0.3)), 3));
        }

        this.damageTypeHandler.ApplyWeaponVfxFromDamageTypes(weaponDictionary, mods);

        return mods;
    }

    private WeaponModifications ApplyWeaponModifications(GenericParam weaponDictionary, int rarityId, WeaponTypes weaponType)
    {
        // randomize damage type
        DamageTypeSetup primary = this.damageTypeHandler.ChooseDamageTypeAtRandom(this.Configuration.Settings.ItemLotGeneratorSettings.ChaosLootEnabled, false);
        DamageTypeSetup? secondary = null;

        if (this.Random.GetRandomBoolByPercent(this.weaponGeneratorConfig.SplitDamageTypeChance))
        {
            secondary = this.damageTypeHandler.ChooseDamageTypeAtRandom(this.Configuration.Settings.ItemLotGeneratorSettings.ChaosLootEnabled, true);
        }

        return weaponType switch
        {
            WeaponTypes.StaffsSeals => this.ApplyStaffDamageChanges(weaponDictionary, rarityId),
            WeaponTypes.Shields => this.ApplyShieldCutRateChanges(primary, secondary, weaponDictionary, rarityId),
            _ => this.ApplyNormalDamageChanges(primary, secondary, weaponDictionary, rarityId),
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

