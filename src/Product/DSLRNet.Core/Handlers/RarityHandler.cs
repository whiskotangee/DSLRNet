namespace DSLRNet.Core.Handlers;
using DSLRNet.Core.Extensions;

public class RarityHandler : BaseHandler
{
    private readonly RandomProvider randomNumberGetter;
    private RarityIconMappingConfig iconMappingConfig = new();

    private readonly Dictionary<int, RaritySetup> RarityConfigs = [];

    public RarityHandler(
        RandomProvider randomNumberGetter,
        ParamEditsRepository dataRepository,
        IDataSource<RaritySetup> raritySetupDataSource) : base(dataRepository)
    {
        this.randomNumberGetter = randomNumberGetter;
        this.RarityConfigs = raritySetupDataSource.GetAll().ToDictionary(d => d.ID);
    }

    public int ChooseRarityFromIdSetWithBuiltInWeights(List<int> idset)
    {
        List<int> valididset = [];
        List<int> valididweights = [];

        foreach (int x in idset)
        {
            valididset.Add(this.GetNearestRarityId(x));
        }

        foreach (int x in idset)
        {
            valididweights.Add(this.RarityConfigs[valididset.First(d => d == x)].SelectionWeight);
        }

        int finalid = this.randomNumberGetter.NextWeightedValue(valididset, valididweights);

        return finalid;
    }

    public Queue<bool> GetRarityEffectChanceArray(int desiredCount, int rarityid, bool armortalisman = false)
    {
        Queue<bool> finalboolarray = [];

        int finalrarityid = this.GetNearestRarityId(rarityid);
        int offset = 0;

        if (armortalisman || rarityid > 5)
        {
            finalboolarray.Enqueue(true);
            offset = 1;
        }

        for (int i = offset; i < desiredCount; i++)
        {
            string spefchance = $"SpEffect{i}Chance";
            RaritySetup item = this.RarityConfigs[finalrarityid];

            float speffectchance = (float)item.GetType().GetProperty(spefchance).GetValue(item);
            finalboolarray.Enqueue(this.randomNumberGetter.GetRandomBoolByPercent(speffectchance));
        }

        return finalboolarray;
    }

    public Range<int> GetRarityDamageAdditionRange(int rarityid)
    {
        int finalrarity = this.GetNearestRarityId(rarityid);
        return new Range<int>(this.RarityConfigs[finalrarity].WeaponDmgAddMin, this.RarityConfigs[finalrarity].WeaponDmgAddMax);
    }

    public Range<float> GetRarityArmorCutRateRange(int rarityid)
    {
        int finalrarity = this.GetNearestRarityId(rarityid);
        return new Range<float>(this.RarityConfigs[finalrarity].ArmorCutRateAddMin, this.RarityConfigs[finalrarity].ArmorCutRateAddMax);
    }

    public Range<float> GetRarityShieldGuardRateRange(int rarityid)
    {
        int finalrarity = this.GetNearestRarityId(rarityid);
        return new Range<float>(this.RarityConfigs[finalrarity].ShieldGuardRateMultMin, this.RarityConfigs[finalrarity].ShieldGuardRateMultMax);
    }

    public float GetRarityArmorCutRateAddition(int rarityid)
    {
        int finalrarity = this.GetNearestRarityId(rarityid);
        Range<float> range = this.GetRarityArmorCutRateRange(finalrarity);
        return (float)Math.Round(this.randomNumberGetter.Next(range), 4);
    }

    public List<int> GetRaritiesWithinRange(int highest = 10, int lowerrange = 0)
    {
        if (highest == -1)
        {
            if (this.RarityConfigs.Count != 0)
            {
                return [0];
            }

            highest = this.GetNearestRarityId((int)Math.Round(this.RarityConfigs.Keys.Max() * 0.5));
        }

        highest = this.GetNearestRarityId(highest);

        int lowest = Math.Clamp(this.GetNearestRarityId((int)Math.Round((highest - lowerrange) / 2f)), this.GetLowestRarityId(), this.GetHighestRarityId());
        List<int> finalrarities = [];

        foreach (int x in new int[] { highest, lowest })
        {
            finalrarities.Add(x);
        }

        foreach (int x in this.RarityConfigs.Keys)
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
        int finalrarity = this.GetNearestRarityId(rarityid);
        return new Range<float>(this.RarityConfigs[finalrarity].ArmorResistMinMult, this.RarityConfigs[finalrarity].ArmorResistMaxMult);
    }

    public float GetRarityArmorResistMultiplier(int rarityid)
    {
        int finalrarity = this.GetNearestRarityId(rarityid);
        Range<float> range = this.GetRarityArmorResistMultRange(finalrarity);

        return (float)this.randomNumberGetter.Next(range);
    }

    public int GetRarityParamValue(int rarityid)
    {
        return this.RarityConfigs[this.GetNearestRarityId(rarityid)].RarityParamValue;
    }

    public List<int> GetRaritySpeffectPowerArray(int rarityid)
    {
        int finalrarity = this.GetNearestRarityId(rarityid);

        return [Math.Clamp(this.RarityConfigs[finalrarity].SpEffectPowerMin - 10, 0, this.RarityConfigs[finalrarity].SpEffectPowerMax), this.RarityConfigs[finalrarity].SpEffectPowerMax];
    }

    public string GetRarityName(int rarityid, bool withColor)
    {
        int matchedRarityId = this.GetNearestRarityId(rarityid);

        if (withColor)
        {
            return this.RarityConfigs[matchedRarityId].Name.WrapTextWithProperties(color: this.RarityConfigs[matchedRarityId].ColorHex);
        }

        return this.RarityConfigs[matchedRarityId].Name;
    }

    public int GetRaritySellValue(int rarityid)
    {
        int finalrarity = this.GetNearestRarityId(rarityid);
        return this.randomNumberGetter.NextInt(this.RarityConfigs[finalrarity].SellValueMin, this.RarityConfigs[finalrarity].SellValueMax);
    }

    public float GetRandomizedWeightForRarity(int rarityId)
    {
        int finalrarity = this.GetNearestRarityId(rarityId);
        return (float)this.randomNumberGetter.NextDouble(this.RarityConfigs[finalrarity].WeightMultMin, this.RarityConfigs[finalrarity].WeightMultMax);
    }

    public int GetLowestRarityId()
    {
        return this.RarityConfigs.Keys.Min();
    }

    public int GetNearestRarityId(int desiredrarityvalue = 0)
    {
        if (desiredrarityvalue == -1)
        {
            return this.randomNumberGetter.GetRandomItem<int>(this.RarityConfigs.Keys.ToList());
        }

        int finalrarityid = desiredrarityvalue;

        if (!this.RarityConfigs.ContainsKey(desiredrarityvalue))
        {
            foreach (int x in this.RarityConfigs.Keys)
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

        RarityIconMapping? options = this.iconMappingConfig.IconSheets.Where(d => d.IconMappings.RarityIds.Contains(rarityId)).Select(s => s.IconMappings)
            .Where(d => d.IconReplacements.Any(d => d.OriginalIconId == iconId)).FirstOrDefault();

        return options?.IconReplacements.FirstOrDefault(s => s.OriginalIconId == iconId)?.NewIconId ?? iconId;
    }

    public void UpdateIconMapping(RarityIconMappingConfig config)
    {
        this.iconMappingConfig = config;
    }
}
