namespace DSLRNet.Core.Generators;

public class ParamLootGenerator<TParamType>(
    RarityHandler rarityHandler,
    AllowListHandler whiteListHandler,
    SpEffectHandler spEffectHandler,
    LoreGenerator loreGenerator,
    RandomProvider random,
    IOptions<Configuration> configuration,
    ParamEditsRepository dataRepository,
    ParamNames outputParamName) : BaseHandler(dataRepository)
{
    public RarityHandler RarityHandler { get; set; } = rarityHandler;

    public AllowListHandler WhiteListHandler { get; set; } = whiteListHandler;

    public SpEffectHandler SpEffectHandler { get; set; } = spEffectHandler;

    public LoreGenerator LoreGenerator { get; set; } = loreGenerator;

    public RandomProvider Random { get; set; } = random;

    public Configuration Configuration { get; set; } = configuration.Value;

    public CumulativeID CumulativeID { get; set; }

    public IDataSource<TParamType> DataSource { get; set; }

    private List<int> PriorityIDs_Current { get; set; } = [];

    private List<string> ParamMandatoryKeys { get; set; } = [];

    public ParamNames OutputParamName { get; } = outputParamName;

    public Dictionary<LootType, string> OutputLootRealNames { get; } = new()
    {
        { LootType.Weapon, "Weapon" },
        { LootType.Armor, "Protector" },
        { LootType.Talisman, "Accessory" }
    };

    public void ExportLootDetails(GenericParam massEditDict, LootType lootType, string title = "", string description = "", string summary = "", List<string> extraFilters = null, List<string> extraBannedValues = null)
    {
        string finalMassEditOutput = this.CreateMassEditParamFromParamDictionary(massEditDict, this.OutputParamName, massEditDict.ID, extraFilters, extraBannedValues, this.ParamMandatoryKeys);

        this.GeneratedDataRepository.AddParamEdit(
            this.OutputParamName,
            ParamOperation.Create,
            finalMassEditOutput,
            this.CreateFmgLootEntrySet(this.OutputLootRealNames[lootType], title, description, summary),
            massEditDict);
    }

    public IEnumerable<SpEffectText> ApplySpEffects(
        int rarityId,
        List<int> allowedSpefTypes,
        GenericParam outputDictionary,
        float spefChanceMult = 1.0f,
        bool armorTalisman = false,
        int spefNumOverride = -1,
        bool overwriteExistingSpEffects = false)
    {
        List<SpEffectText> spEffectTexts = [];

        List<string> speffectParam = !overwriteExistingSpEffects ? this.GetAvailableSpEffectSlots(outputDictionary) : this.GetPassiveSpEffectSlotArrayFromOutputParamName();
        if (speffectParam.Count == 0)
        {
            Log.Logger.Warning($"{outputDictionary.ID} has no available spEffect slots, not applying any");
            return [];
        }

        int finalNumber = spefNumOverride < 0 && spefNumOverride <= speffectParam.Count ? speffectParam.Count : spefNumOverride;
        List<SpEffectText> speffectsToApply = this.SpEffectHandler.GetSpEffects(finalNumber, allowedSpefTypes, rarityId, armorTalisman, spefChanceMult);

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

    public string CreateLootTitle(string originalTitle, int rarityId, string damageType, IEnumerable<SpEffectText>? namePartsCollection, bool colorCoded = true, bool includeSuffix = false)
    {
        List<string> additions =
        [
            this.RarityHandler.GetRarityName(rarityId, colorCoded),
            damageType,
            namePartsCollection?.Where(d => d?.NameParts?.Prefix != null).FirstOrDefault()?.NameParts?.Prefix ?? string.Empty,
            //nameParts?.NameParts.Interfix ?? string.Empty,
            originalTitle,
            includeSuffix ? namePartsCollection?.Where(d => d?.NameParts?.Suffix != null).FirstOrDefault()?.NameParts?.Suffix ?? string.Empty : string.Empty
        ];

        return string.Join(" ", additions.Where(d => !string.IsNullOrWhiteSpace(d)).Distinct());
    }

    public TParamType GetNewLootItem(int baseId = -1)
    {
        if (this.Configuration.Settings.ItemLotGeneratorSettings.ChaosLootEnabled)
        {
            return this.DataSource.GetRandomItem();
        }

        if (baseId == -1 && this.PriorityIDs_Current.Count > 0)
        {
            baseId = this.ChoosePriorityIdAtRandom();
        }

        return this.DataSource.GetItemById(baseId);
    }

    public string CreateAffinityTitle(WeaponModifications modifications)
    {
        List<string> names =
        [
            modifications.PrimaryDamageType.PriName,
            modifications.SecondaryDamageType?.SecName
        ];

        if (string.IsNullOrEmpty(names[0]))
        {
            names[0] = modifications.PrimaryDamageType.SecName != modifications.SecondaryDamageType?.SecName
                        ? modifications.PrimaryDamageType.SecName
                        : string.Empty;
        }

        if (modifications.PrimaryDamageType.SecName == modifications.SecondaryDamageType?.SecName)
        {
            names[1] = string.Empty;
        }

        return string.Join(' ', names.Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    public int ChoosePriorityIdAtRandom()
    {
        int chosenIndex = this.Random.NextInt(0, this.PriorityIDs_Current.Count - 1);
        int chosenId = this.PriorityIDs_Current[chosenIndex];
        if (chosenId != -1)
        {
            Log.Logger.Debug($"PRIORITY ID {chosenId} SELECTED! REMAINING PRIORITY IDS: {string.Join(", ", this.PriorityIDs_Current)}");
        }
        this.PriorityIDs_Current.RemoveAt(chosenIndex);
        return chosenId;
    }

    public List<string> GetPassiveSpEffectSlotArrayFromOutputParamName()
    {
        return this.Configuration.LootParam.Speffects.GetType().GetProperty(this.OutputParamName.ToString()).GetValue(this.Configuration.LootParam.Speffects) as List<string>;
    }

    public List<string> GetAvailableSpEffectSlots(GenericParam itemParam)
    {
        List<string> baseParams = this.GetPassiveSpEffectSlotArrayFromOutputParamName();
        List<string> finalArray = [];
        foreach (string param in baseParams)
        {
            if (itemParam.ContainsKey(param) && itemParam.GetValue<int>(param) <= 0)
            {
                finalArray.Add(param);
            }
        }
        return finalArray;
    }

    public bool HasLootTemplates()
    {
        return this.DataSource.Count() > 0;
    }
}
