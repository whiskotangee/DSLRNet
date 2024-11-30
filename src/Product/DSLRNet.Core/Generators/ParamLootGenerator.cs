using DSLRNet.Core.Common;
using DSLRNet.Core.Config;
using DSLRNet.Core.Contracts;
using DSLRNet.Core.Data;
using DSLRNet.Core.Handlers;
using Microsoft.Extensions.Options;
using Serilog;

namespace DSLRNet.Core.Generators;

public class ParamLootGenerator(
    RarityHandler rarityHandler,
    AllowListHandler whiteListHandler,
    SpEffectHandler spEffectHandler,
    DamageTypeHandler damageTypeHandler,
    LoreGenerator loreGenerator,
    RandomNumberGetter random,
    IOptions<Configuration> configuration,
    DataRepository dataRepository,
    ParamNames outputParamName) : BaseHandler(dataRepository)
{
    public RarityHandler RarityHandler { get; set; } = rarityHandler;

    public AllowListHandler WhiteListHandler { get; set; } = whiteListHandler;

    public SpEffectHandler SpEffectHandler { get; set; } = spEffectHandler;

    public DamageTypeHandler DamageTypeHandler { get; set; } = damageTypeHandler;

    public LoreGenerator LoreGenerator { get; set; } = loreGenerator;

    public RandomNumberGetter Random { get; set; } = random;

    public Configuration Configuration { get; set; } = configuration.Value;

    public CumulativeID CumulativeID { get; set; }

    public List<GenericDictionary> LoadedLoot { get; set; } = [];

    private List<int> PriorityIDs_Current { get; set; } = [];

    private List<string> ParamMandatoryKeys { get; set; } = [];

    public ParamNames OutputParamName { get; } = outputParamName;

    public Dictionary<LootType, string> OutputLootRealNames { get; } = new()
    {
        { LootType.Weapon, "Weapon" },
        { LootType.Armor, "Protector" },
        { LootType.Talisman, "Accessory" }
    };

    public void ExportLootGenParamAndTextToOutputs(GenericDictionary massEditDict, LootType lootType, string title = "", string description = "", string summary = "", List<string> extraFilters = null, List<string> extraBannedValues = null)
    {
        string finalMassEditOutput = CreateMassEditParamFromParamDictionary(massEditDict, OutputParamName, massEditDict.ContainsKey("ID") ? massEditDict.GetValue<int>("ID") : 0, extraFilters, extraBannedValues, ParamMandatoryKeys);

        GeneratedDataRepository.AddParamEdit(
            OutputParamName,
            ParamOperation.Create,
            finalMassEditOutput,
            CreateFmgLootEntrySet(OutputLootRealNames[lootType], massEditDict.GetValue<int>("ID"), title, description, summary),
            massEditDict);
    }

    public void ApplyNextId(GenericDictionary outputDictionary)
    {
        var id = CumulativeID.GetNext();
        outputDictionary.SetValue("ID", id);
    }

    public IEnumerable<SpEffectText> ApplySpEffects(
        int rarityId,
        List<int> allowedSpefTypes,
        GenericDictionary outputDictionary,
        float spefChanceMult = 1.0f,
        bool armorTalisman = false,
        int spefNumOverride = -1,
        bool overwriteExistingSpEffects = false)
    {
        List<SpEffectText> spEffectTexts = [];

        List<string> speffectParam = !overwriteExistingSpEffects ? GetAvailableSpEffectSlots(outputDictionary) : GetPassiveSpEffectSlotArrayFromOutputParamName();
        if (speffectParam.Count == 0)
        {
            Log.Logger.Warning($"{outputDictionary.GetValue<int>("ID")} HAS NO AVAILABLE SPEFFECT SLOTS APPARENTLY! RETURNING EMPTY DICTIONARY");
            return [];
        }

        int finalNumber = spefNumOverride < 0 && spefNumOverride <= speffectParam.Count ? speffectParam.Count : spefNumOverride;
        List<SpEffectText> speffectsToApply = SpEffectHandler.GetSpEffects(finalNumber, allowedSpefTypes, rarityId, armorTalisman, spefChanceMult);

        if (armorTalisman)
        {
            // Additional logic for armorTalisman if needed
        }

        if (speffectsToApply.Count != 0)
        {
            for (int x = 0; x < speffectsToApply.Count; x++)
            {
                outputDictionary.SetValue(speffectParam[x], speffectsToApply[x].ID);

                spEffectTexts.Add(speffectsToApply[x]);
            }
        }
        else if (rarityId > 5)
        {
            Log.Logger.Warning("APPLY SPEFFECTS CALL RESULTED IN NO SPEFFECTS WITH RARITY > 5");
        }

        return spEffectTexts;
    }

    public string CreateLootTitle(string originalTitle, int rarityId, string damageType, IEnumerable<SpEffectText>? namePartsCollection, bool colorCoded = false, bool includeSuffix = false)
    {
        List<string> additions =
        [
            !colorCoded ? RarityHandler.GetRarityName(rarityId) : RarityHandler.GetColorTextForRarity(rarityId),
            damageType,
            namePartsCollection?.Where(d => d?.NameParts?.Prefix != null).FirstOrDefault()?.NameParts?.Prefix ?? string.Empty,
            //nameParts?.NameParts.Interfix ?? string.Empty,
            originalTitle,
            includeSuffix ? namePartsCollection?.Where(d => d?.NameParts?.Suffix != null).FirstOrDefault()?.NameParts?.Suffix ?? string.Empty : string.Empty
        ];

        return string.Join(" ", additions.Where(d => !string.IsNullOrWhiteSpace(d)).Distinct());
    }

    public GenericDictionary GetLootDictionaryFromId(int baseId = -1)
    {
        if (Configuration.Settings.ChaosLootEnabled)
        {
            return ChooseLootDictionaryAtRandom().Clone() as GenericDictionary;
        }

        if (baseId == -1 && PriorityIDs_Current.Count > 0)
        {
            baseId = ChoosePriorityIdAtRandom();
        }

        return LoadedLoot.First(d => d.GetValue<int>("ID") == baseId).Clone() as GenericDictionary;
    }

    public GenericDictionary ChooseLootDictionaryAtRandom()
    {
        int randomIndex = Random.NextInt(0, LoadedLoot.Count - 1);
        return LoadedLoot[randomIndex].Clone() as GenericDictionary;
    }

    public string CreateAffinityTitle(DamageTypeSetup dt1, DamageTypeSetup dt2)
    {
        string firstName = dt1.PriName;
        string secondName = dt2.SecName;
        string space = " ";

        if (string.IsNullOrEmpty(firstName))
        {
            if (dt1.SecName != dt2.SecName)
            {
                firstName = dt1.SecName;
            }
            else
            {
                firstName = "";
                secondName = "";
                space = "";
            }
        }

        if (dt1.SecName == dt2.SecName)
        {
            secondName = "";
            space = "";
        }

        return firstName + space + secondName;
    }

    public void RandomizeLootWeight(GenericDictionary lootDict, float minMult = 0.95f, float maxMult = 1.05f, float absoluteMax = 30.0f)
    {
        if (lootDict.ContainsKey("weight"))
        {
            float originalWeight = lootDict.GetValue<float>("weight");
            float newWeight = (float)Math.Round(originalWeight * Random.NextDouble(minMult, maxMult), 2);
            lootDict.SetValue("weight", (float)Math.Clamp(newWeight, 0.0f, absoluteMax));
        }
    }

    public void RandomizeLootWeightBasedOnRarity(GenericDictionary lootDict, int rarityId = 0)
    {
        List<float> rarityWeight = RarityHandler.GetRarityWeightMultipliers(rarityId);
        RandomizeLootWeight(lootDict, (float)rarityWeight[0], (float)rarityWeight[1]);
    }

    public void SetLootSellValue(GenericDictionary lootDict, int rarityId, float mult = 1.0f)
    {
        lootDict.SetValue(this.Configuration.LootParam.SellValueParam, (int)(RarityHandler.GetRaritySellValue(rarityId) * mult));
    }

    public void SetLootRarityParamValue(GenericDictionary lootDict, int rarityId)
    {
        string rarityParamName = Configuration.LootParam.RarityParam;
        if (lootDict.ContainsKey(rarityParamName))
        {
            lootDict.SetValue(rarityParamName, RarityHandler.GetRarityParamInt(rarityId));
        }
    }

    public int ChoosePriorityIdAtRandom()
    {
        int chosenIndex = Random.NextInt(0, PriorityIDs_Current.Count - 1);
        int chosenId = PriorityIDs_Current[chosenIndex];
        if (chosenId != -1)
        {
            Log.Logger.Debug($"PRIORITY ID {chosenId} SELECTED! REMAINING PRIORITY IDS: {string.Join(", ", PriorityIDs_Current)}");
        }
        PriorityIDs_Current.RemoveAt(chosenIndex);
        return chosenId;
    }

    public List<string> GetPassiveSpEffectSlotArrayFromOutputParamName()
    {
        return Configuration.LootParam.Speffects.GetType().GetProperty(OutputParamName.ToString()).GetValue(Configuration.LootParam.Speffects) as List<string>;
    }

    public List<string> GetAvailableSpEffectSlots(GenericDictionary lootDict)
    {
        List<string> baseParams = GetPassiveSpEffectSlotArrayFromOutputParamName();
        List<string> finalArray = [];
        foreach (string param in baseParams)
        {
            if (lootDict.ContainsKey(param) && lootDict.GetValue<int>(param) <= 0)
            {
                finalArray.Add(param);
            }
        }
        return finalArray;
    }

    public string GetParamLootLore(string lootName = "Dagger", bool armorIfTrue = false)
    {
        return LoreGenerator.GenerateDescription(lootName, armorIfTrue);
    }

    public bool HasNoLootTemplates()
    {
        bool result = LoadedLoot.Count == 0;
        if (result)
        {
            Log.Logger.Debug($"{Name} has no LootTemplateLibrary entries!");
        }
        return result;
    }
}
