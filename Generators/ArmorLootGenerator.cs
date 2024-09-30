namespace DSLRNet.Generators;

using DSLRNet.Config;
using DSLRNet.Contracts;
using DSLRNet.Data;
using DSLRNet.Handlers;
using Microsoft.Extensions.Options;
using Mods.Common;
using System;
using System.Collections.Generic;

public class ArmorLootGenerator : ParamLootGenerator
{

    public List<string> ArmorCutRateParamNames { get; set; } = [];
    public List<string> ArmorCutRateParamRealNames { get; set; } = [];

    private const string CutRateDescString = "+{amt} Extra {type} Damage Cut Rate\\n";

    public ArmorLootGenerator(
        RarityHandler rarityHandler,
        WhiteListHandler whiteListHandler,
        SpEffectHandler spEffectHandler,
        DamageTypeHandler damageTypeHandler,
        LoreGenerator loreGenerator,
        RandomNumberGetter random,
        DataRepository dataRepository,
        IOptions<Configuration> configuration,
        CumulativeID cumulativeID) : base(rarityHandler, whiteListHandler, spEffectHandler, damageTypeHandler, loreGenerator, random, configuration, cumulativeID, dataRepository)
    {
        var armorLoots = CsvLoader.LoadCsv<EquipParamProtector>("DefaultData\\ER\\CSVs\\EquipParamProtector.csv");

        this.LoadedLoot = armorLoots.Select(GenericDictionary.FromObject).ToList();
    }

    public enum CutRates
    {
        Neutral,
        Slash,
        Blow,
        Thrust,
        Magic,
        Fire,
        Thunder,
        Holy
    }

