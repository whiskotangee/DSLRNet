namespace DSLRNet.Generators;

using DSLRNet.Config;
using DSLRNet.Contracts;
using DSLRNet.Data;
using DSLRNet.Handlers;
using Microsoft.Extensions.Options;
using Mods.Common;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;

public class TalismanLootGenerator : ParamLootGenerator
{
    public TalismanLootGenerator(
        IOptions<Configuration> configuration,
        RarityHandler rarityHandler,
        WhiteListHandler whitelistHandler,
        SpEffectHandler spEffectHandler,
        RandomNumberGetter random,
        LoreGenerator loreGenerator,
        DamageTypeHandler damageTypeHandler,
        DataRepository dataRepository,
        CumulativeID cumulativeID)
        : base(rarityHandler, whitelistHandler, spEffectHandler, damageTypeHandler, loreGenerator, random, configuration, cumulativeID, dataRepository)
    {
        this.TalismanConfigs = Csv.LoadCsv<TalismanConfig>("DefaultData\\ER\\CSVs\\TalismanConfig.csv");

        this.LoadedLoot = Csv.LoadCsv<EquipParamAccessory>("DefaultData\\ER\\CSVs\\EquipParamAccessory.csv").Select(GenericDictionary.FromObject).ToList();
        OutputParamName = "EquipParamAccessory";
    }

    public int CreateTalisman(int rarityId = 0, List<int> wllIds = null)
    {
        string accessoryGroupParam = this.Configuration.LootParam.TalismansAccessoryGroupParam;

        // INITIALISE TALISMAN DESCRIPTION AND SUMMARY
        string talismanDesc = "";
        string talismanSummary = "";

        // CREATE OUR NEW TALISMAN
        GenericDictionary newTalisman = GetLootDictionaryFromId(this.WhiteListHandler.GetLootByWhiteList(wllIds, LootType.Talisman));

        // GET OUR TALISMAN'S CONFIG
        TalismanConfig newTalismanConfig = GetRandomTalismanConfig();

        // ADD NEW TALISMAN'S DETAILS TO DESCRIPTION AND SUMMARY
        string talismanEffectDesc = GetTalismanConfigEffectDescription(newTalismanConfig, newTalisman.GetValue<int>("ID"));
        string talismanEffectSummary = !string.IsNullOrEmpty(newTalismanConfig.ShortEffect) ? newTalismanConfig.ShortEffect : newTalismanConfig.Effect;

        talismanDesc += talismanEffectDesc;
        talismanSummary += talismanEffectSummary + " - ";

        // CHOOSE A SET OF NEW SPEFFECTS BASED ON RARITY
        // STORE HOW MANY FREE SPEFFECT SLOTS OUR PARAM HAS
        int freeSlotCount = GetAvailableSpeffectSlotCount(newTalisman);

        // DON'T CONTINUE IF THERE'S NO FREE SLOTS - IF THEY'RE FULL BY DEFAULT THEY'RE PROBABLY NECESSARY FOR THE TALISMAN TO WORK,
        // AND THERE'S NO POINT USING IT AS A BASE IF WE CAN'T REALLY ADD ANYTHING TO IT
        if (freeSlotCount <= 0)
        {
            return -1;
        }

        IEnumerable<SpEffectText> spEffs = ApplySpEffects(rarityId, [0], newTalisman, 1.0f, true, -1, true);

        // STORE ORIGINAL TALISMAN NAME
        string originalName = newTalisman.GetValue<string>("Name");
        string finalNameNormal = CreateLootTitle(originalName, rarityId, "", spEffs.First(), true);

        // SET NEW NAME
        newTalisman.SetValue("Name", finalNameNormal);

        // CREATE FINAL DESCRIPTION AND - IN THIS CASE - SUMMARY
        talismanDesc += spEffs.First().Description;
        talismanSummary += spEffs.First().Summary;

        // ASSIGN NEW ID
        ApplyNextId(newTalisman);

        // SET ACCESSORY GROUP
        SetTalismanGroupVariables(newTalismanConfig, newTalisman, accessoryGroupParam);

        // SET TALISMAN RARITY
        SetLootRarityParamValue(newTalisman, rarityId);

        string talismanOriginalTitle = newTalisman.GetValue<string>("Name");
        string rarityName = this.RarityHandler.GetRarityName(rarityId);

        string talismanFinalTitleColored = CreateLootTitle(
         talismanOriginalTitle,
         rarityId,
         string.Empty,
         spEffs.First(),
         true);

        // EXPORT PARAMETERS
        ExportLootGenParamAndTextToOutputs(newTalisman, LootType.Talisman, talismanFinalTitleColored, talismanDesc, talismanSummary, [], [], false);

        return newTalisman.GetValue<int>("ID");
    }

    private int GetAvailableSpeffectSlotCount(GenericDictionary newTalisman)
    {
        List<string> parms = this.GetPassiveSpEffectSlotArrayFromOutputParamName();

        return parms.Where(d => new List<int>() { 0, -1 }.Contains(newTalisman.GetValue<int>(d))).ToList().Count;
    }

    // WE NEED AN EXTRA CUMULATIVE ID FOR "CATEGORY" SO TALISMANS CAN BE STACKED TOGETHER
    public static CumulativeID AccessoryGroupCumulativeID { get; set; }

    // IN ORDER FOR A TALISMAN TO BE USED, WE'LL NEED A CONFIG FOR IT SO WE CAN GET THE ORIGINAL TALISMAN'S EFFECT DESCRIPTION
    public List<TalismanConfig> TalismanConfigs { get; set; } = [];

    public void SetTalismanGroupVariables(TalismanConfig talisConfig, GenericDictionary newTalisman, string accGroupName)
    {
        int accGroup = TalismanCanBeStacked(talisConfig) ? AccessoryGroupCumulativeID.GetNext() : talisConfig.NoStackingGroupID;
        newTalisman.SetValue(accGroupName, accGroup);
    }

    // TALISMAN INFORMATION FUNCTIONS

    public bool TalismanCanBeStacked(TalismanConfig talisConfig)
    {
        if (talisConfig.NoStackingGroupID == 0)
        {
            return false;
        }

        return new List<int> { 0, -1 }.Contains(talisConfig.NoStackingGroupID);
    }

    public TalismanConfig GetRandomTalismanConfig()
    {
        return this.Random.GetRandomItem<TalismanConfig>(this.TalismanConfigs);
    }

    public string GetTalismanConfigEffectDescription(TalismanConfig config, int id)
    {
        string effectPrefixString = this.Configuration.DSLRDescText.Effect;

        if (config == null)
        {
            return string.Empty;
        }

        string effectString = config.Effect;
        string stackString = TalismanCanBeStacked(config) ? this.Configuration.DSLRDescText.NoStacking : string.Empty;
        return effectPrefixString + effectString + stackString + Environment.NewLine;
    }
}
