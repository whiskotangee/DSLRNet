namespace DSLRNet.Core.Data;
using System.Text;

public class ParamEditsRepository(IDataSource<ItemLotParam_map> mapDataSource, IDataSource<ItemLotParam_enemy> enemyDataSource, ILogger<ParamEditsRepository> logger)
{
    private Dictionary<ParamNames, List<ParamEdit>> paramEdits { get; set; } =
        Enum.GetValues(typeof(ParamNames))
            .Cast<ParamNames>()
            .ToDictionary(paramName => paramName, paramName => new List<ParamEdit>());

    public Dictionary<ParamNames, int> EditCountsByName()
    {
        return this.paramEdits.ToDictionary(t => t.Key, t => t.Value.Count);
    }

    public bool VerifyItemLots()
    {
        List<ParamEdit> enemyLots = this.paramEdits[ParamNames.ItemLotParam_enemy].ToList();
        List<ParamEdit> mapLots = this.paramEdits[ParamNames.ItemLotParam_map].ToList();

        HashSet<int> preExistingIds = mapDataSource.GetAll().SelectMany(s => new List<int>() { s.lotItemId01, s.lotItemId02, s.lotItemId03, s.lotItemId04, s.lotItemId05, s.lotItemId06, s.lotItemId07, s.lotItemId08 })
            .Concat(enemyDataSource.GetAll().SelectMany(s => new List<int>() { s.lotItemId01, s.lotItemId02, s.lotItemId03, s.lotItemId04, s.lotItemId05, s.lotItemId06, s.lotItemId07, s.lotItemId08 }))
            .ToHashSet();

        HashSet<int> lotItemIds = this.paramEdits
            .Where(d => d.Key == ParamNames.EquipParamWeapon || d.Key == ParamNames.EquipParamProtector || d.Key == ParamNames.EquipParamAccessory)
            .SelectMany(d => d.Value.Select(d => d.ParamObject.ID))
            .ToHashSet();

        HashSet<int> expectedIds = enemyLots
            .Concat(mapLots)
            .SelectMany(d => Enumerable.Range(1, 8).Select(s => d.ParamObject.GetValue<int>($"lotItemId0{s}")))
            .Where(d => d > 0)
            .ToHashSet();

        List<int> itemIdsNotGeneratedForItemLots = lotItemIds.Where(d => !expectedIds.Contains(d) && !preExistingIds.Contains(d)).ToList();
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
        ParamEdit? existing = this.paramEdits[paramName]
            .SingleOrDefault(d => d.ParamObject.ID == Id);

        return existing != null;
    }

    public bool TryGetParamEdit(ParamNames paramName, long Id, out ParamEdit? paramEdit)
    {
        paramEdit = this.paramEdits[paramName]
            .SingleOrDefault(d => d.ParamObject.ID == Id);

        return paramEdit != null;
    }

    public void AddParamEdit(ParamEdit paramEdit)
    {
        if (paramEdit.ParamObject != null && this.ContainsParamEdit(paramEdit.ParamName, paramEdit.ParamObject.ID))
        {
            logger.LogError($"Attempting to add param {paramEdit.ParamName} edit with Id {paramEdit.ParamObject.ID} that already exists");
            throw new Exception($"Attempting to add param {paramEdit.ParamName} edit with Id {paramEdit.ParamObject.ID} that already exists");
        }

        this.paramEdits[paramEdit.ParamName].Add(paramEdit);
    }

    public List<ParamEdit> GetParamEdits(ParamOperation? operation = null, string? paramName = null)
    {
        IEnumerable<ParamEdit> edits = [.. this.paramEdits.Values.SelectMany(d => d)];

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
