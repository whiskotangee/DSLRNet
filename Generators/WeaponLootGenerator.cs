namespace DSLRNet.Generators;

using DSLRNet.Config;
using DSLRNet.Contracts;
using DSLRNet.Data;
using DSLRNet.Handlers;
using Microsoft.Extensions.Options;
using Mods.Common;
using Serilog;
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
        WhiteListHandler whitelistHandler,
        SpEffectHandler spEffectHandler,
        RandomNumberGetter random,
        LoreGenerator loreGenerator,
        DamageTypeHandler damageTypeHandler,
        DataRepository dataRepository,
        CumulativeID cumulativeID) : base(rarityHandler, whitelistHandler, spEffectHandler, damageTypeHandler, loreGenerator, random, configuration, cumulativeID, dataRepository)
    {
        this.weaponGeneratorConfig = weaponGeneratorConfig.Value;
        this.ashofWarHandler = ashofWarHandler;

        var weaponLoots = CsvLoader.LoadCsv<EquipWeaponParam>("DefaultData\\ER\\CSVs\\EquipWeaponParam.csv");

        this.LoadedLoot = weaponLoots.Select(GenericDictionary.FromObject).ToList();
    }

    public int CreateWeapon(int rarityId, List<int> whitelistLootIds = null)
    {
        whitelistLootIds ??= [];

        if (whitelistLootIds.Count == 0)
        {
            whitelistLootIds.Add(100);
        }

        bool uniqueWeapon = this.Random.GetRandomBoolByPercent(this.weaponGeneratorConfig.UniqueNameChance);
        double uniqueValueMultiplier = uniqueWeapon ? this.weaponGeneratorConfig.UniqueWeaponMultiplier : 1.0;
        string weaponDesc = "";

        WeaponTypes goalWeaponType = this.Random.NextWeightedValue(this.weaponGeneratorConfig.Types, this.weaponGeneratorConfig.Weights, 1.0f);

        GenericDictionary weaponDictionary = GetLootDictionaryFromId(this.WhiteListHandler.GetLootByWhiteList(whitelistLootIds, LootType.Weapon));

        string affinity = "";

        WeaponTypes generatedType = this.GetWeaponType(weaponDictionary.GetValue<int>(this.Configuration.LootParam.WeaponsWepMotionCategory));

        bool isStaffSeal = generatedType == WeaponTypes.StaffsSeals;
        bool isShield = generatedType == WeaponTypes.Shields;

        // randomize damage type
        var DT1 = this.DamageTypeHandler.ChooseDamageTypeAtRandom(this.Configuration.Settings.ChaosLootEnabled, false);
        var DT2 = this.DamageTypeHandler.ChooseDamageTypeAtRandom(this.Configuration.Settings.ChaosLootEnabled, true);

        if (!this.Random.GetRandomBoolByPercent(this.weaponGeneratorConfig.SplitDamageTypeChance))
        {
            DT2 = DT1;
        }

        var DTAdditions = DamageTypeAddition.CreateEmpty();

        ApplyWeaponDamageAdditions(
            DT1,
            DT2,
            DTAdditions,
            weaponDictionary,
            rarityId,
            generatedType);

        weaponDesc += DTAdditions.SpEffectDescription;

        if (!isShield && !isStaffSeal)
        {
            affinity = CreateAffinityTitle(DT1, DT2);
        }

        if (weaponDictionary.GetValue<int>(this.Configuration.LootParam.WeaponReinforceTypeId) != 2200 && generatedType != WeaponTypes.StaffsSeals)
        {
            this.ashofWarHandler.AssignAshOfWar(weaponDictionary);
        }

        SetLootSellValue(weaponDictionary, rarityId, (double)uniqueValueMultiplier);
        SetLootRarityParamValue(weaponDictionary, rarityId);
        RandomizeLootWeightBasedOnRarity(weaponDictionary, rarityId);
        ApplyNextId(weaponDictionary);

        // 42300 LETS WEAPONS TAKE DAMAGE SCALING FROM ALL POSSIBLE SOURCES (STR,DEX,INT,FTH) 
        weaponDictionary.SetValue("attackElementCorrectId", 42300);
        /* SETUP A FEW WEPMOTIONCATEGORIES THAT ABSOLUTELY SHOULD NOT BE GIVEN AOWS LIKE STAFFS/SEALS
		    const bannedfromaows : Array = [31,41]
        */
        weaponDictionary.SetValue("gemMountType", generatedType == WeaponTypes.StaffsSeals ? 0 : 2);
        // PREVENT CHANGING AFFINITY
        weaponDictionary.SetValue(this.Configuration.LootParam.NoAffinityChange, 1);

        ApplyWeaponScalingRange(weaponDictionary, rarityId);
        SetWeaponOriginParam(weaponDictionary, weaponDictionary.GetValue<int>("ID"), replace: true);

        string weaponOriginalTitle = weaponDictionary.GetValue<string>("Name");
        string weaponRarity = this.RarityHandler.GetRarityName(rarityId);

        string weaponFinalTitle = CreateLootTitle(
            weaponOriginalTitle,
            rarityId,
            affinity,
            DTAdditions.SpEffectTexts.FirstOrDefault());

        string weaponFinalTitleColored = CreateLootTitle(
            weaponOriginalTitle,
            rarityId,
            affinity,
            DTAdditions.SpEffectTexts.FirstOrDefault(),
            true);

        if (uniqueWeapon)
        {
            string uniqueName = this.LoreGenerator.CreateRandomUniqueName("", isShield);
            if (!string.IsNullOrEmpty(uniqueName))
            {
                weaponFinalTitleColored = $"{uniqueName} ({weaponRarity})";
            }
            else
            {
                Log.Logger.Error("GENERATED UNIQUENAME WAS EMPTY!");
            }
        }

        string wftDescription = uniqueWeapon ? $"({weaponFinalTitle})\\n" : "";
        weaponDictionary.SetValue("Name", "DSLR " + weaponFinalTitle);

        ExportLootGenParamAndTextToOutputs(weaponDictionary, LootType.Weapon, weaponFinalTitleColored, wftDescription + weaponDesc + GetParamLootLore(weaponFinalTitle, false), "", new List<string>(), new List<string> { "-1", "0" });

        return Convert.ToInt32(weaponDictionary.GetValue<int>("ID"));
    }

    private void SetWeaponOriginParam(GenericDictionary weaponDictionary, int id, int upgradeCap = 25, bool replace = true)
    {
        var originParams =
            Enumerable.Range(1, upgradeCap)
            .Where(d => weaponDictionary.ContainsKey($"{this.Configuration.LootParam.WeaponOriginParamBase}{d}") &&
                        weaponDictionary.GetValue<int>($"{this.Configuration.LootParam.WeaponOriginParamBase}{d}") > 0)
            .Select(d => $"{this.Configuration.LootParam.WeaponOriginParamBase}{d}").ToList();

        weaponDictionary.SetValue(this.Configuration.LootParam.WeaponOriginParamBase, id);

        if (replace)
        {
            foreach (var param in originParams)
            {
                if (weaponDictionary.GetValue<int>(param) > 0)
                {
                    weaponDictionary.SetValue(param, id);
                }
            }
        }
        else
        {
            foreach (var param in originParams)
            {
                weaponDictionary.SetValue(param, id);
            }
        }
    }

    private void ApplyWeaponScalingRange(GenericDictionary weaponDictionary, int rarityId)
    {
        var currentScalings = this.Configuration.LootParam.WeaponsScaling.Select(d => new { ParamName = d, Value = weaponDictionary.GetValue<double>(d) }).ToDictionary(d => d.ParamName);
        List<int> scalingRange = this.RarityHandler.GetRarityDamageAdditionRange(rarityId);

        foreach (var scaling in currentScalings.Keys)
        {
            var currentScaling = currentScalings[scaling];

            double newValue = Math.Max(currentScaling.Value, this.Random.NextDouble(15.0, 25.0));

            weaponDictionary.SetValue(currentScaling.ParamName, newValue);
        }

        var primaryAddition = MathFunctions.RoundToXDecimalPlaces((double)this.Random.NextDouble(scalingRange[0], scalingRange[1]) * 0.6f, 2);

        var maxParam = currentScalings.MaxBy(d => d.Value.Value).Value;

        weaponDictionary.SetValue(maxParam.ParamName, maxParam.Value + primaryAddition);

        // randomly choose secondary stat and apply
        var randomScalingKey = this.Random.GetRandomItem(currentScalings.Keys.Except([maxParam.ParamName]).ToList());

        var randomScaling = currentScalings[randomScalingKey];

        weaponDictionary.SetValue(randomScaling.ParamName, Math.Clamp(this.Random.NextDouble(scalingRange[0], scalingRange[1]) * .5f, 0, 130));
    }

    private void ApplyShieldCutRateChanges(DamageType dT1, DamageType dT2, DamageTypeAddition dTAdditions, GenericDictionary weaponDictionary, int rarityId)
    {
        var spEffectMultiplier = 1.0f;
        int existingValue = weaponDictionary.GetValue<int>(dT1.ShieldParam);
        
        if (dT1.SpEffect == existingValue)
        {
            spEffectMultiplier += 0.2f / 2;
        }
        if (dT1 == dT2)
        {
            spEffectMultiplier += 0.2f;
        }

        dTAdditions.PrimaryDamageType.Params.SetValue(dT1.ShieldParam, Math.Clamp(existingValue * spEffectMultiplier, 0, 100));
        dTAdditions.SecondaryDamageType.Params.SetValue(dT2.ShieldParam, Math.Clamp(existingValue * spEffectMultiplier, 0, 100));

        var nameParts = ApplySpEffects(rarityId, [0], weaponDictionary, spEffectMultiplier, true);

        dTAdditions.SpEffectTexts = nameParts.ToList();
    }

    private void ApplyStaffDamageChanges(DamageType dT1, DamageType dT2, DamageTypeAddition dTAdditions, GenericDictionary weaponDictionary, int rarityId)
    {
        var options = this.SpEffectHandler.GetPossibleWeaponSpeffectTypes(weaponDictionary);

        var nameParts = ApplySpEffects(rarityId, options, weaponDictionary, 1.0f, true);

        dTAdditions.SpEffectTexts = nameParts.ToList();
    }

    private void ApplyNormalDamageChanges(DamageType dT1, DamageType dT2, DamageTypeAddition dTAdditions, GenericDictionary weaponDictionary, int rarityId)
    {
        List<int> dmgRange = this.RarityHandler.GetRarityDamageAdditionRange(rarityId);

        var dmgParams = this.Configuration.LootParam.WeaponsDamageParam;
        //var params : Array = get_gametype_dictionary()["LootParam"]["weapons_damageparam"] if !isshield else get_gametype_dictionary()["LootParam"]["weapons_guardrate_param"]

        Dictionary<string, int> originalValues = [];
        // reset all damage
        foreach (var dmgParam in dmgParams)
        {
            originalValues.Add(dmgParam, weaponDictionary.GetValue<int>(dmgParam));
            weaponDictionary.SetValue(dmgParam, 0);
        }

        var overallMultiplier = Math.Min(dT1.OverallMultiplier, dT2.OverallMultiplier);

        var primaryDamage = (int)(this.Random.NextInt(dmgRange[0], dmgRange[1]) * overallMultiplier);

        var secondaryDamage = (int)(this.Random.NextInt(dmgRange[0], dmgRange[1]) * overallMultiplier);

        weaponDictionary.SetValue(dT1.Param, originalValues[dT1.Param] + primaryDamage);
        weaponDictionary.SetValue(dT2.Param, secondaryDamage);

        dTAdditions.PrimaryDamageType.Value = primaryDamage;
        dTAdditions.SecondaryDamageType.Value = secondaryDamage;

        var options = this.SpEffectHandler.GetPossibleWeaponSpeffectTypes(weaponDictionary);

        var texts = this.ApplySpEffects(rarityId, options, weaponDictionary);

        var passiveParams = this.GetPassiveSpEffectSlotArrayFromOutputParamName();

        var behSpEffectSlots = this.Configuration.LootParam.WeaponBehSpeffects;

        var weaponEffectTextParams = this.Configuration.LootParam.SpeffectMsg;

        weaponDictionary.SetValue(passiveParams[0], dT1.SpEffect);
        weaponDictionary.SetValue(behSpEffectSlots[0], dT1.OnHitSpEffect);
        weaponDictionary.SetValue(weaponEffectTextParams[0], dT1.Message);

        if (dT1 != dT2)
        {
            weaponDictionary.SetValue(passiveParams[1], dT2.SpEffect);
            weaponDictionary.SetValue(behSpEffectSlots[1], dT2.OnHitSpEffect);
            weaponDictionary.SetValue(weaponEffectTextParams[1], dT2.Message);
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

        dTAdditions.SpEffectDescription = "Effect: " + string.Join("\\n", effectStrings);


        var hitVfxParam = this.Configuration.LootParam.WeaponsHitVfx;
        var hitVfx = dT1.HitEffectCategory != 0 ? dT1.HitEffectCategory : dT2.HitEffectCategory;

        weaponDictionary.SetValue(hitVfxParam, hitVfx);

        var critParam = this.Configuration.LootParam.WeaponsThrowDamageParam;
        var critMultiplier = dT1.CriticalMultAddition + dT2.CriticalMultAddition;

        var critValue = 0;

        if (critMultiplier > 0.0)
        {
            critValue = this.Random.NextInt(5, 20);
        }

        weaponDictionary.SetValue(critParam, critValue * (1 + critMultiplier));

        var staminaParam = this.Configuration.LootParam.WeaponsStaminaRate;
        var maxDamage = this.Configuration.LootParam.WeaponsDamageParam.Max(par => weaponDictionary.GetValue<int>(par));

        if (weaponDictionary.ContainsKey(staminaParam) && maxDamage > 170)
        {
            weaponDictionary.SetValue(staminaParam, MathFunctions.RoundToXDecimalPlaces((double)(weaponDictionary.GetValue<double>(staminaParam) + this.Random.NextDouble(0.05, 0.3)), 3));
        }

        this.DamageTypeHandler.ApplyWeaponVfxFromDamageTypes(weaponDictionary, dT1, dT2);
    }

    private void ApplyWeaponDamageAdditions(DamageType dT1, DamageType dT2, DamageTypeAddition dTAdditions, GenericDictionary weaponDictionary, int rarityId, WeaponTypes weaponType)
    {
        switch(weaponType)
        {
            case WeaponTypes.Normal:
                this.ApplyNormalDamageChanges(dT1, dT2, dTAdditions, weaponDictionary, rarityId);
                break;
            case WeaponTypes.StaffsSeals:
                this.ApplyStaffDamageChanges(dT1, dT2, dTAdditions, weaponDictionary, rarityId);
                break;
            case WeaponTypes.Shields:
                this.ApplyShieldCutRateChanges(dT1, dT2, dTAdditions, weaponDictionary, rarityId);
                break;
        }
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

