namespace DSLRNet.Core.Handlers;

using DSLRNet.Core.DAL;

public class SmithingStoneCostHandler(
    ParamEditsRepository generatedDataRepository,
    DataAccess dataAccess,
    IOptions<Settings> settings,
    ILogger<SmithingStoneCostHandler> logger) : BaseHandler(generatedDataRepository)
{
    public void ReduceSmithingStoneCostIfEnabled()
    {
        if (settings.Value.RestrictSmithingStoneCost)
        {
            logger.LogInformation($"Reducing smithing stone cost in EquipMtrlSetParam");

            // The filter defined in appsettings.Default.json for this data source already restricts to only 
            // those params who apply to regular smithing stones
            foreach (var materialCostParam in dataAccess.EquipMtrlSetParam.GetAll())
            {
                if (materialCostParam.itemNum01 > 1)
                {
                    materialCostParam.itemNum01 = Math.Min(materialCostParam.itemNum01, (sbyte)settings.Value.MaxSmithingStoneCost);

                    GeneratedDataRepository.AddParamEdit(new ParamEdit
                    {
                        ParamObject = materialCostParam.GenericParam,
                        ParamName = ParamNames.EquipMtrlSetParam,
                        Operation = ParamOperation.Create
                    });
                }
            }
        }
    }
}
