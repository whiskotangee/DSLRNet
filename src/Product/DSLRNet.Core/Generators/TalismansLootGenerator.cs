namespace DSLRNet.Core.Generators;

using DSLRNet.Core.Common;
using DSLRNet.Core.Config;
using DSLRNet.Core.Contracts;
using DSLRNet.Core.Contracts.Params;
using DSLRNet.Core.Data;
using DSLRNet.Core.Handlers;
using Microsoft.Extensions.Options;
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
        ParamEditsRepository dataRepository,
        IDataSource<TalismanConfig> talismanConfigDataSource,
        IDataSource<EquipParamAccessory> talismanParamDataSource)
        : base(rarityHandler, whitelistHandler, spEffectHandler, loreGenerator, random, configuration, dataRepository, ParamNames.EquipParamAccessory)
    {
        CumulativeID = new CumulativeID() { IDMultiplier = 10 };

        TalismanConfigs = talismanConfigDataSource.LoadAll().ToList();

        var accessoriesLoot = talismanParamDataSource.LoadAll();

        LoadedLoot = accessoriesLoot.Select(GenericParam.FromObject).ToList();
    }

    public int CreateTalisman(int rarityId = 0, List<int> allowListIds = null)
    {
        string accessoryGroupParam = Configuration.LootParam.TalismansAccessoryGroupParam;

        List<string> talismanDescriptions = [];
        List<string> talismanSummaries = [];

        GenericParam newTalisman = GetLootDictionaryFromId(WhiteListHandler.GetLootByAllowList(allowListIds, LootType.Talisman));

        int freeSlotCount = GetAvailableSpeffectSlotCount(newTalisman);

        if (freeSlotCount <= 0)
        {
            return -1;
        }

        TalismanConfig newTalismanConfig = Random.GetRandomItem(TalismanConfigs);

        var availableSlot = GetAvailableSpEffectSlots(newTalisman).First();

        newTalisman.SetValue(availableSlot, newTalismanConfig.RefSpEffect);

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

        string originalName = newTalisman.Name;
        string finalNameNormal = CreateLootTitle(originalName, rarityId, "", spEffs, true);

        //newTalisman.Name = finalNameNormal;

        talismanDescriptions.AddRange(spEffs.Select(s => s.Description).Where(s => !string.IsNullOrWhiteSpace(s)));
        talismanSummaries.AddRange(spEffs.Select(s => s.Summary).Where(s => !string.IsNullOrWhiteSpace(s)));

        talismanDescriptions.Add(LoreGenerator.GenerateDescription(finalNameNormal, false));

        newTalisman.ID = CumulativeID.GetNext();

        SetTalismanGroupVariables(newTalismanConfig, newTalisman, accessoryGroupParam);

        SetLootRarityParamValue(newTalisman, rarityId);

        newTalisman.SetValue("iconId", RarityHandler.GetIconIdForRarity(newTalisman.GetValue<int>("iconId"), rarityId));

        string talismanFinalTitleColored = CreateLootTitle(
         originalName,
         rarityId,
         string.Empty,
         spEffs,
         true);

        ExportLootGenParamAndTextToOutputs(newTalisman, LootType.Talisman, talismanFinalTitleColored, string.Join(Environment.NewLine, talismanDescriptions.Select(s => s.Replace(Environment.NewLine, "")).ToList()), string.Join(Environment.NewLine, talismanSummaries), [], []);

        return newTalisman.ID;
    }

    private int GetAvailableSpeffectSlotCount(GenericParam newTalisman)
    {
        List<string> parms = GetPassiveSpEffectSlotArrayFromOutputParamName();

        return parms.Where(d => new List<int>() { 0, -1 }.Contains(newTalisman.GetValue<int>(d))).ToList().Count;
    }

    public static CumulativeID AccessoryGroupCumulativeID { get; set; }

    public List<TalismanConfig> TalismanConfigs { get; set; } = [];

    public void SetTalismanGroupVariables(TalismanConfig talisConfig, GenericParam newTalisman, string accGroupName)
    {
        int accGroup = talisConfig.NoStackingGroupID < 0
            ? AccessoryGroupCumulativeID.GetNext()
            : talisConfig.NoStackingGroupID;
        newTalisman.SetValue(accGroupName, accGroup);
    }

    public string GetTalismanConfigEffectDescription(TalismanConfig config)
    {
        string effectPrefixString = Configuration.DSLRDescText.Effect;

        if (config == null)
        {
            return string.Empty;
        }

        string effectString = config.Effect;
        string stackString = config.NoStackingGroupID < 0 ? Configuration.DSLRDescText.NoStacking : string.Empty;
        return effectPrefixString + effectString + stackString + Environment.NewLine;
    }

    public bool TalismanCanBeStacked(TalismanConfig talisConfig)
    {
        if (talisConfig.NoStackingGroupID == 0)
        {
            return false;
        }

        return talisConfig.NoStackingGroupID <= 0;
    }
}
