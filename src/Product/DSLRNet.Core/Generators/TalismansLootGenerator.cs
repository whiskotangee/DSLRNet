namespace DSLRNet.Core.Generators;

using DSLRNet.Core.Common;
using DSLRNet.Core.Config;
using DSLRNet.Core.Contracts;
using DSLRNet.Core.DAL;
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
        SpEffectHandler spEffectHandler,
        RandomProvider random,
        LoreGenerator loreGenerator,
        ParamEditsRepository dataRepository,
        DataAccess dataAccess,
        ILogger<ParamLootGenerator<EquipParamAccessory>> logger)
        : base(rarityHandler, spEffectHandler, loreGenerator, random, configuration, dataRepository, ParamNames.EquipParamAccessory, logger)
    {
        this.CumulativeID = new CumulativeID(logger) { IDMultiplier = 10 };

        this.TalismanConfigs = dataAccess.TalismanConfig.GetAll().ToList();

        this.DataSource = dataAccess.EquipParamAccessory;
    }

    public int CreateTalisman(int rarityId = 0)
    {
        List<string> talismanDescriptions = [];
        List<string> talismanSummaries = [];

        EquipParamAccessory newTalisman = this.GetNewLootItem();

        int freeSlotCount = this.GetAvailableSpeffectSlotCount(newTalisman.GenericParam);

        if (freeSlotCount <= 0)
        {
            return 0;
        }

        TalismanConfig newTalismanConfig = this.Random.GetRandomItem(this.TalismanConfigs);

        string availableSlot = this.GetAvailableSpEffectSlots(newTalisman.GenericParam).First();

        newTalisman.ID = (int)this.CumulativeID.GetNext();
        newTalisman.rarity = this.RarityHandler.GetRarityParamValue(rarityId);
        newTalisman.accessoryGroup = newTalismanConfig.NoStackingGroupID < 0
            ? (short)AccessoryGroupCumulativeID.GetNext()
            : newTalismanConfig.NoStackingGroupID;

        newTalisman.iconId = this.RarityHandler.GetIconId(newTalisman.iconId, rarityId);
        newTalisman.GenericParam.SetValue(availableSlot, newTalismanConfig.RefSpEffect);

        List<SpEffectText> spEffs =
        [
            new SpEffectText()
            {
                ID = newTalismanConfig.RefSpEffect,
                Description = this.GetTalismanConfigEffectDescription(newTalismanConfig),
                Summary = !string.IsNullOrEmpty(newTalismanConfig.ShortEffect) ? newTalismanConfig.ShortEffect : newTalismanConfig.Effect,
                NameParts = new NameParts()
                {
                    Prefix = newTalismanConfig.NamePrefix,
                    Interfix = string.Empty,
                    Suffix = string.Empty
                }
            },
            .. this.ApplySpEffects(rarityId, [0], newTalisman.GenericParam, 1.0f, LootType.Talisman, -1, false) ?? [],
        ];

        string originalName = newTalisman.Name;
        string finalNameNormal = this.CreateLootTitle(originalName, rarityId, "", spEffs, true);

        //newTalisman.Name = finalNameNormal;

        talismanDescriptions.AddRange(spEffs.Select(s => s.Description).Where(s => !string.IsNullOrWhiteSpace(s)));
        talismanSummaries.AddRange(spEffs.Select(s => s.Summary).Where(s => !string.IsNullOrWhiteSpace(s)));

        talismanDescriptions.Add(this.LoreGenerator.GenerateDescription(finalNameNormal, false));

        string talismanFinalTitleColored = this.CreateLootTitle(
         originalName,
         rarityId,
         string.Empty,
         spEffs,
         true);

        this.AddLootDetails(
            newTalisman.GenericParam, 
            LootType.Talisman, 
            talismanFinalTitleColored, 
            string.Join(Environment.NewLine, talismanDescriptions.Select(s => s.Replace(Environment.NewLine, "")).ToList()), 
            string.Join(Environment.NewLine, talismanSummaries), 
            [],
            []);

        return newTalisman.ID;
    }

    private int GetAvailableSpeffectSlotCount(GenericParam newTalisman)
    {
        List<string> parms = this.GetPassiveSpEffectSlotArrayFromOutputParamName();

        return parms.Where(d => new List<int>() { 0, -1 }.Contains(newTalisman.GetValue<int>(d))).ToList().Count;
    }

    public static CumulativeID AccessoryGroupCumulativeID { get; set; }

    public List<TalismanConfig> TalismanConfigs { get; set; } = [];

    public string GetTalismanConfigEffectDescription(TalismanConfig config)
    {
        string effectPrefixString = this.Configuration.DSLRDescText.Effect;

        if (config == null)
        {
            return string.Empty;
        }

        string effectString = config.Effect;
        string stackString = config.NoStackingGroupID < 0 ? this.Configuration.DSLRDescText.NoStacking : string.Empty;
        return effectPrefixString + effectString + stackString + Environment.NewLine;
    }
}
