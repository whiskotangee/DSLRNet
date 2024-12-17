namespace DSLRNet.Core.Generators;

using DSLRNet.Core.DAL;
using System.Linq;

public class ArmorLootGenerator : ParamLootGenerator<EquipParamProtector>
{
    private readonly ArmorGeneratorConfig armorConfig;

    public ArmorLootGenerator(
        RarityHandler rarityHandler,
        SpEffectHandler spEffectHandler,
        LoreGenerator loreGenerator,
        RandomProvider random,
        ParamEditsRepository dataRepository,
        IOptions<Configuration> configuration,
        IOptions<ArmorGeneratorConfig> armorConfig,
        DataAccess dataAccess,
        ILogger<ParamLootGenerator<EquipParamProtector>> logger)
        : base(rarityHandler, spEffectHandler, loreGenerator, random, configuration, dataRepository, ParamNames.EquipParamProtector, logger)
    {
        this.CumulativeID = new CumulativeID(logger);

        this.DataSource = dataAccess.EquipParamProtector;
        this.armorConfig = armorConfig.Value;
    }

    public int CreateArmor(int rarity)
    {
        EquipParamProtector newArmor = this.GetNewLootItem();

        newArmor.ID = (int)this.CumulativeID.GetNext();
        newArmor.sellValue = this.RarityHandler.GetSellValue(rarity);
        newArmor.rarity = this.RarityHandler.GetRarityParamValue(rarity);
        newArmor.iconIdM = this.RarityHandler.GetIconId(newArmor.iconIdM, rarity);
        newArmor.iconIdF = this.RarityHandler.GetIconId(newArmor.iconIdF, rarity);

        string armorStatDesc = this.ApplyCutRateAdditions(newArmor, rarity);

        this.ModifyArmorResistance(newArmor, rarity);

        newArmor.weight = this.RarityHandler.GetRandomizedWeight(newArmor.weight, rarity);

        IEnumerable<SpEffectText> speffs = this.ApplySpEffects(rarity, [0], newArmor.GenericParam, 1.0f, LootType.Armor, -1, true);

        string originalName = newArmor.Name;
        string finalTitle = this.CreateLootTitle(originalName.Replace(" (Altered)", ""), rarity, "", speffs, true, false);

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
        IEnumerable<string> cutRateProperties = newArmor.GetFieldNamesByFilter("DamageCutRate", true, "flick");
        List<string> cutRateModifyProperties = this.Random.GetRandomItems(cutRateProperties, this.armorConfig.CutRateParamBuffCount);

        foreach (string param in cutRateModifyProperties)
        {
            float oldValue = newArmor.GetValue<float>(param);
            newArmor.SetValue(param, oldValue - addition);
            descriptionStrings.Add(param.Replace("DamageCutRate", "").ToUpper());
        }

        List<string> defenseProperties = newArmor.GetFieldNamesByFilter("defense", false, "Material").ToList();

        foreach (string param in defenseProperties)
        {
            newArmor.SetValue(param, 0);
        }

        return $"+{addition*100:F1}% {string.Join("/", descriptionStrings)} Defense";
    }

    private void ModifyArmorResistance(EquipParamProtector newArmor, int rarity)
    {
        var resistProperties = newArmor.GetFieldNamesByFilter("resist");
        float multiplier = this.RarityHandler.GetArmorResistMultiplier(rarity);

        foreach (string param in this.Random.GetRandomItems(resistProperties, this.armorConfig.ResistParamBuffCount))
        {
            newArmor.SetValue(param, (int)(newArmor.GetValue<int>(param) * multiplier));
        }
    }

    public string CreateArmorDescription(string speffects = "", string extraProtection = "")
    {
        return $"{speffects}{Environment.NewLine}{extraProtection}";
    }
}