namespace DSLRNet.Core.Handlers;

using DSLRNet.Core.DAL;

public class UpgradeCostHandler(
    ParamEditsRepository generatedDataRepository,
    DataAccess dataAccess,
    IOptions<Settings> settings,
    ILogger<UpgradeCostHandler> logger) : BaseHandler(generatedDataRepository)
{
    public void ModifyCostOfUpgrades()
    {
        foreach (var reinforceParam in dataAccess.ReinforceParamWeapon.GetAll())
        {
            var newReinforceParam = reinforceParam.Clone();
            newReinforceParam.ID += settings.Value.EquipMtrlParamStartId;

            // normalize the scaling to be equal
            var baseAttackRate = newReinforceParam.correctStrengthRate;
            newReinforceParam.correctAgilityRate = baseAttackRate;
            newReinforceParam.correctFaithRate = baseAttackRate;
            newReinforceParam.correctMagicRate = baseAttackRate;
            newReinforceParam.correctLuckRate = baseAttackRate;

            GeneratedDataRepository.AddParamEdit(new ParamEdit
            {
                ParamObject = newReinforceParam.GenericParam,
                ParamName = ParamNames.ReinforceParamWeapon,
                Operation = ParamOperation.Create
            });
        }

        if (settings.Value.RestrictSmithingStoneCost)
        {
            logger.LogInformation($"Reducing smithing stone cost in EquipMtrlSetParam");

            // The filter defined in appsettings.Default.json for this data source already restricts to only 
            // those params who apply to regular smithing stones
            foreach (var materialCostParam in dataAccess.EquipMtrlSetParam.GetAll())
            {
                // copy over 0-25 to 9000-9025

                var newCostParam = materialCostParam.Clone();
                newCostParam.ID += settings.Value.EquipMtrlParamStartId;

                newCostParam.itemNum01 = Math.Min(materialCostParam.itemNum01, (sbyte)settings.Value.MaxSmithingStoneCost);

                GeneratedDataRepository.AddParamEdit(new ParamEdit
                {
                    ParamObject = newCostParam.GenericParam,
                    ParamName = ParamNames.EquipMtrlSetParam,
                    Operation = ParamOperation.Create
                });
            }
        }
    }
}
