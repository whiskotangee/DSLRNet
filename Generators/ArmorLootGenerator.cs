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

    private const string CutRateDescString = "+{amt} Extra {type} Damage Cut Rate";

    public ArmorLootGenerator(
        RarityHandler rarityHandler,
        AllowListHandler whiteListHandler,
        SpEffectHandler spEffectHandler,
        DamageTypeHandler damageTypeHandler,
        LoreGenerator loreGenerator,
        RandomNumberGetter random,
        DataRepository dataRepository,
        IOptions<Configuration> configuration) : base(rarityHandler, whiteListHandler, spEffectHandler, damageTypeHandler, loreGenerator, random, configuration, dataRepository)
    {
        this.CumulativeID = new CumulativeID();

        List<EquipParamProtector> armorLoots = Csv.LoadCsv<EquipParamProtector>("DefaultData\\ER\\CSVs\\EquipParamProtector.csv");

        this.LoadedLoot = armorLoots.Select(GenericDictionary.FromObject).ToList();

        OutputParamName = "EquipParamProtector";
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
        // TRY TO GRAB A WHITELISTED LOOT ARMOR DICTIONARY FOR OUR NEW ARMOR PIECE OR CHOOSE ONE AT RANDOM IF WE DON'T FIND IT

        GenericDictionary newArmor = GetLootDictionaryFromId(this.WhiteListHandler.GetLootByWhiteList(wllIds, LootType.Armor));

        // INITIALISE ARMOR DESCRIPTION
        string armorStatDesc = "";

        // APPLY NEW ID
        newArmor.SetValue<int>("ID", CumulativeID.GetNext());

        // SET ARMOR SELL PRICE
        SetLootSellValue(newArmor, rarityId);

        // SET ARMOR DROP RARITY EFFECT
        SetLootRarityParamValue(newArmor, rarityId);

        // CREATE AND APPLY RANDOM CUTRATE ADDITIONS FOR EACH AVAILABLE CUTRATE PARAM IF WE'RE MAKING RARITY AFFECT ARMOR CUTRATES
        armorStatDesc += ApplyCutRateAdditionsFromRarity(rarityId, newArmor);        

        // APPLY RESISTANCE MULTIPLIERS
        ApplyArmorResistanceAdditions(newArmor, rarityId);

        // RANDOMIZE ARMOR'S WEIGHT
        RandomizeLootWeightBasedOnRarity(newArmor, rarityId);

        // STORE AND APPLY SPEFFECTS
        IEnumerable<SpEffectText> speffs = ApplySpEffects(rarityId, [0], newArmor, 1.0f, true, -1, true);

        newArmor.SetValue<int>("iconIdM", this.RarityHandler.GetIconIdForRarity(newArmor.GetValue<int>("iconIdM"), rarityId));
        newArmor.SetValue<int>("iconIdF", this.RarityHandler.GetIconIdForRarity(newArmor.GetValue<int>("iconIdF"), rarityId));

        // CREATE NEW ARMOR TITLE
        string originalName = newArmor.GetValue<string>("Name");
        string finalTitle = CreateLootTitle(originalName.Replace(" (Altered)", " (Alt)"), rarityId, "", speffs, true, false);

        // APPLY NEW ARMOR TITLE
        //newArmor.SetValue("Name", finalTitle);

        ExportLootGenParamAndTextToOutputs(newArmor, LootType.Armor, finalTitle, CreateArmorDescription(string.Join(Environment.NewLine, speffs.Select(s => s.Description).ToList()), armorStatDesc + GetParamLootLore(finalTitle, true)));

        // RETURN NEW ARMOR'S ID FOR INSTANT ADDITION TO ITEMLOT
        return newArmor.GetValue<int>("ID");
    }
    public string ApplyCutRateAdditionsFromRarity(int rarityId, GenericDictionary outputDictionary)
    {
        // CREATE STRING TO RETURN A PRECOMPILED DESCRIPTION FOR EASY ADDITION
        string descriptionString = "";

        // GET THE PARAMS WE'LL BE WORKING WITH
        List<string> cutRateParams = this.Configuration.LootParam.ArmorParam;
        List<string> defenseParams = this.Configuration.LootParam.ArmorDefenseParams;

        // ITERATE OVER ALL PARAMS HERE, IF THE DICTIONARY HAS THAT PARAM, SUBTRACT A RARITY-DEFINED EXTRA CUTRATE FROM THE ORIGINAL VALUE
        if (cutRateParams.Count > 0)
        {
            foreach (string param in cutRateParams)
            {
                if (outputDictionary.ContainsKey(param))
                {
                    float oldValue = outputDictionary.GetValue<float>(param);
                    outputDictionary.SetValue(param, oldValue - RarityHandler.GetRarityArmorCutRateAddition(rarityId));
                }
            }
        }

        // INITIALISE ALL DEFENCE PARAMS JUST TO BE SURE
        if (defenseParams.Count > 0)
        {
            foreach (string param in defenseParams)
            {
                if (outputDictionary.ContainsKey(param))
                {
                    outputDictionary.SetValue(param, 0);
                }
            }
        }

        return descriptionString;
    }

    public void ApplyArmorResistanceAdditions(GenericDictionary newArmor, int rarity)
    {
        // GET OUR RESISTANCE ADDITIONS
        List<string> resistances = this.Configuration.LootParam.ArmorResistParams;

        // IF WE FOUND ANY PARAMS, MULTIPLY THEM BY A VALUE BETWEEN A RARITY DEFINED MIN AND MAX MULTIPLIER
        if (resistances.Count > 0)
        {
            foreach (string param in resistances)
            {
                if (newArmor.ContainsKey(param))
                {
                    int oldValue = newArmor.GetValue<int>(param);
                    newArmor.SetValue(param, (int)(oldValue * RarityHandler.GetRarityArmorresistmultMultiplier(rarity)));
                }
            }
        }
    }

    public string CreateArmorDescription(string speffects = "", string extraProtection = "")
    {
        return $"{speffects}{Environment.NewLine}{extraProtection}";
    }

    // ARMOR INFORMATION FUNCTIONS

    public void UpdateCutRateParamNames()
    {
        ArmorCutRateParamNames = GetCutRateParamNameArray();
        ArmorCutRateParamRealNames = GetCutRateParamRealNameArray();
    }

    public List<string> GetCutRateParamNameArray()
    {
        return this.Configuration.LootParam.ArmorParam;
    }

    public List<string> GetCutRateParamRealNameArray()
    {
        return this.Configuration.LootParam.ArmorRealName;
    }

    public string GetDescriptionCutRateStringWithFormatting(string amount = "0", string dmgType = "")
    {
        return CutRateDescString.Replace("{amt}", amount).Replace("{type}", dmgType);
    }
}