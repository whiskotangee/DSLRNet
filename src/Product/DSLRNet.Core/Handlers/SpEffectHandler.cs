namespace DSLRNet.Core.Handlers;

using DSLRNet.Core.DAL;
using System.Diagnostics;

public class SpEffectHandler : BaseHandler
{
    private readonly Configuration configuration;
    private readonly RarityHandler rarityHandler;

    private readonly RandomProvider randomNumberGetter;

    public IEnumerable<SpEffectConfig> LoadedSpEffectConfigs { get; set; }

    public SpEffectHandler(
        ILogger<SpEffectHandler> logger,
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

        IEnumerable<SpEffectParamNew> spEffectsToAdd = dataAccess.SpEffectParamNew.GetAll().ToList();

        foreach (SpEffectParamNew? spEffectParam in spEffectsToAdd)
        {
            this.GeneratedDataRepository.AddParamEdit(
                new ParamEdit
                {
                    ParamName = ParamNames.SpEffectParam,
                    Operation = ParamOperation.Create,
                    ParamObject = spEffectParam.GenericParam
                });
        }
    }

    public List<SpEffectDetails> GetSpEffects(int desiredCount, List<int> allowedtypes, int rarityId, float chanceMultiplier = 1.0f)
    {
        List<bool> spEffectApplyResults = this.rarityHandler.GetRarityEffectChances(rarityId, chanceMultiplier);

        IntValueRange powerRange = this.rarityHandler.GetSpeffectPowerRange(rarityId);

        desiredCount = Math.Clamp(desiredCount, 0, 3);

        List<SpEffectDetails> effects = [];

        if (desiredCount > 0)
        {
            List<SpEffectConfig> spEffectChoices = this.GetAvailableSpEffectConfigs(powerRange, allowedtypes).ToList();
            if (spEffectChoices.Count == 0)
            {
                return effects;
            }

            for(int i = 0; i < desiredCount; i++)
            {
                if (spEffectApplyResults[i])
                {
                    SpEffectConfig newSpEffect = this.randomNumberGetter.GetRandomItem(spEffectChoices);

                    string description = this.GetSpeffectDescription(
                        newSpEffect.Description,
                        newSpEffect.Value.ToString(),
                        newSpEffect.Stacks == 1);

                    string summary = this.GetSpeffectDescription(
                        newSpEffect.ShortDescription == ""
                            ? newSpEffect.Description
                            : newSpEffect.ShortDescription,
                        newSpEffect.Value.ToString(),
                        newSpEffect.Stacks == 1,
                        true);

                    effects.Add(new SpEffectDetails
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

    public string GetSpeffectDescription(string description, string value, bool stacks = false, bool includeEffectText = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string stacking = !stacks ? this.configuration.DSLRDescText.NoStacking : string.Empty;
        string effectText = !includeEffectText ? this.configuration.DSLRDescText.Effect : string.Empty;
        string replacedValue = description.Replace("{VALUE}", value);

        return effectText + replacedValue + stacking;
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
