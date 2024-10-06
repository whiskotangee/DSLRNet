using DSLRNet.Config;
using DSLRNet.Contracts;
using DSLRNet.Data;
using Microsoft.Extensions.Options;
using Mods.Common;
using Serilog;
using System.Reflection.Metadata.Ecma335;

namespace DSLRNet.Handlers;

public class SpEffectHandler : BaseHandler
{
    private readonly Configuration configuration;
    private RarityHandler rarityHandler;

    private RandomNumberGetter randomNumberGetter;

    //ARRAY TO STORE ALL SPEFFECTPARAM ENTRIES IN MASSEDIT FORM TO BE INCLUDED IN EACH SEED
    public List<string> SpEffects = [];

    enum SpEffectType { Normal, OnHit, StaffOnly, SealOnly }

    //SPEFFECTCONFIGS ARE WHAT DSLR ACTUALLY USES TO ASSIGN SPEFFECTS, PROVIDING SPEFFECTIDS (CUSTOM ONES NEED TO BE IN A
    //SPEFFECT PARAM CSV THAT WILL BE CONVERTED TO MASSEDIT AS ABOVE), POWER RATINGS, DESCRIPTIONS, VALUES ETC. 
    public List<SpEffectConfig_Default> LoadedSpEffectConfigs;

    public HashSet<string> SpEffectConfigWordFilter = ["SpEffectConfig", "speffectconfig", "Speffectconfig", "speffectConfig", "SpEffectSetup", "speffectSetup", "speffectsetup"];

    public SpEffectHandler(IOptions<Configuration> configuration, RarityHandler rarityHandler, RandomNumberGetter random, DataRepository dataRepository) : base(dataRepository)
    {
        this.configuration = configuration.Value;
        this.rarityHandler = rarityHandler;
        randomNumberGetter = random;

        this.LoadedSpEffectConfigs = Data.Csv.LoadCsv<SpEffectConfig_Default>("DefaultData\\ER\\CSVs\\SpEffectConfig_Default.csv");

        List<GenericDictionary> loadedSpEffectParams = Data.Csv.LoadCsv<SpEffectParam>("DefaultData\\ER\\CSVs\\SpEffectParam.csv")
            .Select(GenericDictionary.FromObject)
            .ToList();

        foreach (GenericDictionary? spEffectParam in loadedSpEffectParams)
        {
            this.GeneratedDataRepository.AddParamEdit(
                "SpEffectParam", 
                ParamOperation.MassEdit, 
                this.CreateMassEditParamFromParamDictionary(
                    spEffectParam,
                    "SpEffectParam",
                    spEffectParam.GetValue<int>("ID"),
                    [],
                    ["0", "-1"],
                    ["conditionHp", "effectEndurance", "conditionHpRate"]),
                [],
                spEffectParam);
        }
    }

    //SPEFFECTS WILL BE FILLED WITH MULTIPLE NUMERICAL KEYS DENOTING THE TYPE OF EFFECT - GENERAL WILL BE 0, BEHAVIOUR 1, STAFF ONLY 2, SEAL ONLY 3 ETC.