    // MAIN ARMOR GENERATION FUNCTION
    public int CreateArmor(int rarityId, List<int> wllIds)
    {
        return -1;
        /*
        // TRY TO GRAB A WHITELISTED LOOT ARMOR DICTIONARY FOR OUR NEW ARMOR PIECE OR CHOOSE ONE AT RANDOM IF WE DON'T FIND IT
        var newArmor = GetLootDictionaryFromId(WhiteListHandler.ChooseLootFromWhitelistId(wllIds, true));

        // INITIALISE ARMOR DESCRIPTION
        string armorStatDesc = "";

        // APPLY NEW ID
        newArmor["ID"] = CumulativeID.GetNext();

        // SET ARMOR SELL PRICE
        SetLootSellValue(newArmor, rarityId);

        // SET ARMOR DROP RARITY EFFECT
        SetLootRarityParamValue(newArmor, rarityId);

        // CREATE AND APPLY RANDOM CUTRATE ADDITIONS FOR EACH AVAILABLE CUTRATE PARAM IF WE'RE MAKING RARITY AFFECT ARMOR CUTRATES
        if (RarityAffectsArmorAbsorption())
        {
            armorStatDesc += ApplyCutRateAdditionsFromRarity(rarityId, newArmor);
        }

        // APPLY RESISTANCE MULTIPLIERS
        ApplyArmorResistanceAdditions(newArmor, rarityId);

        // RANDOMIZE ARMOR'S WEIGHT
        RandomizeLootWeightBasedOnRarity(newArmor, rarityId);

        // STORE AND APPLY SPEFFECTS
        var speffs = ApplySpeffectsAndStoreFixesArray(rarityId, new List<int> { 0 }, newArmor, 1.0, true, -1, true);

        // CREATE NEW ARMOR TITLE
        string originalName = newArmor["Name"].ToString();
        string finalTitle = CreateLootTitle(originalName.Replace(" (Altered)", " (Alt)"), rarityId, "", speffs["fixes"]["Suffix"], speffs["fixes"]["Prefix"], speffs["fixes"]["Interfix"], false);

        // APPLY NEW ARMOR TITLE
        newArmor["Name"] = finalTitle;

        // OUTPUT TEXT AND LOOTGEN
        // -EXPORT TITLE MULTIPLE TIMES, MAYBE THIS WILL FIX ARMOR TITLES NOT GOING THROUGH?
        ExportLootgenParamAndTextToOutputs(newArmor, finalTitle, CreateArmorDescription(speffs["description"], armorStatDesc + GetParamLootLore(finalTitle, true)), "", [], ["-1", "0"], true);

        // RETURN NEW ARMOR'S ID FOR INSTANT ADDITION TO ITEMLOT
        return Convert.ToInt32(newArmor["ID"]);
        */
    }
    /*
    public string ApplyCutRateAdditionsFromRarity(int rarityId, Dictionary<string, object> outputDictionary)
    {
        // CREATE STRING TO RETURN A PRECOMPILED DESCRIPTION FOR EASY ADDITION
        string descriptionString = "";

        // GET ORIGINAL CUTRATE VALUES FOR ARMOR
        var originalCutRate = new List<double>();

        // GET THE PARAMS WE'LL BE WORKING WITH
        var cutRateParams = GetLootParamDictionaryFromGameType()["armor_param"];
        var defenseParams = GetLootParamDictionaryFromGameType()["armor_defenseparams"];

        // WE NEED TO HANDLE THINGS DIFFERENTLY DEPENDING ON IF WE'RE WORKING WITH ELDEN RING OR AN EARLIER GAME LIKE DS3, AS ER EXCLUSIVELY USES ABSORPTION
        if (GameIsEldenRing())
        {
            // ITERATE OVER ALL PARAMS HERE, IF THE DICTIONARY HAS THAT PARAM, SUBTRACT A RARITY-DEFINED EXTRA CUTRATE FROM THE ORIGINAL VALUE
            if (cutRateParams.Count > 0)
            {
                foreach (var param in cutRateParams)
                {
                    if (outputDictionary.ContainsKey(param))
                    {
                        double oldValue = Convert.ToSingle(outputDictionary[param]);
                        // SUBTRACT A RARITY DEFINED CUTRATE VALUE FROM THE OLD VALUE AND SET IT TO THE NEW ONE
                        outputDictionary[param] = oldValue - RarityHandler.GetRarityArmorCutRateAddition(rarityId);
                    }
                }
            }

            // INITIALISE ALL DEFENCE PARAMS JUST TO BE SURE
            if (defenseParams.Count > 0)
            {
                foreach (var param in defenseParams)
                {
                    if (outputDictionary.ContainsKey(param))
                    {
                        outputDictionary[param] = 0;
                    }
                }
            }
        }

        return descriptionString;
    }

    public void ApplyArmorResistanceAdditions(Dictionary<string, object> armorDict, int rarity)
    {
        // GET OUR RESISTANCE ADDITIONS
        var resistances = GetLootParamDictionaryFromGameType()["armor_resistparams"];

        // IF WE FOUND ANY PARAMS, MULTIPLY THEM BY A VALUE BETWEEN A RARITY DEFINED MIN AND MAX MULTIPLIER
        if (resistances.Count > 0)
        {
            foreach (var param in resistances)
            {
                if (armorDict.ContainsKey(param))
                {
                    int oldValue = Convert.ToInt32(armorDict[param]);
                    armorDict[param] = oldValue * RarityHandler.GetRarityArmorResistMultMultiplier(rarity);
                }
            }
        }
    }

    public string CreateArmorDescription(string speffects = "", string extraProtection = "")
    {
        // FIRST CREATE REQUIRED NEWLINES BASED ON WHETHER OR NOT ARGUMENTS ARE EMPTY
        var newLines = new List<string>();
        var additions = new List<string> { speffects, extraProtection };

        // ITERATE OVER ALL ADDITIONS AND ADD A NEWLINE TO NEWLINES ARRAY IF THEY'RE NOT EMPTY
        foreach (var addition in additions)
        {
            newLines.Add(string.IsNullOrEmpty(addition) ? "" : Environment.NewLine);
        }

        return speffects + newLines[0] + extraProtection + newLines[1];
    }

    public void LoadArmorTemplates()
    {
        // GET ALL ARMOR CSV FILES
        var armorTemplateToLoad = ModManager.GetAllFilesInModFolderFromAllMods(ModManager.MF.P_Armor, GetAllFilesBannedArray);

        // LOAD THESE TEMPLATES INTO LOOTTEMPLATELIBRARY IF WE FOUND ANYTHING
        if (armorTemplateToLoad.Count > 0 && armorTemplateToLoad["result"].Count > 0)
        {
            LoadMultipleParamCsvsGettingHeaderFromFirst(armorTemplateToLoad["result"], LootTemplateLibrary, armorTemplateToLoad["notdefault"]);
            // GET ANY PRIORITISED ARMOR SETS FROM LTL ["notdefault"]
            GetPriorityIdsFromDictionary(LootTemplateLibrary);
            EmitSignal("loottemplatelibrary_loading_complete");
        }
    }

    // ARMOR INFORMATION FUNCTIONS

    public void UpdateCutRateParamNames()
    {
        ArmorCutRateParamNames = GetCutRateParamNameArray();
        ArmorCutRateParamRealNames = GetCutRateParamRealNameArray();
    }

    public List<string> GetCutRateParamNameArray()
    {
        return this.configuration.LootParam.ArmorParam;
    }

    public List<string> GetCutRateParamRealNameArray()
    {
        return GetGameTypeDictionary()["LootParam"]["armor_realname"];
    }

    public string GetDescriptionCutRateStringWithFormatting(string amount = "0", string dmgType = "")
    {
        return CutRateDescString.Replace("{amt}", amount).Replace("{type}", dmgType);
    }

    // SETUP FUNCTIONS

    public void SetupMandatoryKeysArmor()
    {
        var finalArray = new List<string>();
        var gtDict = GetGameTypeDictionary();

        // MAKE ALL CUTRATES MANDATORY
        finalArray.AddRange(gtDict["LootParam"]["armor_param"]);
        // MAKE ALL DEFENCE PARAMS MANDATORY TO STOP THEM MAKING ARMOR OP - DEFAULT INSERTED IS 100, NOT 0
        finalArray.AddRange(gtDict["LootParam"]["armor_defenseparams"]);
        // MAKE ALL RESISTANCE PARAMS MANDATORY
        finalArray.AddRange(gtDict["LootParam"]["armor_resistparams"]);
        // INCLUDE ANY OTHER MANDATORY ARMOR FIELDS
        ParamMandatoryKeys.AddRange(finalArray);
    }

    private Dictionary<string, List<string>> GetLootParamDictionaryFromGameType()
    {
        // Placeholder for the actual implementation of get_loot_param_dictionary_from_gametype()
        return new Dictionary<string, List<string>>();
    }

    private void SetLootSellValue(Dictionary<string, object> loot, int rarityId)
    {
        // Placeholder for the actual implementation of set_loot_sell_value()
    }

    private void SetLootRarityParamValue(Dictionary<string, object> loot, int rarityId)
    {
        // Placeholder for the actual implementation of set_loot_rarity_param_value()
    }

    private bool RarityAffectsArmorAbsorption()
    {
        // Placeholder for the actual implementation of rarity_affects_armor_absorption()
        return false;
    }

    private string ApplyCutRateAdditionsFromRarity(int rarityId, Dictionary<string, object> armor)
    {
        // Placeholder for the actual implementation of apply_cutrate_additions_from_rarity()
        return "";
    }

    private void ApplyArmorResistanceAdditions(Dictionary<string, object> armor, int rarityId)
    {
        // Placeholder for the actual implementation of apply_armor_resistance_additions()
    }

    private void RandomizeLootWeightBasedOnRarity(Dictionary<string, object> loot, int rarityId)
    {
        // Placeholder for the actual implementation of randomize_loot_weight_based_on_rarity()
    }

    private Dictionary<string, Dictionary<string, string>> ApplySpeffectsAndStoreFixesArray(int rarityId, List<int> speffIds, Dictionary<string, object> loot, double multiplier, bool apply, int speffType, bool store)
    {
        // Placeholder for the actual implementation of apply_speffects_and_store_fixes_array()
        return new Dictionary<string, Dictionary<string, string>>();
    }

    private string CreateLootTitle(string originalName, int rarityId, string prefix, string suffix, string interfix, bool isUnique)
    {
        // Placeholder for the actual implementation of create_loot_title()
        return "";
    }

    private void ExportLootgenParamAndTextToOutputs(Dictionary<string, object> loot, string title, string description, string lore, List<string> tags, List<string> flags, bool export)
    {
        // Placeholder for the actual implementation of export_lootgen_param_and_text_to_outputs()
    }

    private string CreateArmorDescription(string speffDescription, string armorStatDesc)
    {
        // Placeholder for the actual implementation of create_armor_description()
        return "";
    }

    private string GetParamLootLore(string title, bool include)
    {
        // Placeholder for the actual implementation of get_paramloot_lore()
        return "";
    }
    */
}