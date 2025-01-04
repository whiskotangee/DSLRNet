namespace DSLRNet.Core.Generators;

using DSLRNet.Core.Contracts;
using DSLRNet.Core.DAL;
using System.Linq;

public class ArmorLootGenerator : ParamLootGenerator<EquipParamProtector>
{
    public ArmorLootGenerator(
        RarityHandler rarityHandler,
        SpEffectHandler spEffectHandler,
        LoreGenerator loreGenerator,
        RandomProvider random,
        ParamEditsRepository dataRepository,
        IOptions<Configuration> configuration,
        IOptions<Settings> settings,
        DataAccess dataAccess,
        ILogger<ParamLootGenerator<EquipParamProtector>> logger)
        : base(rarityHandler, spEffectHandler, loreGenerator, random, configuration, settings, dataRepository, ParamNames.EquipParamProtector, logger)
    {
        this.IDGenerator = new IDGenerator()
        {
            StartingID = 80000000,
            Multiplier = 1000,
        };

        this.DataSource = dataAccess.EquipParamProtector;
    }

    public int CreateArmor(int rarity)
    {
        EquipParamProtector newArmor = this.DataSource.GetRandomItem().Clone();

        var nextId = (int)this.IDGenerator.GetNext();
        newArmor.ID = nextId;
        newArmor.sellValue = this.RarityHandler.GetSellValue(rarity);
        newArmor.rarity = this.RarityHandler.GetRarityParamValue(rarity);
        newArmor.iconIdM = this.RarityHandler.GetIconId(newArmor.iconIdM, rarity);
        newArmor.iconIdF = this.RarityHandler.GetIconId(newArmor.iconIdF, rarity);

        string armorStatDesc = this.ApplyCutRateAdditions(newArmor, rarity);

        this.ModifyArmorResistance(newArmor, rarity);

        newArmor.weight = this.RarityHandler.GetRandomizedWeight(newArmor.weight, rarity);

        IEnumerable<SpEffectDetails> spEffects = this.ApplySpEffects(rarity, [0], newArmor.GenericParam, 1.0f, LootType.Armor, -1, true);

        string originalName = newArmor.Name;
        string finalTitle = this.CreateLootTitle(originalName.Replace(" (Altered)", ""), rarity, "", spEffects, true, false);
        string description = string.Join(Environment.NewLine, spEffects.Select(s => s.Description).Append(armorStatDesc).Append(this.LoreGenerator.GenerateDescription(finalTitle, true)));
        //newArmor.Name = finalTitle;

        this.GeneratedDataRepository.AddParamEdit(
            new ParamEdit
            {
                ParamName = this.OutputParamName,
                Operation = ParamOperation.Create,
                ItemText = new LootFMG()
                {
                    Category = this.OutputLootRealNames[LootType.Armor],
                    Name = finalTitle,
                    Caption = description
                },
                ParamObject = newArmor.GenericParam
            });

        return newArmor.ID;
    }

    private string ApplyCutRateAdditions(EquipParamProtector newArmor, int rarityId)
    {
        List<string> descriptionStrings = [];

        float addition = this.RarityHandler.GetArmorCutRateAddition(rarityId);
        IEnumerable<string> cutRateProperties = newArmor.GetFieldNamesByFilter("DamageCutRate", true, "flick");
        List<string> cutRateModifyProperties = this.Random.GetRandomItems(cutRateProperties, this.Settings.ArmorGeneratorSettings.CutRateParamBuffCount);

        foreach (string param in cutRateModifyProperties)
        {
            float oldValue = newArmor.GetValue<float>(param);
            newArmor.SetValue(param, oldValue - addition);
            descriptionStrings.Add(param.Replace("DamageCutRate", "").ToUpper());
        }

        List<string> defenseProperties = [.. newArmor.GetFieldNamesByFilter("defense", false, "Material")];

        foreach (string param in defenseProperties)
        {
            newArmor.SetValue(param, 0);
        }

        return $"+{addition * 100:F1}% {string.Join("/", descriptionStrings)} Defense";
    }

    private void ModifyArmorResistance(EquipParamProtector newArmor, int rarity)
    {
        List<string> resistProperties = newArmor.GetFieldNamesByFilter("resist");
        float multiplier = this.RarityHandler.GetArmorResistMultiplier(rarity);

        foreach (string param in this.Random.GetRandomItems(resistProperties, this.Settings.ArmorGeneratorSettings.ResistParamBuffCount))
        {
            newArmor.SetValue(param, (int)(newArmor.GetValue<int>(param) * multiplier));
        }
    }

    public string CreateArmorDescription(string spEffects = "", string extraProtection = "")
    {
        if (string.IsNullOrEmpty(spEffects))
        {
            return extraProtection;
        }

        return $"{spEffects}{Environment.NewLine}{extraProtection}";
    }
}