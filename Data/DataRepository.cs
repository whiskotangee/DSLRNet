using DSLRNet.Config;
using Serilog;

namespace DSLRNet.Data;

public enum ParamOperation { Create, MassEdit, TextOnly }

public class ParamEdit
{
    public ParamOperation Operation { get; set; }

    public string ParamName { get; set; }

    public LootFMG? MessageText { get; set; }
    public string MassEditString { get; set; }

    public GenericDictionary ParamObject { get; set; }
}

public class DataRepository
{
    private List<ParamEdit> paramEdits = [];

    public Dictionary<string, int> ParamEditCount()
    {
        return paramEdits.GroupBy(d => d.ParamName).ToDictionary(g => g.Key, g => g.Count());
    }

    public bool VerifyItemLots()
    {
        var enemyLots = paramEdits.Where(d => d.ParamName.Equals("ItemLot_Enemy", StringComparison.OrdinalIgnoreCase)).ToList();
        var mapLots = paramEdits.Where(d => d.ParamName.Equals("ItemLot_Map", StringComparison.OrdinalIgnoreCase)).ToList();
        var lotItemIds = paramEdits.Where(d => d.ParamName.Equals("EquipParamWeapon", StringComparison.OrdinalIgnoreCase)).ToList()
            .Union(paramEdits.Where(d => d.ParamName.Equals("EquipParamProtector", StringComparison.OrdinalIgnoreCase)).ToList())
            .Union(paramEdits.Where(d => d.ParamName.Equals("EquipParamAccessory", StringComparison.OrdinalIgnoreCase)).ToList())
            .Select(d => d.ParamObject.GetValue<long>("ID"));

        var expectedIds = enemyLots
            .SelectMany(d => Enumerable.Range(1, 8).Select(s => d.ParamObject.GetValue<long>($"lotItemId0{s}")))
            .Union(mapLots.SelectMany(d => Enumerable.Range(1, 8).Select(s => d.ParamObject.GetValue<long>($"lotItemId0{s}"))));

        var itemIdsNotGeneratedForItemLots = lotItemIds.Where(d => expectedIds.Contains(d)).ToList();
        var itemIdsNotInItemLots = expectedIds.Where(d => lotItemIds.Contains(d)).ToList();

        if (itemIdsNotInItemLots.Any())
        {
            Log.Logger.Error($"Generated Item Ids ({string.Join(",", itemIdsNotInItemLots)}) that don't exist in Item Lots");
        }

        if (itemIdsNotGeneratedForItemLots.Any())
        {
            Log.Logger.Error($"Item lots referencing Ids ({string.Join(",", itemIdsNotGeneratedForItemLots)}) that were not generated");
        }

        var duplicatedEnemyLotIds = enemyLots.GroupBy(d => d.ParamObject.Properties["ID"]).Where(d => d.Count() > 1).ToList();
        if (duplicatedEnemyLotIds.Any())
        {
            Log.Logger.Error($"Enemy Item lots are duplicated: {string.Join(",", duplicatedEnemyLotIds)}");
        }

        var duplicatedMapLotIds = mapLots.GroupBy(d => d.ParamObject.Properties["ID"]).Where(d => d.Count() > 1).ToList();
        if (duplicatedMapLotIds.Any())
        {
            Log.Logger.Error($"Map Item lots are duplicated: {string.Join(",", duplicatedMapLotIds)}");
        }

        if (mapLots.Any(d => d.ParamObject.GetValue<int>("getItemFlagId") <= 0))
        {
            Log.Logger.Error($"Map lots without acquisition flag Ids: {string.Join(",", mapLots.Where(d => d.ParamObject.GetValue<int>("getItemFlagId") <= 0).Select(d => d.ParamObject.GetValue<int>("ID")))}");
        }

        return !(itemIdsNotGeneratedForItemLots.Any() || itemIdsNotInItemLots.Any());
    }

    public bool ContainsParamEdit(string paramName, long Id)
    {
        return paramEdits.SingleOrDefault(d => d.ParamName == paramName && d.ParamObject.GetValue<long>("ID") == Id) != null;
    }

    public bool TryGetParamEdit(string paramName, long Id, out ParamEdit? paramEdit)
    {        
        paramEdit = paramEdits.SingleOrDefault(d => d.ParamName == paramName && d.ParamObject.GetValue<long>("ID") == Id);

        return paramEdit != null;
    }

    public void AddParamEdit(string name, ParamOperation operation, string massEditString, LootFMG text,  GenericDictionary? param)
    {
        if (param != null)
        {
            if (this.ContainsParamEdit(name, param.GetValue<long>("ID")))
            {
                Log.Logger.Error($"Attempting to add param {name} edit with Id {param.GetValue<long>("ID")} that already exists");
            }
        }

        paramEdits.Add(new ParamEdit
        {
            Operation = operation,
            ParamName = name,
            MassEditString = massEditString,
            MessageText = text,
            ParamObject = param ?? new GenericDictionary()
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
