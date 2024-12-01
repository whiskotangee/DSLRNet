namespace DSLRNet.Core.Handlers;

public class SpEffectHandler : BaseHandler
{
    private readonly Configuration configuration;
    private RarityHandler rarityHandler;

    private RandomNumberGetter randomNumberGetter;

    public IEnumerable<SpEffectConfig> LoadedSpEffectConfigs { get; set; }

    public SpEffectHandler(
        IOptions<Configuration> configuration, 
        RarityHandler rarityHandler, 
        RandomNumberGetter random, 
        ParamEditsRepository dataRepository,
        IDataSource<SpEffectConfig> spEffectConfigDataSource,
        IDataSource<SpEffectParam> spEffectDataSource) : base(dataRepository)
    {
        this.configuration = configuration.Value;
        this.rarityHandler = rarityHandler;
        randomNumberGetter = random;

        LoadedSpEffectConfigs = spEffectConfigDataSource.GetAll();

        IEnumerable<SpEffectParam> loadedSpEffectParams = spEffectDataSource.GetAll().ToList();

        foreach (SpEffectParam? spEffectParam in loadedSpEffectParams)
        {
            GeneratedDataRepository.AddParamEdit(
                ParamNames.SpEffectParam,
                ParamOperation.MassEdit,
                CreateMassEditParamFromParamDictionary(
                    spEffectParam.GenericParam,
                    ParamNames.SpEffectParam,
                    spEffectParam.ID,
                    [],
                    ["0", "-1"],
                    ["conditionHp", "effectEndurance", "conditionHpRate"]),
                null,
                spEffectParam.GenericParam);
        }
    }

    public List<SpEffectText> GetSpEffects(int desiredCount, List<int> allowedtypes, int rarityid, bool armortalisman = false, float chancemult = 1.0f)
    {
        int finalrarity = rarityHandler.GetNearestRarityId(rarityid);
        Queue<bool> chancearray = rarityHandler.GetRarityEffectChanceArray(desiredCount, finalrarity, armortalisman);

        List<int> speffectpowerrange = rarityHandler.GetRaritySpeffectPowerArray(rarityid);

        desiredCount = Math.Clamp(desiredCount, 0, 4);

        List<SpEffectText> effects = [];

        if (desiredCount > 0)
        {
            List<SpEffectConfig> spEffectChoices = GetAvailableSpEffectConfigs(speffectpowerrange[0], speffectpowerrange[1], allowedtypes).ToList();
            if (spEffectChoices.Count > 0)
            {
                while (chancearray.TryDequeue(out bool result))
                {
                    if (result)
                    {
                        SpEffectConfig newSpEffect = randomNumberGetter.GetRandomItem(spEffectChoices);

                        string newdescription = GetSpeffectDescriptionWithValue(
                            newSpEffect.Description,
                            newSpEffect.Value.ToString(),
                            newSpEffect.Stacks == 1);

                        string newsummary = GetSpeffectDescriptionWithValue(
                            newSpEffect.ShortDescription == ""
                                ? newSpEffect.Description
                                : newSpEffect.ShortDescription,
                            newSpEffect.Value.ToString(),
                            newSpEffect.Stacks == 1,
                            true);

                        effects.Add(new SpEffectText
                        {
                            ID = newSpEffect.ID,
                            Description = newdescription,
                            Summary = newsummary,
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
        }
        return effects;
    }

    public string GetSpeffectDescriptionWithValue(string description, string value, bool stacks = false, bool noeffecttext = false)
    {
        string stacking = !stacks ? configuration.DSLRDescText.NoStacking : string.Empty;
        string effecttext = !noeffecttext ? configuration.DSLRDescText.Effect : string.Empty;
        string returnstring = description.Replace("{VALUE}", value);

        return effecttext + returnstring + stacking;
    }

    public List<int> GetPossibleWeaponSpeffectTypes(GenericParam weapdict, bool allowstandardspeffects = true)
    {
        List<int> speffecttypes = [];
        WeaponsCanCastParamConfig weaponsCanCastConfig = configuration.LootParam.WeaponsCanCastParam;
        List<string> speffectparams = [weaponsCanCastConfig.Sorcery, weaponsCanCastConfig.Miracles];

        List<int> speffectvalues = [3, 2];

        for (int i = 0; i < speffectparams.Count; i++)
        {
            if (weapdict.ContainsKey(speffectparams[i]))
            {
                if (weapdict.GetValue<int>(speffectparams[i]) == 1)
                {
                    speffecttypes.Add(speffectvalues[i]);
                }
            }
        }

        if (allowstandardspeffects)
        {
            speffecttypes.Add(0);
        }

        return speffecttypes;
    }

    public IEnumerable<SpEffectConfig> GetAvailableSpEffectConfigs(int powermin, int powermax, List<int> allowedtypes)
    {
        List<SpEffectConfig> spEffects = [];

        powermax = Math.Clamp(powermax, 0, 9999);
        powermin = Math.Clamp(powermin, 0, powermax);
        int threshold = (int)((powermin + powermax) * 0.5);

        int superthreshold = Math.Clamp((int)(powermax * 0.9), 0, 9999);

        if (LoadedSpEffectConfigs.Count() == 0)
        {
            return [];
        }

        foreach (int x in allowedtypes)
        {
            List<SpEffectConfig> allOptions = LoadedSpEffectConfigs
                .Where(d => d.SpEffectType == x)
                .ToList();

            IEnumerable<SpEffectConfig> filteredOptions = allOptions
                .Where(d => d.SpEffectPower >= powermin && d.SpEffectPower <= powermax);

            if (filteredOptions.Any())
            {
                spEffects.AddRange(filteredOptions);
                // add within threshold twice to make it more likely
                spEffects.AddRange(filteredOptions.Where(d => d.SpEffectPower >= threshold && d.SpEffectPower <= superthreshold).ToList());
            }
            else
            {
                // add fallbacks if they exist
                spEffects.AddRange(allOptions.Where(d => d.SpEffectPower < powermin));
            }
        }

        return spEffects;
    }
}
