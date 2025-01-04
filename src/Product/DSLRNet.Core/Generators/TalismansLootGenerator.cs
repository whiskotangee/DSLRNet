namespace DSLRNet.Core.Generators;

using DSLRNet.Core.Common;
using DSLRNet.Core.Config;
using DSLRNet.Core.Contracts;
using DSLRNet.Core.DAL;
using DSLRNet.Core.Handlers;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

public class TalismanLootGenerator : ParamLootGenerator<EquipParamAccessory>
{
    public TalismanLootGenerator(
        IOptions<Configuration> configuration,
        IOptions<Settings> settings,
        RarityHandler rarityHandler,
        SpEffectHandler spEffectHandler,
        RandomProvider random,
        LoreGenerator loreGenerator,
        ParamEditsRepository dataRepository,
        DataAccess dataAccess,
        ILogger<ParamLootGenerator<EquipParamAccessory>> logger)
        : base(rarityHandler, spEffectHandler, loreGenerator, random, configuration, settings, dataRepository, ParamNames.EquipParamAccessory, logger)
    {
        this.IDGenerator = new IDGenerator() 
        {
            StartingID = 80000,
            Multiplier = 10 
        };

        this.TalismanConfigs = dataAccess.TalismanConfig.GetAll().ToList();

        this.DataSource = dataAccess.EquipParamAccessory;
    }

    public int CreateTalisman(int rarityId = 0)
    {
        List<string> talismanDescriptions = [];
        List<string> talismanSummaries = [];

        EquipParamAccessory newTalisman = this.DataSource.GetRandomItem().Clone();

        var freeSpEffectSlots = this.GetAvailablePassiveSpEffectSlots(newTalisman.GenericParam);

        if (freeSpEffectSlots.Count <= 0)
        {
            return 0;
        }

        TalismanConfig newTalismanConfig = this.Random.GetRandomItem(this.TalismanConfigs);

        string availableSlot = this.GetAvailablePassiveSpEffectSlots(newTalisman.GenericParam).First();

        newTalisman.ID = (int)this.IDGenerator.GetNext();
        newTalisman.rarity = this.RarityHandler.GetRarityParamValue(rarityId);
        newTalisman.accessoryGroup = newTalismanConfig.NoStackingGroupID;

        newTalisman.iconId = this.RarityHandler.GetIconId(newTalisman.iconId, rarityId);
        newTalisman.SetValue(availableSlot, newTalismanConfig.RefSpEffect);

        List<SpEffectDetails> spEffs =
        [
            new SpEffectDetails()
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

        this.GeneratedDataRepository.AddParamEdit(
            new ParamEdit
            {
                ParamName = this.OutputParamName,
                Operation = ParamOperation.Create,
                ItemText = new LootFMG()
                {
                    Category = this.OutputLootRealNames[LootType.Talisman],
                    Name = talismanFinalTitleColored,
                    Caption = string.Join(Environment.NewLine, talismanDescriptions.Select(s => s.Replace(Environment.NewLine, "")).ToList()),
                    Info = string.Join(Environment.NewLine, talismanSummaries)
                },
                ParamObject = newTalisman.GenericParam
            });

        return newTalisman.ID;
    }

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