    public List<SpEffectText> GetSpEffects(int desiredCount, List<int> allowedtypes, int rarityid, bool armortalisman = false, float chancemult = 1.0f)
    {
        //PREPARE SPEFFECT CHANCE ARRAY BY GRABBING THE SPECIAL EFFECT CHANCES FROM CHOSEN RARITY
        int finalrarity = rarityHandler.GetNearestRarityId(rarityid);
        List<bool> chancearray = this.rarityHandler.GetRarityEffectChanceArray(finalrarity, armortalisman, chancemult);
        //SPEFFECT POWER RANGE AS DETERMINED BY RARITY
        List<int> speffectpowerrange = rarityHandler.GetRaritySpeffectPowerArray(rarityid);
        //CLAMP HOWMANY TO VALID VALUES
        desiredCount = Math.Clamp(desiredCount, 0, 4);

        //SETUP DICTIONARY TO RETURN - ARRAY OF IDS, AND A PRECOMPILED DESCRIPTION AND SUMMARY FOR THE SET
        List<SpEffectText> effects = [];

        //ITERATE OVER EACH ENTRY IN CHANCEARRAY [howmany] TIMES, FOR EACH SUCCESSFUL ONE CHOOSE A SPEFFECT FROM THE AVAILABLE POWERS
        if (desiredCount > 0)
        {
            //STORE AN ARRAY OF POSSIBLE CHOICES BASED ON AVAILABLE SPEFF POWER RANGE
            List<SpEffectConfig_Default> spEffectChoices = this.GetAvailableSpEffectConfigs(speffectpowerrange[0], speffectpowerrange[1], allowedtypes).ToList();
            if (spEffectChoices.Count > 0)
            {
                foreach (int x in Enumerable.Range(0, desiredCount))
                {
                    if (chancearray[x])
                    {
                        SpEffectConfig_Default newSpEffect = spEffectChoices[this.randomNumberGetter.NextInt(0, spEffectChoices.Count - 1)];

                        string newdescription = GetSpeffectDescriptionWithValue(
                            newSpEffect.Description, 
                            newSpEffect.SpEffectPower.ToString(), 
                            newSpEffect.Stacks == 1);

                        string newsummary = GetSpeffectDescriptionWithValue(
                            newSpEffect.ShortDescription == "" 
                                ? newSpEffect.Description 
                                : newSpEffect.ShortDescription, 
                            newSpEffect.SpEffectPower.ToString(), 
                            newSpEffect.Stacks == 1, 
                            true, 
                            "");

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

    public string GetSpeffectDescriptionWithValue(string description, string value, bool stacks = false, bool noeffecttext = false, string newline = "\\n")
    {
        string stacking = !stacks ? this.configuration.DSLRDescText.NoStacking : string.Empty;
        string effecttext = !noeffecttext ? this.configuration.DSLRDescText.Effect : string.Empty;
        string returnstring = description.Replace("{VALUE}", value);

        return effecttext + returnstring + stacking + newline;
    }

    public List<int> GetPossibleWeaponSpeffectTypes(GenericDictionary weapdict, bool allowstandardspeffects = true)
    {
        List<int> speffecttypes = [];
        WeaponsCanCastParamConfig weaponsCanCastConfig = this.configuration.LootParam.WeaponsCanCastParam;
        List<string> speffectparams = [weaponsCanCastConfig.Sorcery, weaponsCanCastConfig.Miracles];

        List<int> speffectvalues = [3, 2];

        // #ITERATE OVER OUR PARAMS, APPENDING THE RESPECTIVE VALUE IF WEAPDICT HAS THAT PARAM AND IT'S SET TO 1, I.E. ENABLED
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

        //ADD STANDARD SPEFFECTS [0] IF ALLOWSTANDARDSPEFFECTS
        if (allowstandardspeffects)
        {
            speffecttypes.Add(0);
        }

        return speffecttypes;
    }

    public IEnumerable<SpEffectConfig_Default> GetAvailableSpEffectConfigs(int powermin, int powermax, List<int> allowedtypes)
    {
        //GET ALL SPEFFECT IDS WITHIN EACH SPEFFECT POWER AVAILABLE IN RANGE, ADDING THOSE CLOSER TO MAX TWICE TO MAKE
        //THEM MORE LIKELY
        List<SpEffectConfig_Default> spEffects = [];

        //CREATE A THRESHOLD VALUE HALFWAY BETWEEN PMIN AND PMAX SO WE KNOW WHICH POWER TO PRIORITISE
        //CLAMP VALUES
        powermax = Math.Clamp(powermax, 0, 9999);
        powermin = Math.Clamp(powermin, 0, powermax);
        int threshold = (int)((powermin + powermax) * 0.5);

        int superthreshold = Math.Clamp((int)(powermax * 0.9), 0, 9999);

        if (this.LoadedSpEffectConfigs.Count == 0)
        {
            return [];
        }

        //ITERATE OVER EACH ALLOWEDTYPE
        foreach (int x in allowedtypes)
        {
            // try with effects in range
            List<SpEffectConfig_Default> allOptions = LoadedSpEffectConfigs
                .Where(d => d.SpEffectType == x)
                .ToList();

            IEnumerable<SpEffectConfig_Default> filteredOptions = allOptions
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
