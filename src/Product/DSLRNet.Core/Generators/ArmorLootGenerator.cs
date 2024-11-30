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

public class ArmorLootGenerator : ParamLootGenerator
{
    public ArmorLootGenerator(
        RarityHandler rarityHandler,
        AllowListHandler whiteListHandler,
        SpEffectHandler spEffectHandler,
        DamageTypeHandler damageTypeHandler,
        LoreGenerator loreGenerator,
        RandomNumberGetter random,
        DataRepository dataRepository,
        IOptions<Configuration> configuration)
        : base(rarityHandler, whiteListHandler, spEffectHandler, damageTypeHandler, loreGenerator, random, configuration, dataRepository, ParamNames.EquipParamProtector)
    {
        CumulativeID = new CumulativeID();

        List<EquipParamProtector> armorLoots = Csv.LoadCsv<EquipParamProtector>("DefaultData\\ER\\CSVs\\EquipParamProtector.csv");

        LoadedLoot = armorLoots.Select(GenericDictionary.FromObject).ToList();
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

    public int CreateArmor(int rarityId, List<int> wllIds)
    {
        GenericDictionary newArmor = GetLootDictionaryFromId(WhiteListHandler.GetLootByAllowList(wllIds, LootType.Armor));

        string armorStatDesc = "";

        newArmor.SetValue("ID", CumulativeID.GetNext());

        SetLootSellValue(newArmor, rarityId);

        SetLootRarityParamValue(newArmor, rarityId);

        armorStatDesc += ApplyCutRateAdditionsFromRarity(rarityId, newArmor);

        ApplyArmorResistanceAdditions(newArmor, rarityId);

        RandomizeLootWeightBasedOnRarity(newArmor, rarityId);

        IEnumerable<SpEffectText> speffs = ApplySpEffects(rarityId, [0], newArmor, 1.0f, true, -1, true);

        newArmor.SetValue("iconIdM", RarityHandler.GetIconIdForRarity(newArmor.GetValue<int>("iconIdM"), rarityId));
        newArmor.SetValue("iconIdF", RarityHandler.GetIconIdForRarity(newArmor.GetValue<int>("iconIdF"), rarityId));

        string originalName = newArmor.GetValue<string>("Name");
        string finalTitle = CreateLootTitle(originalName.Replace(" (Altered)", " (Alt)"), rarityId, "", speffs, true, false);

        //newArmor.SetValue("Name", finalTitle);

        ExportLootGenParamAndTextToOutputs(newArmor, LootType.Armor, finalTitle, CreateArmorDescription(string.Join(Environment.NewLine, speffs.Select(s => s.Description).ToList()), armorStatDesc + GetParamLootLore(finalTitle, true)));

        return newArmor.GetValue<int>("ID");
    }

    public string ApplyCutRateAdditionsFromRarity(int rarityId, GenericDictionary outputDictionary)
    {
        string descriptionString = "";

        List<string> cutRateParams = Configuration.LootParam.ArmorParam;
        List<string> defenseParams = Configuration.LootParam.ArmorDefenseParams;

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
        List<string> resistances = Configuration.LootParam.ArmorResistParams;

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
}