namespace DSLRNet.Core.Handlers;

using DSLRNet.Core.DAL;

public class SpEffectHandler : BaseHandler
{
    private readonly Configuration configuration;
    private readonly RarityHandler rarityHandler;

    private readonly RandomProvider randomNumberGetter;

    public IEnumerable<SpEffectConfig> LoadedSpEffectConfigs { get; set; }

    public SpEffectHandler(
        IOptions<Configuration> configuration,
        RarityHandler rarityHandler,
        RandomProvider random,
        ParamEditsRepository dataRepository,
        DataAccess dataAccess) : base(dataRepository)
    {
        this.configuration = configuration.Value;
        this.rarityHandler = rarityHandler;
        this.randomNumberGetter = random;

        this.LoadedSpEffectConfigs = dataAccess.SpEffectConfig.GetAll();

        IEnumerable<SpEffectParamNew> loadedSpEffectParams = dataAccess.SpEffectParamNew.GetAll().ToList();

        foreach (SpEffectParamNew? spEffectParam in loadedSpEffectParams)
        {
            this.GeneratedDataRepository.AddParamEdit(
                new ParamEdit
                {
                    ParamName = ParamNames.SpEffectParam,
                    Operation = ParamOperation.MassEdit,
                    MassEditString = this.CreateMassEdit(
                        spEffectParam.GenericParam,
                        ParamNames.SpEffectParam,
                        spEffectParam.ID,
                        bannedEquals: ["0", "-1"],
                        mandatoryKeys: ["conditionHp", "effectEndurance", "conditionHpRate"]),
                    MessageText = null,
                    ParamObject = spEffectParam.GenericParam
                });
        }
    }

    public List<SpEffectText> GetSpEffects(int desiredCount, List<int> allowedtypes, int rarityid, LootType lootType, float chanceMultiplier = 1.0f)
    {
        int finalrarity = this.rarityHandler.GetNearestRarityId(rarityid);
        Queue<bool> chanceQueue = this.rarityHandler.GetRarityEffectChances(desiredCount, finalrarity, lootType, chanceMultiplier);

        IntValueRange powerRange = this.rarityHandler.GetSpeffectPowerArray(rarityid);

        desiredCount = Math.Clamp(desiredCount, 0, 4);

        List<SpEffectText> effects = [];

        if (desiredCount > 0)
        {
            List<SpEffectConfig> spEffectChoices = this.GetAvailableSpEffectConfigs(powerRange, allowedtypes).ToList();
            if (spEffectChoices.Count == 0)
            {
                return effects;
            }
            while (chanceQueue.TryDequeue(out bool result))
            {
                if (result)
                {
                    SpEffectConfig newSpEffect = this.randomNumberGetter.GetRandomItem(spEffectChoices);

                    string description = this.GetSpeffectDescriptionWithValue(
                        newSpEffect.Description,
                        newSpEffect.Value.ToString(),
                        newSpEffect.Stacks == 1);

                    string summary = this.GetSpeffectDescriptionWithValue(
                        newSpEffect.ShortDescription == ""
                            ? newSpEffect.Description
                            : newSpEffect.ShortDescription,
                        newSpEffect.Value.ToString(),
                        newSpEffect.Stacks == 1,
                        true);

                    effects.Add(new SpEffectText
                    {
                        ID = newSpEffect.ID,
                        Description = description,
                        Summary = summary,
                        NameParts = new NameParts
                        {
                            Suffix = newSpEffect.Suffix,
                            Prefix = newSpEffect.Prefix,
                            Interfix = newSpEffect.Interfix,
                        }
                    });
                }
            }
        }
        return effects;
    }

    public string GetSpeffectDescriptionWithValue(string description, string value, bool stacks = false, bool noeffecttext = false)
    {
        string stacking = !stacks ? this.configuration.DSLRDescText.NoStacking : string.Empty;
        string effecttext = !noeffecttext ? this.configuration.DSLRDescText.Effect : string.Empty;
        string returnstring = description.Replace("{VALUE}", value);

        return effecttext + returnstring + stacking;
    }

    public List<int> GetPossibleWeaponSpeffectTypes(EquipParamWeapon weapon, bool allowstandardspeffects = true)
    {
        List<int> speffecttypes = [];
        List<int> speffectvalues = [3, 2];

        if (weapon.enableSorcery == 1)
        {
            speffecttypes.Add(3);
        }

        if (weapon.enableMiracle == 1)
        {
            speffectvalues.Add(2);
        }

        if (allowstandardspeffects)
        {
            speffecttypes.Add(0);
        }

        return speffecttypes;
    }

    private IEnumerable<SpEffectConfig> GetAvailableSpEffectConfigs(IntValueRange range, List<int> allowedTypes)
    {
        List<SpEffectConfig> spEffects = [];

        int threshold = (int)((range.Min + range.Max) * 0.5);

        int upperThreshold = Math.Clamp((int)(range.Max * 0.9), 0, 9999);

        foreach (int x in allowedTypes)
        {
            List<SpEffectConfig> allOptions = this.LoadedSpEffectConfigs
                .Where(d => d.SpEffectType == x)
                .ToList();

            IEnumerable<SpEffectConfig> filteredOptions = allOptions
                .Where(d => d.SpEffectPower >= range.Min && d.SpEffectPower <= range.Max);

            if (filteredOptions.Any())
            {
                spEffects.AddRange(filteredOptions);
                // add within threshold twice to make it more likely
                spEffects.AddRange(filteredOptions.Where(d => d.SpEffectPower >= threshold && d.SpEffectPower <= upperThreshold).ToList());
            }
            else
            {
                // add fallbacks if they exist
                spEffects.AddRange(allOptions.Where(d => d.SpEffectPower < range.Min));
            }
        }

        return spEffects;
    }
}
