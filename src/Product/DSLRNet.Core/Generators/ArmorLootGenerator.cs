namespace DSLRNet.Core.Generators;

public class ArmorLootGenerator : ParamLootGenerator<EquipParamProtector>
{
    public ArmorLootGenerator(
        RarityHandler rarityHandler,
        AllowListHandler whiteListHandler,
        SpEffectHandler spEffectHandler,
        LoreGenerator loreGenerator,
        RandomNumberGetter random,
        ParamEditsRepository dataRepository,
        IOptions<Configuration> configuration,
        IDataSource<EquipParamProtector> paramDataSource)
        : base(rarityHandler, whiteListHandler, spEffectHandler, loreGenerator, random, configuration, dataRepository, ParamNames.EquipParamProtector)
    {
        CumulativeID = new CumulativeID();

        DataSource = paramDataSource;
    }

    public int CreateArmor(int rarityId, List<int> wllIds)
    {
        EquipParamProtector newArmor = GetLootDictionaryFromId(WhiteListHandler.GetLootByAllowList(wllIds, LootType.Armor));

        string armorStatDesc = "";

        newArmor.ID = CumulativeID.GetNext();

        SetLootSellValue(newArmor.GenericParam, rarityId);

        SetLootRarityParamValue(newArmor.GenericParam, rarityId);

        armorStatDesc += ApplyCutRateAdditionsFromRarity(rarityId, newArmor.GenericParam);

        ApplyArmorResistanceAdditions(newArmor.GenericParam, rarityId);

        RandomizeLootWeightBasedOnRarity(newArmor.GenericParam, rarityId);

        IEnumerable<SpEffectText> speffs = ApplySpEffects(rarityId, [0], newArmor.GenericParam, 1.0f, true, -1, true);

        newArmor.iconIdM = RarityHandler.GetIconIdForRarity(newArmor.iconIdM, rarityId);
        newArmor.iconIdF = RarityHandler.GetIconIdForRarity(newArmor.iconIdF, rarityId);

        string originalName = newArmor.Name;
        string finalTitle = CreateLootTitle(originalName.Replace(" (Altered)", " (Alt)"), rarityId, "", speffs, true, false);

        //newArmor.Name = finalTitle;

        ExportLootGenParamAndTextToOutputs(newArmor.GenericParam, LootType.Armor, finalTitle, CreateArmorDescription(string.Join(Environment.NewLine, speffs.Select(s => s.Description).ToList()), armorStatDesc + GetParamLootLore(finalTitle, true)));

        return newArmor.ID;
    }

    public string ApplyCutRateAdditionsFromRarity(int rarityId, GenericParam outputDictionary)
    {
        string descriptionString = "";

        List<string> cutRateParams = Configuration.LootParam.ArmorParam;
        List<string> defenseParams = Configuration.LootParam.ArmorDefenseParams;

        if (cutRateParams.Count > 0)
        {
            foreach (string param in cutRateParams)
            {
                if (outputDictionary.ContainsKey(param))
                {
                    float oldValue = outputDictionary.GetValue<float>(param);
                    outputDictionary.SetValue(param, oldValue - RarityHandler.GetRarityArmorCutRateAddition(rarityId));
                }
            }
        }

        if (defenseParams.Count > 0)
        {
            foreach (string param in defenseParams)
            {
                if (outputDictionary.ContainsKey(param))
                {
                    outputDictionary.SetValue(param, 0);
                }
            }
        }

        return descriptionString;
    }

    public void ApplyArmorResistanceAdditions(GenericParam newArmor, int rarity)
    {
        List<string> resistances = Configuration.LootParam.ArmorResistParams;

        if (resistances.Count > 0)
        {
            foreach (string param in resistances)
            {
                if (newArmor.ContainsKey(param))
                {
                    int oldValue = newArmor.GetValue<int>(param);
                    newArmor.SetValue(param, (int)(oldValue * RarityHandler.GetRarityArmorResistMultiplier(rarity)));
                }
            }
        }
    }

    public string CreateArmorDescription(string speffects = "", string extraProtection = "")
    {
        return $"{speffects}{Environment.NewLine}{extraProtection}";
    }
}