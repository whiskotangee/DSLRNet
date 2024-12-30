﻿namespace DSLRNet.Core.Data;

using DSLRNet.Core.DAL;
using System.Text;

public class ParamEditsRepository(
    DataAccess dataAccess, 
    ILogger<ParamEditsRepository> logger,
    RegulationBinBank regulationBin,
    IOperationProgressTracker? progressTracker = null)
{
    private Dictionary<ParamNames, Dictionary<long, ParamEdit>> paramEdits { get; set; } =
        Enum.GetValues(typeof(ParamNames))
            .Cast<ParamNames>()
            .ToDictionary(paramName => paramName, paramName => new Dictionary<long, ParamEdit>());

    public Dictionary<ParamNames, int> EditCountsByName()
    {
        return this.paramEdits.ToDictionary(t => t.Key, t => t.Value.Count);
    }

    public bool VerifyItemLots()
    {
        List<ParamEdit> enemyLots = this.paramEdits[ParamNames.ItemLotParam_enemy].Values.ToList();
        List<ParamEdit> mapLots = this.paramEdits[ParamNames.ItemLotParam_map].Values.ToList();

        HashSet<int> preExistingIds = dataAccess.ItemLotParamMap.GetAll().SelectMany(s => new List<int>() { s.lotItemId01, s.lotItemId02, s.lotItemId03, s.lotItemId04, s.lotItemId05, s.lotItemId06, s.lotItemId07, s.lotItemId08 })
            .Concat(dataAccess.ItemLotParamEnemy.GetAll().SelectMany(s => new List<int>() { s.lotItemId01, s.lotItemId02, s.lotItemId03, s.lotItemId04, s.lotItemId05, s.lotItemId06, s.lotItemId07, s.lotItemId08 }))
            .ToHashSet();

        HashSet<long> lotItemIds = this.paramEdits
            .Where(d => d.Key == ParamNames.EquipParamWeapon || d.Key == ParamNames.EquipParamProtector || d.Key == ParamNames.EquipParamAccessory)
            .SelectMany(p => p.Value.Keys)
            .ToHashSet();

        HashSet<int> expectedIds = enemyLots
            .Concat(mapLots)
            .SelectMany(d => Enumerable.Range(1, 8).Select(s => d.ParamObject.GetValue<int>($"lotItemId0{s}")))
            .Where(d => d > 0)
            .ToHashSet();

        List<long> itemIdsNotGeneratedForItemLots = lotItemIds.Where(d => !expectedIds.Contains((int)d) && !preExistingIds.Contains((int)d)).ToList();
        List<int> itemIdsNotInItemLots = expectedIds.Where(d => !lotItemIds.Contains(d) && !preExistingIds.Contains(d)).ToList();

        StringBuilder errorMessages = new();

        if (itemIdsNotInItemLots.Any())
        {
            errorMessages.AppendLine($"Generated Item Ids ({string.Join(",", itemIdsNotInItemLots)}) that don't exist in Item Lots");
        }

        if (itemIdsNotGeneratedForItemLots.Any())
        {
            errorMessages.AppendLine($"Item lots referencing Ids ({string.Join(",", itemIdsNotGeneratedForItemLots)}) that were not generated");
        }

        List<int> duplicatedEnemyLotIds = enemyLots
            .GroupBy(d => d.ParamObject.ID)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicatedEnemyLotIds.Any())
        {
            errorMessages.AppendLine($"Enemy Item lots are duplicated: {string.Join(",", duplicatedEnemyLotIds)}");
        }

        List<int> duplicatedMapLotIds = mapLots
            .GroupBy(d => d.ParamObject.ID)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicatedMapLotIds.Any())
        {
            errorMessages.AppendLine($"Map Item lots are duplicated: {string.Join(",", duplicatedMapLotIds)}");
        }

        List<int> mapLotsWithoutFlagIds = mapLots
            .Where(d => d.ParamObject.GetValue<int>("getItemFlagId") <= 0)
            .Select(d => d.ParamObject.ID)
            .ToList();

        if (mapLotsWithoutFlagIds.Any())
        {
            errorMessages.AppendLine($"Map lots without acquisition flag Ids: {string.Join(",", mapLotsWithoutFlagIds)}");
        }

        if (errorMessages.Length > 0)
        {
            logger.LogError(errorMessages.ToString());
            throw new Exception($"Item lot verification failed with the following errors:\n{errorMessages}");
        }

        return true;
    }

    public bool ContainsParamEdit(ParamNames paramName, long Id)
    {
        return this.paramEdits[paramName].ContainsKey(Id);
    }

    public bool TryGetParamEdit(ParamNames paramName, long Id, out ParamEdit? paramEdit)
    {
        return this.paramEdits[paramName].TryGetValue(Id, out paramEdit);
    }

    public void AddParamEdit(ParamEdit paramEdit)
    {
        if (paramEdit.ParamObject != null && this.ContainsParamEdit(paramEdit.ParamName, paramEdit.ParamObject.ID))
        {
            logger.LogError($"Attempting to add param {paramEdit.ParamName} edit with Id {paramEdit.ParamObject.ID} that already exists");
            throw new Exception($"Attempting to add param {paramEdit.ParamName} edit with Id {paramEdit.ParamObject.ID} that already exists");
        }

        if (progressTracker != null)
        {
            switch(paramEdit.ParamName)
            {
                case ParamNames.EquipParamWeapon:
                    progressTracker.GeneratedWeapons += 1;
                    break;
                case ParamNames.EquipParamProtector:
                    progressTracker.GeneratedArmor += 1;
                    break;
                case ParamNames.EquipParamAccessory:
                    progressTracker.GeneratedTalismans += 1;
                    break;
                case ParamNames.ItemLotParam_enemy:
                    progressTracker.GeneratedEnemyItemLots += 1;
                    break;
                case ParamNames.ItemLotParam_map:
                    progressTracker.GeneratedMapItemLots += 1;
                    break;
            }
        }

        this.paramEdits[paramEdit.ParamName][paramEdit.ParamObject.ID] = paramEdit;
    }

    public async Task ApplyEditsToRegulationBinAsync(string writePath)
    {
        logger.LogInformation($"Applying adds and edits to regulation bin");
        await Parallel.ForEachAsync(this.paramEdits.Keys.Where(d => d != ParamNames.TextOnly), (paramName, c) =>
        {
            logger.LogInformation($"Applying changes to {paramName}");

            (int updatedRows, int addedRows) = regulationBin.AddOrUpdateRows(Enum.Parse<DataSourceNames>(paramName.ToString()), this.paramEdits[paramName].Values);

            logger.LogInformation($"Applied {updatedRows} updates and {addedRows} adds for {paramName}");

            return ValueTask.CompletedTask;
        });

        logger.LogInformation($"Writing regulation bin to {writePath}");
        regulationBin.SaveRegulationBin(writePath);
    }

    public List<ParamEdit> GetParamEdits(ParamOperation? operation = null, string? paramName = null)
    {
        IEnumerable<ParamEdit> edits = [.. this.paramEdits.Values.SelectMany(d => d.Values)];

        if (operation != null)
        {
            edits = edits.Where(d => d.Operation == operation);
        }

        if (paramName != null)
        {
            edits = edits.Where(d => d.Equals(paramName));
        }

        return edits.ToList();
    }
}
