using DSLRNet.Config;
using DSLRNet.Contracts;
using DSLRNet.Data;
using DSLRNet.Handlers;
using Microsoft.Extensions.Options;
using Mods.Common;
using Serilog;

namespace DSLRNet.Generators;

public enum OutputText { Param, Title, Summary, Description }
public enum MEOutput { PARAM, TEXT }

public class ParamLootGenerator(
    RarityHandler rarityHandler,
    WhiteListHandler whiteListHandler,
    SpEffectHandler spEffectHandler,
    DamageTypeHandler damageTypeHandler,
    LoreGenerator loreGenerator,
    RandomNumberGetter random,
    IOptions<Configuration> configuration,
    CumulativeID cumulativeID,
    DataRepository dataRepository) : BaseHandler(dataRepository)
{
    // RARITY HANDLER - ALL LOOT IS HANDLED BY RARITY
    public RarityHandler RarityHandler { get; set; } = rarityHandler;

    // WHITELIST ID HANDLER - FOR ENEMY SPECIFIC LOOT
    public WhiteListHandler WhiteListHandler { get; set; } = whiteListHandler;

    public SpEffectHandler SpEffectHandler { get; set; } = spEffectHandler;

    // DAMAGETYPE HANDLER
    public DamageTypeHandler DamageTypeHandler { get; set; } = damageTypeHandler;

    // LORE HANDLER
    public LoreGenerator LoreGenerator { get; set; } = loreGenerator;

    public RandomNumberGetter Random { get; set; } = random;

    public Configuration Configuration { get; set; } = configuration.Value;

    // CUMULATIVE ID TO HANDLE ID ASSIGNMENTS
    public CumulativeID CumulativeID { get; set; } = cumulativeID;

    public List<GenericDictionary> LoadedLoot = [];

    // THE CURRENT POOL OF PRIORITY IDS FOR THIS GENERATION - WHEN WE CHOOSE LOOT AT RANDOM, WE SHOULD PICK ONE OF THESE AT SOME POINT AND REMOVE THAT ID FROM THE SET
    private List<int> PriorityIDs_Current = [];  

    // HEADERS ARRAY STORAGE - THIS WILL BE GRABBED FROM GLOBALVARIABLES BASED ON THE CURRENT GAMETYPES ONCE MODSREADY SIGNAL IS RECEIVED
    private List<string> ParamMandatoryKeys = [];

    // ALL DSL GENERATORS TEXT FUNCTIONS
    // CUSTOMIZABLE OUTPUTLOOTTYPE AND PARAMS NAMED SO WE CAN QUICKLY 
    // PUT TOGETHER THE CORRECT FILENAMES ON THE FLY

    public string OutputParamName = "EquipParamWeapon";

    public Dictionary<LootType, string> OutputLootRealNames = new()
    {
        { LootType.Weapon, "Weapon" },
        { LootType.Armor, "Protector" },
        { LootType.Talisman, "Accessory" }
    };

    public void ExportLootGenParamAndTextToOutputs(GenericDictionary massEditDict, LootType lootType, string title = "", string description = "", string summary = "", List<string> extraFilters = null, List<string> extraBannedValues = null, bool multiName = false)
    {
        string finalMassEditOutput = CreateMassEditParamFromParamDictionary(massEditDict, OutputParamName, massEditDict.ContainsKey("ID") ? massEditDict.GetValue<int>("ID") : 0, extraFilters, extraBannedValues, ParamMandatoryKeys);

        this.GeneratedDataRepository.AddParamEdit(
            OutputParamName,
            ParamOperation.Create,
            finalMassEditOutput,
            CreateFmgLootEntrySet(OutputLootRealNames[lootType], massEditDict.GetValue<int>("ID"), title, description, summary, multiName),
            massEditDict);
    }

    public void ApplyNextId(GenericDictionary outputDictionary)
    {
        outputDictionary.SetValue("ID",this.CumulativeID.GetNext());
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
        List<string> speffectParam = !overwriteExistingSpEffects ? GetAvailableSpEffectSlots(outputDictionary) : GetPassiveSpEffectSlotArrayFromOutputParamName();
        if (speffectParam.Count == 0)
        {
            Log.Logger.Warning($"{outputDictionary.GetValue<int>("ID")} HAS NO AVAILABLE SPEFFECT SLOTS APPARENTLY! RETURNING EMPTY DICTIONARY");
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
                outputDictionary.SetValue<long>(speffectParam[x], speffectsToApply[x].ID);
                
                yield return speffectsToApply[x];
            }
        }
        else
        {
            Log.Logger.Warning("APPLY SPEFFECTS CALL INVALID, RETURNING EMPTY DICTIONARY, CALLING FUNCTION SHOULD BE ABLE TO ACCOUNT FOR THIS!");
        }
    }

    public string CreateLootTitle(string originalTitle, int rarityId, string damageType, SpEffectText? nameParts, bool colorCoded = false)
    {
        List<string> additions =
        [
            !colorCoded ? this.RarityHandler.GetRarityName(rarityId) : this.RarityHandler.GetColorTextForRarity(rarityId),
            damageType,
            nameParts?.NameParts.Prefix ?? string.Empty,
            //nameParts?.NameParts.Interfix ?? string.Empty,
            originalTitle,
            nameParts?.NameParts.Suffix ?? string.Empty
        ];

        return string.Join(" ", additions.Where(d => !string.IsNullOrWhiteSpace(d)).Distinct());
    }

    public GenericDictionary GetLootDictionaryFromId(int baseId = -1)
    {
        if (this.Configuration.Settings.ChaosLootEnabled)
        {
            return ChooseLootDictionaryAtRandom().Clone() as GenericDictionary;
        }

        if (baseId == -1 && PriorityIDs_Current.Count > 0)
        {
            baseId = ChoosePriorityIdAtRandom();
        }

        return this.LoadedLoot.First(d => d.GetValue<int>("ID") == baseId).Clone() as GenericDictionary;
    }

    public GenericDictionary ChooseLootDictionaryAtRandom()
    {
        int randomIndex = this.Random.NextInt(0, LoadedLoot.Count - 1);
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
            float newWeight = (float)Math.Round(originalWeight * this.Random.NextDouble(minMult, maxMult), 2);
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
        lootDict.SetValue(GetSellValueParam(), (int)(RarityHandler.GetRaritySellValue(rarityId) * mult));
    }

    public void SetLootRarityParamValue(GenericDictionary lootDict, int rarityId)
    {
        string rarityParamName = this.Configuration.LootParam.RarityParam;
        if (lootDict.ContainsKey(rarityParamName))
        {
            lootDict.SetValue(rarityParamName, this.RarityHandler.GetRarityParamInt(rarityId));
        }
    }

    public int ChoosePriorityIdAtRandom()
    {
        int chosenIndex = this.Random.NextInt(0, PriorityIDs_Current.Count - 1);
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
        return this.Configuration.LootParam.Speffects.GetType().GetProperty(OutputParamName).GetValue(this.Configuration.LootParam.Speffects) as List<string>;
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
        return this.LoreGenerator.GenerateDescription(lootName, armorIfTrue);
    }

    public string GetSellValueParam()
    {
        return this.Configuration.LootParam.SellValueParam;
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
