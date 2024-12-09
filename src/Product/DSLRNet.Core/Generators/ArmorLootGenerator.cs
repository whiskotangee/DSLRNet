namespace DSLRNet.Core.Generators;

using System.Linq;

public class ArmorLootGenerator : ParamLootGenerator<EquipParamProtector>
{
    private readonly ArmorGeneratorConfig armorConfig;

    public ArmorLootGenerator(
        RarityHandler rarityHandler,
        AllowListHandler whiteListHandler,
        SpEffectHandler spEffectHandler,
        LoreGenerator loreGenerator,
        RandomProvider random,
        ParamEditsRepository dataRepository,
        IOptions<Configuration> configuration,
        IOptions<ArmorGeneratorConfig> armorConfig,
        IDataSource<EquipParamProtector> paramDataSource,
        ILogger<ParamLootGenerator<EquipParamProtector>> logger)
        : base(rarityHandler, whiteListHandler, spEffectHandler, loreGenerator, random, configuration, dataRepository, ParamNames.EquipParamProtector, logger)
    {
        this.CumulativeID = new CumulativeID(logger);

        this.DataSource = paramDataSource;
        this.armorConfig = armorConfig.Value;
    }

    public int CreateArmor(int rarityId, List<int> wllIds)
    {
        EquipParamProtector newArmor = this.GetNewLootItem(this.WhiteListHandler.GetLootByAllowList(wllIds, LootType.Armor));

        newArmor.ID = this.CumulativeID.GetNext();
        newArmor.sellValue = this.RarityHandler.GetSellValue(rarityId);
        newArmor.rarity = this.RarityHandler.GetRarityParamValue(rarityId);
        newArmor.iconIdM = this.RarityHandler.GetIconId(newArmor.iconIdM, rarityId);
        newArmor.iconIdF = this.RarityHandler.GetIconId(newArmor.iconIdF, rarityId);

        string armorStatDesc = this.ApplyCutRateAdditions(newArmor, rarityId);

        this.ModifyArmorResistance(newArmor, rarityId);

        newArmor.weight = this.RarityHandler.GetRandomizedWeight(newArmor.weight, rarityId);

        IEnumerable<SpEffectText> speffs = this.ApplySpEffects(rarityId, [0], newArmor.GenericParam, 1.0f, true, -1, true);

        string originalName = newArmor.Name;
        string finalTitle = this.CreateLootTitle(originalName.Replace(" (Altered)", ""), rarityId, "", speffs, true, false);

        //newArmor.Name = finalTitle;

        this.AddLootDetails(
            newArmor.GenericParam, 
            LootType.Armor, 
            finalTitle, 
            this.CreateArmorDescription(string.Join(Environment.NewLine, speffs.Select(s => s.Description).ToList()), armorStatDesc),
            this.LoreGenerator.GenerateDescription(finalTitle, true));

        return newArmor.ID;
    }

    private string ApplyCutRateAdditions(EquipParamProtector newArmor, int rarityId)
    {
        List<string> descriptionStrings = [];

        float addition = this.RarityHandler.GetArmorCutRateAddition(rarityId);
        IEnumerable<string> cutRateProperties = newArmor.GenericParam.GetFieldNamesByFilter("DamageCutRate", true, "flick");
        List<string> cutRateModifyProperties = this.Random.GetRandomItems(cutRateProperties, this.armorConfig.CutRateParamBuffCount);

        foreach (string param in cutRateModifyProperties)
        {
            float oldValue = newArmor.GenericParam.GetValue<float>(param);
            newArmor.GenericParam.SetValue(param, oldValue - addition);
            descriptionStrings.Add(param.Replace("DamageCutRate", "").ToUpper());
        }

        List<string> defenseProperties = newArmor.GenericParam.GetFieldNamesByFilter("defense", false, "Material").ToList();

        foreach (string param in defenseProperties)
        {
            newArmor.GenericParam.SetValue(param, 0);
        }

        return $"+{addition*100:F1}% {string.Join("/", descriptionStrings)} Defense";
    }

    private void ModifyArmorResistance(EquipParamProtector newArmor, int rarity)
    {
        var resistProperties = newArmor.GenericParam.GetFieldNamesByFilter("resist");
        float multiplier = this.RarityHandler.GetArmorResistMultiplier(rarity);

        foreach (string param in this.Random.GetRandomItems(resistProperties, this.armorConfig.ResistParamBuffCount))
        {
            newArmor.GenericParam.SetValue(param, (int)(newArmor.GenericParam.GetValue<int>(param) * multiplier));
        }
    }

    public string CreateArmorDescription(string speffects = "", string extraProtection = "")
    {
        return $"{speffects}{Environment.NewLine}{extraProtection}";
    }
}