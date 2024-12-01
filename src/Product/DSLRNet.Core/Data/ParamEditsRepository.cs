using System.Text;

namespace DSLRNet.Core.Data;

public class ParamEditsRepository(IDataSource<ItemLotParam_map> mapDataSource, IDataSource<ItemLotParam_enemy> enemyDataSource)
{
    private Dictionary<ParamNames, List<ParamEdit>> paramEdits { get; set; } = 
        Enum.GetValues(typeof(ParamNames)) 
            .Cast<ParamNames>()
            .ToDictionary(paramName => paramName, paramName => new List<ParamEdit>());

    public Dictionary<ParamNames, int> EditCountsByName()
    {
        return paramEdits.ToDictionary(t => t.Key, t => t.Value.Count);
    }

    public bool VerifyItemLots()
    {
        var enemyLots = paramEdits[ParamNames.ItemLotParam_enemy].ToList();
        var mapLots = paramEdits[ParamNames.ItemLotParam_map].ToList();

        var preExistingIds = mapDataSource.GetAll().SelectMany(s => new List<int>() { s.lotItemId01, s.lotItemId02, s.lotItemId03, s.lotItemId04, s.lotItemId05, s.lotItemId06, s.lotItemId07, s.lotItemId08 })
            .Concat(enemyDataSource.GetAll().SelectMany(s => new List<int>() { s.lotItemId01, s.lotItemId02, s.lotItemId03, s.lotItemId04, s.lotItemId05, s.lotItemId06, s.lotItemId07, s.lotItemId08 }))
            .ToHashSet();

        var lotItemIds = paramEdits
            .Where(d => d.Key == ParamNames.EquipParamWeapon || d.Key == ParamNames.EquipParamProtector || d.Key == ParamNames.EquipParamAccessory)
            .SelectMany(d => d.Value.Select(d => d.ParamObject.ID))
            .ToHashSet();

        var expectedIds = enemyLots
            .Concat(mapLots)
            .SelectMany(d => Enumerable.Range(1, 8).Select(s => d.ParamObject.GetValue<int>($"lotItemId0{s}")))
            .Where(d => d > 0)
            .ToHashSet();

        var itemIdsNotGeneratedForItemLots = lotItemIds.Where(d => !expectedIds.Contains(d) && !preExistingIds.Contains(d)).ToList();
        var itemIdsNotInItemLots = expectedIds.Where(d => !lotItemIds.Contains(d) && !preExistingIds.Contains(d)).ToList();

        var errorMessages = new StringBuilder();

        if (itemIdsNotInItemLots.Any())
        {
            errorMessages.AppendLine($"Generated Item Ids ({string.Join(",", itemIdsNotInItemLots)}) that don't exist in Item Lots");
        }

        if (itemIdsNotGeneratedForItemLots.Any())
        {
            errorMessages.AppendLine($"Item lots referencing Ids ({string.Join(",", itemIdsNotGeneratedForItemLots)}) that were not generated");
        }

        var duplicatedEnemyLotIds = enemyLots
            .GroupBy(d => d.ParamObject.ID)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicatedEnemyLotIds.Any())
        {
            errorMessages.AppendLine($"Enemy Item lots are duplicated: {string.Join(",", duplicatedEnemyLotIds)}");
        }

        var duplicatedMapLotIds = mapLots
            .GroupBy(d => d.ParamObject.ID)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicatedMapLotIds.Any())
        {
            errorMessages.AppendLine($"Map Item lots are duplicated: {string.Join(",", duplicatedMapLotIds)}");
        }

        var mapLotsWithoutFlagIds = mapLots
            .Where(d => d.ParamObject.GetValue<int>("getItemFlagId") <= 0)
            .Select(d => d.ParamObject.ID)
            .ToList();

        if (mapLotsWithoutFlagIds.Any())
        {
            errorMessages.AppendLine($"Map lots without acquisition flag Ids: {string.Join(",", mapLotsWithoutFlagIds)}");
        }

        if (errorMessages.Length > 0)
        {
            Log.Logger.Error(errorMessages.ToString());
            throw new Exception($"Item lot verification failed with the following errors:\n{errorMessages}");
        }

        return true;
    }

    public bool ContainsParamEdit(ParamNames paramName, long Id)
    {
        var existing = paramEdits[paramName]
            .SingleOrDefault(d => d.ParamObject.ID == Id);

        return existing != null;
    }

    public bool TryGetParamEdit(ParamNames paramName, long Id, out ParamEdit? paramEdit)
    {
        paramEdit = paramEdits[paramName]
            .SingleOrDefault(d => d.ParamObject.ID == Id);

        return paramEdit != null;
    }

    public void AddParamEdit(ParamNames name, ParamOperation operation, string massEditString, LootFMG text, GenericParam? param)
    {
        if (param != null && ContainsParamEdit(name, param.ID))
        {
            Log.Logger.Error($"Attempting to add param {name} edit with Id {param.ID} that already exists");
            throw new Exception($"Attempting to add param {name} edit with Id {param.ID} that already exists");            
        }

        paramEdits[name].Add(new ParamEdit
        {
            Operation = operation,
            ParamName = name,
            MassEditString = massEditString,
            MessageText = text,
            ParamObject = param ?? new GenericParam()
        });
    }

    public List<ParamEdit> GetParamEdits(ParamOperation? operation = null, string? paramName = null)
    {
        IEnumerable<ParamEdit> edits = [.. paramEdits.Values.SelectMany(d => d)];

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
