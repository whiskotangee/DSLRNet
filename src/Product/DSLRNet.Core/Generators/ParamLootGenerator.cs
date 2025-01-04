namespace DSLRNet.Core.Generators;

using DSLRNet.Core.DAL;

public class ParamLootGenerator<TParamType>(
    RarityHandler rarityHandler,
    SpEffectHandler spEffectHandler,
    LoreGenerator loreGenerator,
    RandomProvider random,
    IOptions<Configuration> configuration,
    IOptions<Settings> settings,
    ParamEditsRepository dataRepository,
    ParamNames outputParamName,
    ILogger<ParamLootGenerator<TParamType>> logger) : BaseHandler(dataRepository)
    where TParamType : class, ICloneable<TParamType>
{
    public RarityHandler RarityHandler { get; set; } = rarityHandler;

    public SpEffectHandler SpEffectHandler { get; set; } = spEffectHandler;

    public LoreGenerator LoreGenerator { get; set; } = loreGenerator;

    public RandomProvider Random { get; set; } = random;

    public Configuration Configuration { get; set; } = configuration.Value;

    public Settings Settings { get; set; } = settings.Value;

    public required IDGenerator IDGenerator { get; set; }

    public required IDataSource<TParamType> DataSource { get; set; }

    public ParamNames OutputParamName { get; } = outputParamName;

    public Dictionary<LootType, string> OutputLootRealNames { get; } = new()
    {
        { LootType.Weapon, "Weapon" },
        { LootType.Armor, "Protector" },
        { LootType.Talisman, "Accessory" }
    };

    public IEnumerable<SpEffectDetails> ApplySpEffects(
        int rarityId,
        List<int> allowedSpefTypes,
        GenericParam lootItem,
        float spefChanceMult,
        LootType lootType,
        int spefNumOverride = -1,
        bool overwriteExistingSpEffects = false)
    {
        List<SpEffectDetails> spEffectTexts = [];

        List<string> speffectParam = !overwriteExistingSpEffects ? this.GetAvailablePassiveSpEffectSlots(lootItem) : this.GetPassiveSpEffectFieldNames();
        if (speffectParam.Count == 0)
        {
            logger.LogInformation($"New item {lootItem.ID} of type {lootType} has no available spEffect slots, not applying any");
            return [];
        }

        int finalNumber = spefNumOverride < 0 && spefNumOverride <= speffectParam.Count ? speffectParam.Count : spefNumOverride;
        List<SpEffectDetails> speffectsToApply = this.SpEffectHandler.GetSpEffects(finalNumber, allowedSpefTypes, rarityId, spefChanceMult);

        if (speffectsToApply.Count != 0)
        {
            for (int x = 0; x < speffectsToApply.Count; x++)
            {
                lootItem.SetValue(speffectParam[x], speffectsToApply[x].ID);

                spEffectTexts.Add(speffectsToApply[x]);
            }
        }
        else if (rarityId > 7)
        {
            logger.LogWarning($"New Item {lootItem.ID} of type {lootType} Failed to apply any SpEffects when rarity > 7");
        }

        return spEffectTexts;
    }

    public string CreateLootTitle(string originalTitle, int rarityId, string damageType, IEnumerable<SpEffectDetails>? namePartsCollection, bool colorCoded = true, bool includeSuffix = false)
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

    public List<string> GetPassiveSpEffectFieldNames()
    {
        return this.Configuration.LootParam.Speffects.GetType().GetProperty(this.OutputParamName.ToString())?.GetValue(this.Configuration.LootParam.Speffects) as List<string>
            ?? throw new Exception($"Could not get spEffect property names for {this.OutputParamName} param");
    }

    public List<string> GetAvailablePassiveSpEffectSlots(GenericParam itemParam)
    {
        List<string> baseParams = this.GetPassiveSpEffectFieldNames();
        List<string> finalArray = [];
        foreach (string param in baseParams)
        {
            if (itemParam.GetValue<int>(param) <= 0)
            {
                finalArray.Add(param);
            }
        }
        return finalArray;
    }

    public bool IsLoaded()
    {
        return this.DataSource.Count() > 0;
    }
}
