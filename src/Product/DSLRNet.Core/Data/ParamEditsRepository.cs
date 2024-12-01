namespace DSLRNet.Core.Data;

public class ParamEditsRepository
{
    private List<ParamEdit> paramEdits { get; set; } = [];

    public Dictionary<ParamNames, int> EditCountsByName()
    {
        return paramEdits.GroupBy(d => d.ParamName).ToDictionary(g => g.Key, g => g.Count());
    }

    public bool VerifyItemLots()
    {
        var enemyLots = paramEdits.Where(d => d.ParamName == ParamNames.ItemLotParam_enemy).ToList();
        var mapLots = paramEdits.Where(d => d.ParamName == ParamNames.ItemLotParam_map).ToList();

        var lotItemIds = paramEdits
            .Where(d => d.ParamName == ParamNames.EquipParamWeapon || d.ParamName == ParamNames.EquipParamProtector || d.ParamName == ParamNames.EquipParamAccessory)
            .Select(d => d.ParamObject.ID)
            .Distinct()
            .ToList();

        var expectedIds = enemyLots
            .Concat(mapLots)
            .SelectMany(d => Enumerable.Range(1, 8).Select(s => d.ParamObject.GetValue<int>($"lotItemId0{s}")))
            .Where(d => d > 0)
            .ToList();

        var itemIdsNotGeneratedForItemLots = lotItemIds.Where(d => !expectedIds.Contains(d)).ToList();
        var itemIdsNotInItemLots = expectedIds.Where(d => !lotItemIds.Contains(d)).ToList();

        if (itemIdsNotInItemLots.Any())
        {
            Log.Logger.Error($"Generated Item Ids ({string.Join(",", itemIdsNotInItemLots)}) that don't exist in Item Lots");
        }

        if (itemIdsNotGeneratedForItemLots.Any())
        {
            Log.Logger.Error($"Item lots referencing Ids ({string.Join(",", itemIdsNotGeneratedForItemLots)}) that were not generated");
        }

        var duplicatedEnemyLotIds = enemyLots
            .GroupBy(d => d.ParamObject.ID)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicatedEnemyLotIds.Any())
        {
            Log.Logger.Error($"Enemy Item lots are duplicated: {string.Join(",", duplicatedEnemyLotIds)}");
        }

        var duplicatedMapLotIds = mapLots
            .GroupBy(d => d.ParamObject.ID)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicatedMapLotIds.Any())
        {
            Log.Logger.Error($"Map Item lots are duplicated: {string.Join(",", duplicatedMapLotIds)}");
        }

        var mapLotsWithoutFlagIds = mapLots
            .Where(d => d.ParamObject.GetValue<int>("getItemFlagId") <= 0)
            .Select(d => d.ParamObject.ID)
            .ToList();

        if (mapLotsWithoutFlagIds.Any())
        {
            Log.Logger.Error($"Map lots without acquisition flag Ids: {string.Join(",", mapLotsWithoutFlagIds)}");
        }

        return !(itemIdsNotGeneratedForItemLots.Any() || itemIdsNotInItemLots.Any());
    }

    public bool ContainsParamEdit(ParamNames paramName, long Id)
    {
        return paramEdits.SingleOrDefault(d => d.ParamName == paramName && d.ParamObject.ID == Id) != null;
    }

    public bool TryGetParamEdit(ParamNames paramName, long Id, out ParamEdit? paramEdit)
    {
        paramEdit = paramEdits.SingleOrDefault(d => d.ParamName == paramName && d.ParamObject.ID == Id);

        return paramEdit != null;
    }

    public void AddParamEdit(ParamNames name, ParamOperation operation, string massEditString, LootFMG text, GenericParam? param)
    {
        if (param != null)
        {
            var id = param.ID;

            if (ContainsParamEdit(name, id))
            {
                Log.Logger.Error($"Attempting to add param {name} edit with Id {id} that already exists");
                throw new Exception();
            }
        }

        paramEdits.Add(new ParamEdit
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
        IEnumerable<ParamEdit> edits = [.. paramEdits];

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
