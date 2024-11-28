using DSLRNet.Config;
using DSLRNet.Data;
using Mods.Common;
using Newtonsoft.Json;
using Serilog;

namespace DSLRNet.Handlers;

public class RarityHandler : BaseHandler
{
    private readonly RandomNumberGetter randomNumberGetter;
    private readonly RarityIconMappingConfig iconMappingConfig;

    //DICTIONARY TO HOLD ALL OF THE RARITY CONFIGURATIONS AVAILABLE WHEN MODSREADY SIGNAL IS RECEIVED - KEY IS THE RARITY ID
    private Dictionary<int,RaritySetup> RarityConfigs = [];

    public RarityHandler(RandomNumberGetter randomNumberGetter, DataRepository dataRepository) : base(dataRepository)
    {
        this.randomNumberGetter = randomNumberGetter;

        this.iconMappingConfig = JsonConvert.DeserializeObject<RarityIconMappingConfig>(File.ReadAllText("DefaultData\\ER\\iconmappings.json"));

        this.RarityConfigs = Csv.LoadCsv<RaritySetup>("DefaultData\\ER\\CSVs\\RaritySetup.csv").ToDictionary(d => d.ID);
    }

    public int ChooseRarityFromIdSetWithBuiltInWeights(List<int> idset, float highmult)
    {
        List<int> valididset = [];
        List<int> valididweights = [];

        //FIRST, FIND THE NEAREST AVAILABLE ID FOR EACH ID IN THE SET
        foreach (int x in idset)
        {
            valididset.Add(GetNearestRarityId(x));
        }

        //NOW GET THE WEIGHTS FOR EACH OF THESE VALID RARITY IDS
        foreach (int x in idset)
        {
            //#print_debug(RarityConfigs)
            valididweights.Add(this.RarityConfigs[valididset.First(d => d == x)].SelectionWeight);
            //#print_debug(GD.Str(valididset)+" "+str(valididweights))
        }

        //NOW USE OUR WEIGHTED RNG FUNCTION TO CHOOSE ONE FROM THOSE WEIGHTS AND RARITIES
        int finalid = (int)this.randomNumberGetter.NextWeightedValue(valididset, valididweights, highmult);

        //#print_debug("Final selected rarity from ID set: "+str(finalid))
        return finalid;

    }

    public Queue<bool> GetRarityEffectChanceArray(int desiredCount, int rarityid = 0, bool armortalisman = false, float chancemult = 1.0f)
    {
        Queue<bool> finalboolarray = [];
        //FIRST, GET THE CLOSEST RARITY TO THE ID SPECIFIED
        int finalrarityid = GetNearestRarityId(rarityid);
        //NOW GENERATE FOUR RANDOM BOOL RESULTS (BECAUSE THE MAX SPEFFECTS POSSIBLE TO ASSIGN TO EQUIPMENT IS TALISMANS WITH FOUR)
        //BASED ON THE RARITY CONFIGURATION'S FOUR "SpEffect(x)Chance" VALUES
        //IF ARMORTALISMAN IS TRUE, THE FIRST SPEFFECT IS GUARANTEED, SO WE MANDATE THE FIRST ELEMENT IN THE ARRAY IS
        //1.0, THEN SHIFT THE ITERATION OFFSET BY 1 SO SPEFFECT0CHANCE BECOMES THE CHANCE FOR A SECOND SPEFFECT
        int offset = 0;

        if (armortalisman || rarityid > 5)
        {
            finalboolarray.Enqueue(true);
            offset = 1;
        }

        for(int i = offset; i < desiredCount; i++)
        {
            String spefchance = $"SpEffect{i}Chance";
            RaritySetup item = RarityConfigs[finalrarityid];

            float speffectchance = (float)item.GetType().GetProperty(spefchance).GetValue(item);
            finalboolarray.Enqueue(this.randomNumberGetter.GetRandomBoolByPercent(speffectchance));
        }

        return finalboolarray;
    }

    public List<int> GetRarityDamageAdditionRange(int rarityid = 0)
    {
        int finalrarity = GetNearestRarityId(rarityid);
        return [RarityConfigs[finalrarity].WeaponDmgAddMin, RarityConfigs[finalrarity].WeaponDmgAddMax];
    }

    public List<float> GetRarityArmorCutRateRange(int rarityid = 0)
    {
        int finalrarity = GetNearestRarityId(rarityid);
        return [RarityConfigs[finalrarity].ArmorCutRateAddMin, RarityConfigs[finalrarity].ArmorCutRateAddMax];
    }

    public float GetRarityArmorCutRateAddition(int rarityid = 0)
    {
        int finalrarity = GetNearestRarityId(rarityid);
        List<float> range = GetRarityArmorCutRateRange(finalrarity);
        return (float)Math.Round(this.randomNumberGetter.NextDouble(range[0], range[1]), 4);
    }

