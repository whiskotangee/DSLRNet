namespace DSLRNet.Core.Generators;

public class ArmorLootGenerator : ParamLootGenerator<EquipParamProtector>
{
    public ArmorLootGenerator(
        RarityHandler rarityHandler,
        AllowListHandler whiteListHandler,
        SpEffectHandler spEffectHandler,
        LoreGenerator loreGenerator,
        RandomProvider random,
        ParamEditsRepository dataRepository,
        IOptions<Configuration> configuration,
        IDataSource<EquipParamProtector> paramDataSource,
        ILogger<ParamLootGenerator<EquipParamProtector>> logger)
        : base(rarityHandler, whiteListHandler, spEffectHandler, loreGenerator, random, configuration, dataRepository, ParamNames.EquipParamProtector, logger)
    {
        this.CumulativeID = new CumulativeID(logger);

        this.DataSource = paramDataSource;
    }

    public int CreateArmor(int rarityId, List<int> wllIds)
    {
        EquipParamProtector newArmor = this.GetNewLootItem(this.WhiteListHandler.GetLootByAllowList(wllIds, LootType.Armor));

        string armorStatDesc = "";

        newArmor.ID = this.CumulativeID.GetNext();
        newArmor.sellValue = this.RarityHandler.GetRaritySellValue(rarityId);
        newArmor.rarity = this.RarityHandler.GetRarityParamValue(rarityId);
        newArmor.iconIdM = this.RarityHandler.GetIconIdForRarity(newArmor.iconIdM, rarityId);
        newArmor.iconIdF = this.RarityHandler.GetIconIdForRarity(newArmor.iconIdF, rarityId);

        armorStatDesc += this.ApplyCutRateAdditionsFromRarity(rarityId, newArmor.GenericParam);

        this.ApplyArmorResistanceAdditions(newArmor.GenericParam, rarityId);

        newArmor.weight = this.RarityHandler.GetRandomizedWeight(newArmor.weight, rarityId);

        IEnumerable<SpEffectText> speffs = this.ApplySpEffects(rarityId, [0], newArmor.GenericParam, 1.0f, true, -1, true);

        string originalName = newArmor.Name;
        string finalTitle = this.CreateLootTitle(originalName.Replace(" (Altered)", " (Alt)"), rarityId, "", speffs, true, false);

        //newArmor.Name = finalTitle;

        this.ExportLootDetails(newArmor.GenericParam, LootType.Armor, finalTitle, this.CreateArmorDescription(string.Join(Environment.NewLine, speffs.Select(s => s.Description).ToList()), armorStatDesc + this.LoreGenerator.GenerateDescription(finalTitle, true)));

        return newArmor.ID;
    }

    public string ApplyCutRateAdditionsFromRarity(int rarityId, GenericParam outputDictionary)
    {
        string descriptionString = "";

        List<string> cutRateParams = this.Configuration.LootParam.ArmorParam;
        List<string> defenseParams = this.Configuration.LootParam.ArmorDefenseParams;

        if (cutRateParams.Count > 0)
        {
            foreach (string param in cutRateParams)
            {
                if (outputDictionary.ContainsKey(param))
                {
                    float oldValue = outputDictionary.GetValue<float>(param);
                    outputDictionary.SetValue(param, oldValue - this.RarityHandler.GetRarityArmorCutRateAddition(rarityId));
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
        List<string> resistances = this.Configuration.LootParam.ArmorResistParams;

        if (resistances.Count > 0)
        {
            foreach (string param in resistances)
            {
                if (newArmor.ContainsKey(param))
                {
                    int oldValue = newArmor.GetValue<int>(param);
                    newArmor.SetValue(param, (int)(oldValue * this.RarityHandler.GetRarityArmorResistMultiplier(rarity)));
                }
            }
        }
    }

    public string CreateArmorDescription(string speffects = "", string extraProtection = "")
    {
        return $"{speffects}{Environment.NewLine}{extraProtection}";
    }
}