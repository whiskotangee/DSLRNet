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

public class TalismanLootGenerator : ParamLootGenerator<EquipParamAccessory>
{
    public TalismanLootGenerator(
        IOptions<Configuration> configuration,
        RarityHandler rarityHandler,
        AllowListHandler whitelistHandler,
        SpEffectHandler spEffectHandler,
        RandomProvider random,
        LoreGenerator loreGenerator,
        ParamEditsRepository dataRepository,
        IDataSource<TalismanConfig> talismanConfigDataSource,
        IDataSource<EquipParamAccessory> talismanParamDataSource)
        : base(rarityHandler, whitelistHandler, spEffectHandler, loreGenerator, random, configuration, dataRepository, ParamNames.EquipParamAccessory)
    {
        CumulativeID = new CumulativeID() { IDMultiplier = 10 };

        TalismanConfigs = talismanConfigDataSource.GetAll().ToList();

        DataSource = talismanParamDataSource;
    }

    public int CreateTalisman(int rarityId = 0, List<int> allowListIds = null)
    {
        string accessoryGroupParam = Configuration.LootParam.TalismansAccessoryGroupParam;

        List<string> talismanDescriptions = [];
        List<string> talismanSummaries = [];

        EquipParamAccessory newTalisman = GetNewLootItem(WhiteListHandler.GetLootByAllowList(allowListIds, LootType.Talisman));

        int freeSlotCount = GetAvailableSpeffectSlotCount(newTalisman.GenericParam);

        if (freeSlotCount <= 0)
        {
            return -1;
        }

        TalismanConfig newTalismanConfig = Random.GetRandomItem(TalismanConfigs);

        var availableSlot = GetAvailableSpEffectSlots(newTalisman.GenericParam).First();

        newTalisman.ID = CumulativeID.GetNext();
        newTalisman.rarity = RarityHandler.GetRarityParamValue(rarityId);
        newTalisman.accessoryGroup = newTalismanConfig.NoStackingGroupID < 0
            ? AccessoryGroupCumulativeID.GetNext()
            : newTalismanConfig.NoStackingGroupID;

        newTalisman.iconId = RarityHandler.GetIconIdForRarity(newTalisman.iconId, rarityId);
        newTalisman.GenericParam.SetValue(availableSlot, newTalismanConfig.RefSpEffect);

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
            .. ApplySpEffects(rarityId, [0], newTalisman.GenericParam, 1.0f, true, -1, false) ?? [],
        ];

        string originalName = newTalisman.Name;
        string finalNameNormal = CreateLootTitle(originalName, rarityId, "", spEffs, true);

        //newTalisman.Name = finalNameNormal;

        talismanDescriptions.AddRange(spEffs.Select(s => s.Description).Where(s => !string.IsNullOrWhiteSpace(s)));
        talismanSummaries.AddRange(spEffs.Select(s => s.Summary).Where(s => !string.IsNullOrWhiteSpace(s)));

        talismanDescriptions.Add(LoreGenerator.GenerateDescription(finalNameNormal, false));

        string talismanFinalTitleColored = CreateLootTitle(
         originalName,
         rarityId,
         string.Empty,
         spEffs,
         true);

        ExportLootDetails(newTalisman.GenericParam, LootType.Talisman, talismanFinalTitleColored, string.Join(Environment.NewLine, talismanDescriptions.Select(s => s.Replace(Environment.NewLine, "")).ToList()), string.Join(Environment.NewLine, talismanSummaries), [], []);

        return newTalisman.ID;
    }

    private int GetAvailableSpeffectSlotCount(GenericParam newTalisman)
    {
        List<string> parms = GetPassiveSpEffectSlotArrayFromOutputParamName();

        return parms.Where(d => new List<int>() { 0, -1 }.Contains(newTalisman.GetValue<int>(d))).ToList().Count;
    }

    public static CumulativeID AccessoryGroupCumulativeID { get; set; }

    public List<TalismanConfig> TalismanConfigs { get; set; } = [];

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
}
