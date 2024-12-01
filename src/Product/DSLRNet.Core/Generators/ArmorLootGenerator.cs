namespace DSLRNet.Core.Generators;

public class ArmorLootGenerator : ParamLootGenerator
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

        IEnumerable<EquipParamProtector> armorLoots = paramDataSource.LoadAll();

        LoadedLoot = armorLoots.Select(GenericParam.FromObject).ToList();
    }

    public int CreateArmor(int rarityId, List<int> wllIds)
    {
        GenericParam newArmor = GetLootDictionaryFromId(WhiteListHandler.GetLootByAllowList(wllIds, LootType.Armor));

        string armorStatDesc = "";

        newArmor.SetValue("ID", CumulativeID.GetNext());

        SetLootSellValue(newArmor, rarityId);

        SetLootRarityParamValue(newArmor, rarityId);

        armorStatDesc += ApplyCutRateAdditionsFromRarity(rarityId, newArmor);

        ApplyArmorResistanceAdditions(newArmor, rarityId);

        RandomizeLootWeightBasedOnRarity(newArmor, rarityId);

        IEnumerable<SpEffectText> speffs = ApplySpEffects(rarityId, [0], newArmor, 1.0f, true, -1, true);

        newArmor.SetValue("iconIdM", RarityHandler.GetIconIdForRarity(newArmor.GetValue<int>("iconIdM"), rarityId));
        newArmor.SetValue("iconIdF", RarityHandler.GetIconIdForRarity(newArmor.GetValue<int>("iconIdF"), rarityId));

        string originalName = newArmor.Name;
        string finalTitle = CreateLootTitle(originalName.Replace(" (Altered)", " (Alt)"), rarityId, "", speffs, true, false);

        //newArmor.Name = finalTitle;

        ExportLootGenParamAndTextToOutputs(newArmor, LootType.Armor, finalTitle, CreateArmorDescription(string.Join(Environment.NewLine, speffs.Select(s => s.Description).ToList()), armorStatDesc + GetParamLootLore(finalTitle, true)));

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