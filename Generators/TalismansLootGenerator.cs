namespace DSLRNet.Generators;

using DSLRNet.Config;
using DSLRNet.Contracts;
using DSLRNet.Data;
using DSLRNet.Data.Generated;
using DSLRNet.Handlers;
using Microsoft.Extensions.Options;
using Mods.Common;
using System;
using System.Collections.Generic;

public class TalismanLootGenerator : ParamLootGenerator
{
    public TalismanLootGenerator(
        IOptions<Configuration> configuration,
        RarityHandler rarityHandler,
        AllowListHandler whitelistHandler,
        SpEffectHandler spEffectHandler,
        RandomNumberGetter random,
        LoreGenerator loreGenerator,
        DamageTypeHandler damageTypeHandler,
        DataRepository dataRepository)
        : base(rarityHandler, whitelistHandler, spEffectHandler, damageTypeHandler, loreGenerator, random, configuration, dataRepository, ParamNames.EquipParamAccessory)
    {
        this.CumulativeID = new CumulativeID() { IDMultiplier = 10 };

        this.TalismanConfigs = Csv.LoadCsv<TalismanConfig>("DefaultData\\ER\\CSVs\\TalismanConfig.csv");

        var accessoriesLoot = Csv.LoadCsv<EquipParamAccessory>("DefaultData\\ER\\CSVs\\EquipParamAccessory.csv");

        this.LoadedLoot = accessoriesLoot.Select(GenericDictionary.FromObject).ToList();
    }

    public int CreateTalisman(int rarityId = 0, List<int> wllIds = null)
    {
        string accessoryGroupParam = this.Configuration.LootParam.TalismansAccessoryGroupParam;

        // INITIALISE TALISMAN DESCRIPTION AND SUMMARY
        List<string> talismanDescriptions = [];
        List<string> talismanSummaries = [];

        // CREATE OUR NEW TALISMAN
        GenericDictionary newTalisman = GetLootDictionaryFromId(this.WhiteListHandler.GetLootByWhiteList(wllIds, LootType.Talisman));

        // CHOOSE A SET OF NEW SPEFFECTS BASED ON RARITY
        // STORE HOW MANY FREE SPEFFECT SLOTS OUR PARAM HAS
        int freeSlotCount = GetAvailableSpeffectSlotCount(newTalisman);

        // DON'T CONTINUE IF THERE'S NO FREE SLOTS - IF THEY'RE FULL BY DEFAULT THEY'RE PROBABLY NECESSARY FOR THE TALISMAN TO WORK,
        // AND THERE'S NO POINT USING IT AS A BASE IF WE CAN'T REALLY ADD ANYTHING TO IT
        if (freeSlotCount <= 0)
        {
            return -1;
        }

        //// GET OUR TALISMAN'S CONFIG
        TalismanConfig newTalismanConfig = this.Random.GetRandomItem(this.TalismanConfigs);

        var availableSlot = this.GetAvailableSpEffectSlots(newTalisman).First();

        newTalisman.SetValue<int>(availableSlot, newTalismanConfig.RefSpEffect);

        List<SpEffectText> spEffs =
        [
            new SpEffectText()
            {
                ID = newTalismanConfig.RefSpEffect,
                Description = GetTalismanConfigEffectDescription(newTalismanConfig),
                Summary = !string.IsNullOrEmpty(newTalismanConfig.ShortEffect) ? newTalismanConfig.ShortEffect : newTalismanConfig.Effect,
                NameParts = new NameParts()
                {
                    Prefix = newTalismanConfig.NamePrefix,
                    Interfix = string.Empty,
                    Suffix = string.Empty
                }
            },
            .. ApplySpEffects(rarityId, [0], newTalisman, 1.0f, true, -1, false) ?? [],
        ];

        // STORE ORIGINAL TALISMAN NAME
        string originalName = newTalisman.GetValue<string>("Name");
        string finalNameNormal = CreateLootTitle(originalName, rarityId, "", spEffs, true);

        // SET NEW NAME
        //newTalisman.SetValue("Name", finalNameNormal);

        // CREATE FINAL DESCRIPTION AND - IN THIS CASE - SUMMARY
        talismanDescriptions.AddRange(spEffs.Select(s => s.Description).Where(s => !string.IsNullOrWhiteSpace(s)));
        talismanSummaries.AddRange(spEffs.Select(s => s.Summary).Where(s => !string.IsNullOrWhiteSpace(s)));

        talismanDescriptions.Add(this.LoreGenerator.GenerateDescription(finalNameNormal, false));

        // ASSIGN NEW ID
        ApplyNextId(newTalisman);

        // SET ACCESSORY GROUP
        SetTalismanGroupVariables(newTalismanConfig, newTalisman, accessoryGroupParam);

        // SET TALISMAN RARITY
        SetLootRarityParamValue(newTalisman, rarityId);

        newTalisman.SetValue<int>("iconId", this.RarityHandler.GetIconIdForRarity(newTalisman.GetValue<int>("iconId"), rarityId));

        string talismanFinalTitleColored = CreateLootTitle(
         originalName,
         rarityId,
         string.Empty,
         spEffs,
         true);

        // EXPORT PARAMETERS
        ExportLootGenParamAndTextToOutputs(newTalisman, LootType.Talisman, talismanFinalTitleColored, string.Join(Environment.NewLine, talismanDescriptions.Select(s => s.Replace(Environment.NewLine,"")).ToList()), string.Join(Environment.NewLine, talismanSummaries), [], []);

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
        int accGroup = TalismanCanBeStacked(talisConfig) 
            ? AccessoryGroupCumulativeID.GetNext() 
            : talisConfig.NoStackingGroupID;
        newTalisman.SetValue(accGroupName, accGroup);
    }

    // TALISMAN INFORMATION FUNCTIONS

    public bool TalismanCanBeStacked(TalismanConfig talisConfig)
    {
        if (talisConfig.NoStackingGroupID == 0)
        {
            return false;
        }

        return talisConfig.NoStackingGroupID <= 0;
    }

    public string GetTalismanConfigEffectDescription(TalismanConfig config)
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
