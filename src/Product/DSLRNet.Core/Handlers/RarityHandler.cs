using DSLRNet.Core.Extensions;

namespace DSLRNet.Core.Handlers;

public class RarityHandler : BaseHandler
{
    private readonly RandomProvider randomNumberGetter;
    private readonly RarityIconMappingConfig iconMappingConfig;

    private Dictionary<int, RaritySetup> RarityConfigs = [];

    public RarityHandler(
        RandomProvider randomNumberGetter, 
        ParamEditsRepository dataRepository,
        IDataSource<RaritySetup> raritySetupDataSource) : base(dataRepository)
    {
        this.randomNumberGetter = randomNumberGetter;

        iconMappingConfig = JsonConvert.DeserializeObject<RarityIconMappingConfig>(File.ReadAllText("DefaultData\\ER\\iconmappings.json"));

        RarityConfigs = raritySetupDataSource.GetAll().ToDictionary(d => d.ID);
    }

    public int ChooseRarityFromIdSetWithBuiltInWeights(List<int> idset)
    {
        List<int> valididset = [];
        List<int> valididweights = [];

        foreach (int x in idset)
        {
            valididset.Add(GetNearestRarityId(x));
        }

        foreach (int x in idset)
        {
            valididweights.Add(RarityConfigs[valididset.First(d => d == x)].SelectionWeight);
        }

        int finalid = (int)randomNumberGetter.NextWeightedValue(valididset, valididweights);

        return finalid;
    }

    public Queue<bool> GetRarityEffectChanceArray(int desiredCount, int rarityid, bool armortalisman = false)
    {
        Queue<bool> finalboolarray = [];

        int finalrarityid = GetNearestRarityId(rarityid);
        int offset = 0;

        if (armortalisman || rarityid > 5)
        {
            finalboolarray.Enqueue(true);
            offset = 1;
        }

        for (int i = offset; i < desiredCount; i++)
        {
            string spefchance = $"SpEffect{i}Chance";
            RaritySetup item = RarityConfigs[finalrarityid];

            float speffectchance = (float)item.GetType().GetProperty(spefchance).GetValue(item);
            finalboolarray.Enqueue(randomNumberGetter.GetRandomBoolByPercent(speffectchance));
        }

        return finalboolarray;
    }

    public Range<int> GetRarityDamageAdditionRange(int rarityid)
    {
        int finalrarity = GetNearestRarityId(rarityid);
        return new Range<int>(RarityConfigs[finalrarity].WeaponDmgAddMin, RarityConfigs[finalrarity].WeaponDmgAddMax);
    }

    public Range<float> GetRarityArmorCutRateRange(int rarityid)
    {
        int finalrarity = GetNearestRarityId(rarityid);
        return new Range<float>(RarityConfigs[finalrarity].ArmorCutRateAddMin, RarityConfigs[finalrarity].ArmorCutRateAddMax);
    }

    public Range<float> GetRarityShieldGuardRateRange(int rarityid)
    {
        int finalrarity = GetNearestRarityId(rarityid);
        return new Range<float>(RarityConfigs[finalrarity].ShieldGuardRateMultMin, RarityConfigs[finalrarity].ShieldGuardRateMultMax);
    }

    public float GetRarityArmorCutRateAddition(int rarityid)
    {
        int finalrarity = GetNearestRarityId(rarityid);
        var range = GetRarityArmorCutRateRange(finalrarity);
        return (float)Math.Round(randomNumberGetter.Next(range), 4);
    }

    public List<int> GetRaritiesWithinRange(int highest = 10, int lowerrange = 0)
    {
        if (highest == -1)
        {
            if (RarityConfigs.Count != 0)
            {
                return [0];
            }

            highest = GetNearestRarityId((int)Math.Round(RarityConfigs.Keys.Max() * 0.5));
        }

        highest = GetNearestRarityId(highest);
        
        int lowest = Math.Clamp(GetNearestRarityId((int)Math.Round((highest - lowerrange) / 2f)), GetLowestRarityId(), GetHighestRarityId());
        List<int> finalrarities = [];
        
        foreach (int x in new int[] { highest, lowest })
        {
            finalrarities.Add(x);
        }

        foreach (int x in RarityConfigs.Keys)
        {
            if (x > lowest && x < highest)
            {
                finalrarities.Add(x);
            }
        }

        return finalrarities;
    }

    public Range<float> GetRarityArmorResistMultRange(int rarityid)
    {
        int finalrarity = GetNearestRarityId(rarityid);
        return new Range<float>(RarityConfigs[finalrarity].ArmorResistMinMult, RarityConfigs[finalrarity].ArmorResistMaxMult);
    }

    public float GetRarityArmorResistMultiplier(int rarityid)
    {
        int finalrarity = GetNearestRarityId(rarityid);
        var range = GetRarityArmorResistMultRange(finalrarity);

        return (float)randomNumberGetter.Next(range);
    }

    public int GetRarityParamValue(int rarityid)
    {
        return RarityConfigs[GetNearestRarityId(rarityid)].RarityParamValue;
    }

    public List<int> GetRaritySpeffectPowerArray(int rarityid)
    {
        int finalrarity = GetNearestRarityId(rarityid);

        return [Math.Clamp(RarityConfigs[finalrarity].SpEffectPowerMin - 10, 0, RarityConfigs[finalrarity].SpEffectPowerMax), RarityConfigs[finalrarity].SpEffectPowerMax];
    }

    public string GetRarityName(int rarityid, bool withColor)
    {
        int matchedRarityId = GetNearestRarityId(rarityid);

        if (withColor)
        {
            return RarityConfigs[matchedRarityId].Name.WrapTextWithProperties(color: RarityConfigs[matchedRarityId].ColorHex);
        }

        return RarityConfigs[matchedRarityId].Name;
    }

    public int GetRaritySellValue(int rarityid)
    {
        int finalrarity = GetNearestRarityId(rarityid);
        return randomNumberGetter.NextInt(RarityConfigs[finalrarity].SellValueMin, RarityConfigs[finalrarity].SellValueMax);
    }

    public float GetRandomizedWeightForRarity(int rarityId)
    {
        int finalrarity = GetNearestRarityId(rarityId);
        return (float)this.randomNumberGetter.NextDouble(RarityConfigs[finalrarity].WeightMultMin, RarityConfigs[finalrarity].WeightMultMax);
    }

    public int GetLowestRarityId()
    {
        return RarityConfigs.Keys.Min();
    }

    public int GetNearestRarityId(int desiredrarityvalue = 0)
    {
        if (desiredrarityvalue == -1)
        {
            return this.randomNumberGetter.GetRandomItem<int>(RarityConfigs.Keys.ToList());
        }

        int finalrarityid = desiredrarityvalue;

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
        return RarityConfigs.Keys.Max();
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

        var options = iconMappingConfig.IconSheets.Where(d => d.IconMappings.RarityIds.Contains(rarityId)).Select(s => s.IconMappings)
            .Where(d => d.IconReplacements.Any(d => d.OriginalIconId == iconId)).FirstOrDefault();

        return options?.IconReplacements.FirstOrDefault(s => s.OriginalIconId == iconId)?.NewIconId ?? iconId;
    }
}