    public List<int> GetRaritiesWithinRange(int highest = 10, int lowerrange = 0)
    {
        //CATCH HIGHEST BEING EMPTY OR -1
        if (highest == -1)
        {
            //IF WE HAVE NO RARITIES WE'D PROBABLY HAVE CRASHED BY NOW, BUT RETURN AN EMPTY ARRAY
            if (this.RarityConfigs.Count != 0)
            {
                return [0];
            }

            highest = GetNearestRarityId((int)Math.Round(RarityConfigs.Keys.Max() * 0.5));
            Log.Logger.Debug("RARITIES WARNING WITHIN RANGE REQUEST WITH HIGHEST OF -1! SOMETHING MAY BE WRONG!");
        }

        highest = GetNearestRarityId(highest);
        //CLAMP LOWEST VALUE TO MINIMUM AND MAXIMUM AVAILABLE RARITYCONFIG
        int lowest = Math.Clamp(GetNearestRarityId((int)Math.Round((highest - lowerrange) / 2f)), GetLowestRarityId(), GetHighestRarityId());
        List<int> finalrarities = [];
        //ADD VALUES NEAREST TO HIGHEST AND HIGHEST-LOWERRANGE
        foreach (int x in new int[] { highest, lowest })
        {
            finalrarities.Add(x);
        }

        //NOW ITERATE OVER ALL AVAILABLE RARITYIDS AND Add ANY HIGHER THAN LOWEST AND LESS THAN HIGHEST
        foreach (int x in RarityConfigs.Keys)
        {
            if (x > lowest && x < highest)
            {
                finalrarities.Add(x);                
            }
        }

        return finalrarities;
    }

    public float[] GetRarityArmorResistMultRange(int rarityid = 0)
    {
        int finalrarity = GetNearestRarityId(rarityid);
        return [RarityConfigs[finalrarity].ArmorResistMinMult, RarityConfigs[finalrarity].ArmorResistMaxMult];
    }

    public float GetRarityArmorresistmultMultiplier(int rarityid = 0)
    {
        int finalrarity = GetNearestRarityId(rarityid);
        float[] range = GetRarityArmorResistMultRange(finalrarity);

        return (float)this.randomNumberGetter.NextDouble(range[0], range[1]);
    }

    public int GetRarityParamInt(int rarityid = 0)
    {
        return RarityConfigs[GetNearestRarityId(rarityid)].RarityParamValue;
    }

    public List<int> GetRaritySpeffectPowerArray(int rarityid = 0)
    {
        int finalrarity = GetNearestRarityId(rarityid);
        //NOW WE HAVE A LESS RIGID WAY OF GETTING SPEFFECTS, SLIGHTLY WIDEN THE RANGE 
        return [Math.Clamp(RarityConfigs[finalrarity].SpEffectPowerMin - 10, 0, RarityConfigs[finalrarity].SpEffectPowerMax), RarityConfigs[finalrarity].SpEffectPowerMax];
    }

    public String GetRarityName(int rarityid = 0)
    {
        int finalrarity = GetNearestRarityId(rarityid);
        return RarityConfigs[finalrarity].Name;
    }

    public int GetRaritySellValue(int rarityid = 0)
    {
        int finalrarity = GetNearestRarityId(rarityid);
        return this.randomNumberGetter.NextInt(RarityConfigs[finalrarity].SellValueMin, RarityConfigs[finalrarity].SellValueMax);
    }

    public string GetColorTextForRarity(int rarityId)
    {
        int matchedRarity = this.GetNearestRarityId(rarityId);
        string name = this.GetRarityName(matchedRarity);
        return $"<font color=\"#{this.RarityConfigs[matchedRarity].ColorHex}\">{name}</font>";
    }

    public List<float> GetRarityWeightMultipliers(int rarityid = 0)
    {
        int finalrarity = GetNearestRarityId(rarityid);
        return [RarityConfigs[finalrarity].WeightMultMin, RarityConfigs[finalrarity].WeightMultMax];
    }

    public int GetLowestRarityId()
    {
        return RarityConfigs.Keys.Min();
    }

    public int GetNearestRarityId(int desiredrarityvalue = 0)
    {
        //IF WE ARE IN RARITY CHAOS MODE OR WE GET A NULL RARITY VALUE OF -1, RETURN A RANDOM RARITY ID
        if (desiredrarityvalue == -1)
        {
            Log.Logger.Debug("BEWARE - NULL DESIRED RARITY DETECTED!");
            return RarityConfigs.Keys.OrderBy(d => this.randomNumberGetter.NextInt(1, 1000)).First();
        }

        //IF DSL ASKS FOR A RARITY THAT DOESN'T EXIST IN OUR CURRENT SETUP,
        //GRAB THE NEAREST ONE INSTEADg

        int finalrarityid = desiredrarityvalue;
        //IF RARITYIDS ALREADY HAS THE DRV, SKIP ITERATING TO FIND IT
        if (!RarityConfigs.ContainsKey(desiredrarityvalue))
        {
            foreach (int x in RarityConfigs.Keys)
            {
                if (desiredrarityvalue <= x)
                {
                    finalrarityid = x;
                }
            }
        }
        return finalrarityid;
    }

    public int GetHighestRarityId()
    {
        return this.RarityConfigs.Keys.Max();
    }

    public int GetIconIdForRarity(int iconId, int rarityId, bool isUnique = false)
    {
        if (rarityId == 0)
        {
            return iconId;
        }

        if (isUnique)
        {
            rarityId = -1;
        }

        var options = this.iconMappingConfig.IconSheets.Where(d => d.IconMappings.RarityIds.Contains(rarityId)).Select(s => s.IconMappings)
            .Where(d => d.IconReplacements.Any(d => d.OriginalIconId == iconId)).FirstOrDefault();

        return options?.IconReplacements.FirstOrDefault(s => s.OriginalIconId == iconId)?.NewIconId ?? iconId;
    }
}
