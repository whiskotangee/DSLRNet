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
            weightedValues.Add(new WeightedValue<int> { Value = this.GetNearestRarityId(x), Weight = this.RarityConfigs[x].SelectionWeight });
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

        rarityId = this.GetNearestRarityId(rarityId);
        int offset = 0;

        if (lootType != LootType.Weapon && rarityId >= 5)
        {
            chanceQueue.Enqueue(true);
            offset = 1;
        }

        for (int i = offset; i < desiredCount; i++)
        {
            string paramName = $"SpEffect{i}Chance";
            RaritySetup item = this.RarityConfigs[rarityId];

            float chance = item.GetValue<float>(paramName);
            chanceQueue.Enqueue(this.randomProvider.PassesPercentCheck(chance * chanceMultiplier));
        }

        return chanceQueue;
    }

    public IntValueRange GetDamageAdditionRange(int rarityId)
    {
        rarityId = this.GetNearestRarityId(rarityId);
        return new IntValueRange(this.RarityConfigs[rarityId].WeaponDmgAddMin, this.RarityConfigs[rarityId].WeaponDmgAddMax);
    }

    public IntValueRange GetWeaponScalingRange(int rarityId)
    {
        rarityId = this.GetNearestRarityId(rarityId);
        return new IntValueRange(this.RarityConfigs[rarityId].ScalingMin, this.RarityConfigs[rarityId].ScalingMax);
    }

    public FloatValueRange GetArmorCutRateRange(int rarityId)
    {
        rarityId = this.GetNearestRarityId(rarityId);
        return new FloatValueRange(this.RarityConfigs[rarityId].ArmorCutRateAddMin, this.RarityConfigs[rarityId].ArmorCutRateAddMax);
    }

    public FloatValueRange GetShieldGuardRateRange(int rarityId)
    {
        rarityId = this.GetNearestRarityId(rarityId);
        return new FloatValueRange(this.RarityConfigs[rarityId].ShieldGuardRateMultMin, this.RarityConfigs[rarityId].ShieldGuardRateMultMax);
    }

    public float GetArmorCutRateAddition(int rarityId)
    {
        rarityId = this.GetNearestRarityId(rarityId);
        FloatValueRange range = this.GetArmorCutRateRange(rarityId);
        return (float)Math.Round(this.randomProvider.Next(range), 4);
    }

    public FloatValueRange GetArmorResistMultRange(int rarityid)
    {
        int finalrarity = this.GetNearestRarityId(rarityid);
        return new FloatValueRange(this.RarityConfigs[finalrarity].ArmorResistMinMult, this.RarityConfigs[finalrarity].ArmorResistMaxMult);
    }

    public float GetArmorResistMultiplier(int rarityid)
    {
        int finalrarity = this.GetNearestRarityId(rarityid);
        FloatValueRange range = this.GetArmorResistMultRange(finalrarity);

        return (float)this.randomProvider.Next(range);
    }

    public byte GetRarityParamValue(int rarityId)
    {
        return (byte)this.RarityConfigs[this.GetNearestRarityId(rarityId)].RarityParamValue;
    }

    public IntValueRange GetSpeffectPowerArray(int rarityId)
    {
        rarityId = this.GetNearestRarityId(rarityId);

        return new IntValueRange(
            Math.Clamp(this.RarityConfigs[rarityId].SpEffectPowerMin - 10, 0, this.RarityConfigs[rarityId].SpEffectPowerMax), 
            this.RarityConfigs[rarityId].SpEffectPowerMax);
    }

    public string GetRarityName(int rarityId, bool withColor)
    {
        rarityId = this.GetNearestRarityId(rarityId);

        if (withColor)
        {
            return this.RarityConfigs[rarityId].Name.WrapTextWithProperties(color: this.RarityConfigs[rarityId].ColorHex);
        }

        return this.RarityConfigs[rarityId].Name;
    }

    public int GetSellValue(int rarityid)
    {
        int finalrarity = this.GetNearestRarityId(rarityid);
        return this.randomProvider.NextInt(this.RarityConfigs[finalrarity].SellValueMin, this.RarityConfigs[finalrarity].SellValueMax);
    }

    public float GetRandomizedWeight(float originalWeight, int rarityId)
    {
        int finalrarity = this.GetNearestRarityId(rarityId);
        return originalWeight * (float)this.randomProvider.Next(this.RarityConfigs[finalrarity].WeightMultMin, this.RarityConfigs[finalrarity].WeightMultMax);
    }

    public int GetNearestRarityId(int desiredrarityvalue = 0)
    {
        if (desiredrarityvalue == -1)
        {
            return this.randomProvider.GetRandomItem(this.RarityConfigs.Keys.ToList());
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
}
