namespace DSLRNet.Core.Handlers;

using DSLRNet.Core.DAL;
using DSLRNet.Core.Extensions;

public class RarityHandler : BaseHandler
{
    private readonly RandomProvider randomProvider;
    private RarityIconMappingConfig iconMappingConfig = new();

    private readonly Dictionary<int, RaritySetup> RarityConfigs = [];

    public RarityHandler(
        RandomProvider randomProvider,
        ParamEditsRepository dataRepository,
        DataAccess dataAccess) : base(dataRepository)
    {
        this.randomProvider = randomProvider;
        this.RarityConfigs = dataAccess.RaritySetup.GetAll().ToDictionary(d => d.ID);
    }

    public Dictionary<int, int> CountByRarity { get; set; } = [];

    public int ChooseRarityFromIdSet(IntValueRange range)
    {
        List<WeightedValue<int>> weightedValues = [];

        foreach (int x in range.ToRangeOfValues())
        {
            weightedValues.Add(new WeightedValue<int> { Value = this.GetNearestRarity(x).ID, Weight = this.RarityConfigs[x].SelectionWeight });
        }

        int finalid = this.randomProvider.NextWeightedValue(weightedValues);

        if (!CountByRarity.TryGetValue(finalid, out var _))
        {
            CountByRarity[finalid] = 0;
        }

        CountByRarity[finalid]++;

        return finalid;
    }

    public Queue<bool> GetRarityEffectChances(int desiredCount, int rarityId, LootType lootType, float chanceMultiplier)
    {
        Queue<bool> chanceQueue = [];

        var rarity = this.GetNearestRarity(rarityId);
        int offset = 0;

        if (lootType != LootType.Weapon && rarityId >= 5)
        {
            chanceQueue.Enqueue(true);
            offset = 1;
        }

        for (int i = offset; i < desiredCount; i++)
        {
            string paramName = $"SpEffect{i}Chance";

            float chance = Convert.ToSingle(rarity.GetType().GetProperties().Single(d => d.Name.Contains(paramName)).GetValue(rarity));
            chanceQueue.Enqueue(this.randomProvider.PassesPercentCheck(chance * chanceMultiplier));
        }

        return chanceQueue;
    }

    public IntValueRange GetStatRequiredAdditionRange(int rarityId)
    {
        var rarity = this.GetNearestRarity(rarityId);
        return new IntValueRange(rarity.StatReqAddMin, rarity.StatReqAddMax);
    }

    public IntValueRange GetDamageAdditionRange(int rarityId)
    {
        var rarity = this.GetNearestRarity(rarityId);
        return new IntValueRange(rarity.WeaponDmgAddMin, rarity.WeaponDmgAddMax);
    }

    public IntValueRange GetWeaponScalingRange(int rarityId)
    {
        var rarity = this.GetNearestRarity(rarityId);
        return new IntValueRange(rarity.ScalingMin, rarity.ScalingMax);
    }

    public FloatValueRange GetShieldGuardRateRange(int rarityId)
    {
        var rarity = this.GetNearestRarity(rarityId);
        return new FloatValueRange(rarity.ShieldGuardRateMultMin, rarity.ShieldGuardRateMultMax);
    }

    public float GetArmorCutRateAddition(int rarityId)
    {
        var rarity = this.GetNearestRarity(rarityId);
        FloatValueRange range = new(rarity.ArmorCutRateAddMin, rarity.ArmorCutRateAddMax);
        return (float)Math.Round(this.randomProvider.Next(range), 4);
    }

    public float GetArmorResistMultiplier(int rarityid)
    {
        var rarity = this.GetNearestRarity(rarityid);
        FloatValueRange range = new(rarity.ArmorResistMinMult, rarity.ArmorResistMaxMult);

        return (float)this.randomProvider.Next(range);
    }

    public byte GetRarityParamValue(int rarityId)
    {
        return (byte)this.GetNearestRarity(rarityId).RarityParamValue;
    }

    public IntValueRange GetSpeffectPowerRange(int rarityId)
    {
        var rarity = this.GetNearestRarity(rarityId);

        return new IntValueRange(
            Math.Clamp(rarity.SpEffectPowerMin - 10, 0, rarity.SpEffectPowerMax),
            rarity.SpEffectPowerMax);
    }

    public string GetRarityName(int rarityId, bool withColor)
    {
        var rarity = this.GetNearestRarity(rarityId);

        if (withColor)
        {
            return rarity.Name.WrapTextWithProperties(color: rarity.ColorHex);
        }

        return rarity.Name;
    }

    public int GetSellValue(int rarityid)
    {
        var rarity = this.GetNearestRarity(rarityid);
        return this.randomProvider.NextInt(rarity.SellValueMin, rarity.SellValueMax);
    }

    public float GetRandomizedWeight(float originalWeight, int rarityId)
    {
        RaritySetup rarity = this.GetNearestRarity(rarityId);
        return originalWeight * (float)this.randomProvider.Next(rarity.WeightMultMin, rarity.WeightMultMax);
    }

    public ushort GetIconId(ushort iconId, int rarityId, bool isUnique = false)
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

    private RaritySetup GetNearestRarity(int rarityId = 0)
    {
        if (rarityId == -1)
        {
            return RarityConfigs[this.randomProvider.GetRandomItem(this.RarityConfigs.Keys.ToList())];
        }

        int nearestRarity = rarityId;

        if (!this.RarityConfigs.ContainsKey(rarityId))
        {
            foreach (int x in this.RarityConfigs.Keys)
            {
                if (rarityId <= x)
                {
                    nearestRarity = x;
                }
            }
        }
        return RarityConfigs[nearestRarity];
    }
}
