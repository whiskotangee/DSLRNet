namespace DSLRNet.Core.Data;
using IniParser;

public enum GameStage { Early, Mid, Late, End }

public class GameStageConfig
{
    public GameStage Stage { get; set; }

    public HashSet<int> ItemLotIds { get; set; }

    public HashSet<int> AllowedRarities { get; set; }
}

public class ItemLotSettings
{
    public static ItemLotSettings? Create(string file, Category category)
    {
        DslItemLotSetup? setup = DslItemLotSetup.Create(file);

        if (setup == null)
        {
            return null;
        }

        ItemLotSettings? obj = JsonConvert.DeserializeObject<ItemLotSettings>(JsonConvert.SerializeObject(setup));
        obj.GameStageConfigs = [];
        obj.GameStageConfigs.Add(new GameStageConfig
        {
            Stage = GameStage.Early,
            AllowedRarities = [.. setup.AllowedRaritiesEarly],
            ItemLotIds = [.. setup.ItemLotIdsEarly]
        });

        obj.GameStageConfigs.Add(new GameStageConfig
        {
            Stage = GameStage.Mid,
            AllowedRarities = [.. setup.AllowedRaritiesMid],
            ItemLotIds = [.. setup.ItemLotIdsMid]
        });

        obj.GameStageConfigs.Add(new GameStageConfig
        {
            Stage = GameStage.Late,
            AllowedRarities = [.. setup.AllowedRaritiesLate],
            ItemLotIds = [.. setup.ItemLotIdsLate]
        });

        obj.GameStageConfigs.Add(new GameStageConfig
        {
            Stage = GameStage.End,
            AllowedRarities = [.. setup.AllowedRaritiesEnd],
            ItemLotIds = [.. setup.ItemLotIdsEnd]
        });

        obj.Category = category.ParamCategory.Equals("ItemLotParam_Map", StringComparison.OrdinalIgnoreCase) ? ItemLotCategory.ItemLot_Map : ItemLotCategory.ItemLot_Enemy;
        obj.ParamName = Enum.Parse<ParamNames>(category.ParamCategory, true);
        obj.NpcParamCategory = category.NpcParamCategory;

        obj.LootWeightsByType =
            [
                new() { Value = LootType.Weapon, Weight = setup.LootTypeWeights[0] },
                new() { Value = LootType.Armor, Weight = setup.LootTypeWeights[1] },
                new() { Value = LootType.Talisman, Weight = setup.LootTypeWeights[2] }
            ];

        obj.WeaponWeightsByType =
            [
                new() {Value = WeaponTypes.Normal, Weight = setup.WeaponTypeWeights[0] },
                new() {Value = WeaponTypes.Shields, Weight = setup.WeaponTypeWeights[1] },
                new() {Value = WeaponTypes.StaffsSeals, Weight = setup.WeaponTypeWeights[2] },
                new() {Value = WeaponTypes.BowsCrossbows, Weight = setup.WeaponTypeWeights[3] }
            ];
        return obj;
    }

    public ItemLotSettings()
    {

    }

    public GameStageConfig GetGameStageConfig(GameStage stage)
    {
        return GameStageConfigs.Single(d => d.Stage == stage);
    }

    public GameStageConfig GetItemLotIdTier(int itemLotId = 0)
    {
        return this.GameStageConfigs.FirstOrDefault(d => d.ItemLotIds.Contains(itemLotId)) ?? this.GameStageConfigs.First();
    }

    public ItemLotCategory Category { get; set; }

    public ParamNames ParamName { get; set; }

    public string NpcParamCategory { get; set; }

    public bool IsForBosses { get; set; } = false;

    public int ID { get; set; }
    public string Realname { get; set; }
    public int Enabled { get; set; }
    public bool GuaranteedDrop { get; set; }
    public int OneTimePickup { get; set; }
    public float DropChanceMultiplier { get; set; }
    public List<GameStageConfig> GameStageConfigs { get; set; }
    public List<WeightedValue<LootType>> LootWeightsByType { get; set; }
    public List<WeightedValue<WeaponTypes>> WeaponWeightsByType { get; set; }
    public List<List<int>> NpcIds { get; set; }
    public List<List<int>> NpcItemlotids { get; set; }
    public List<int> ClearItemlotids { get; set; }
}

public enum ItemLotCategory
{
    ItemLot_Map,
    ItemLot_Enemy
}

class DslItemLotSetup
{
    public static DslItemLotSetup? Create(string file)
    {
        FileIniDataParser iniParser = new();
        IniParser.Model.IniData data = iniParser.ReadFile(file);

        try
        {
            return new DslItemLotSetup
            {
                Id = int.Parse(data["dslitemlotsetup"]["id"]),
                Realname = data["dslitemlotsetup"]["realname"],
                Enabled = int.Parse(data["dslitemlotsetup"]["enabled"]),
                ItemLotIdsEarly = ParseList(data["dslitemlotsetup"]["itemlotids_early"]),
                ItemLotIdsMid = ParseList(data["dslitemlotsetup"]["itemlotids_mid"]),
                ItemLotIdsLate = ParseList(data["dslitemlotsetup"]["itemlotids_late"]),
                ItemLotIdsEnd = ParseList(data["dslitemlotsetup"]["itemlotids_end"]),
                AllowedRaritiesEarly = ParseList(data["dslitemlotsetup"]["allowedrarities_early"]),
                AllowedRaritiesMid = ParseList(data["dslitemlotsetup"]["allowedrarities_mid"]),
                AllowedRaritiesLate = ParseList(data["dslitemlotsetup"]["allowedrarities_late"]),
                AllowedRaritiesEnd = ParseList(data["dslitemlotsetup"]["allowedrarities_end"]),
                GuaranteedDrop = int.Parse(data["dslitemlotsetup"]["guaranteeddrop"]),
                OneTimePickup = int.Parse(data["dslitemlotsetup"]["onetimepickup"]),
                LootTypeWeights = ParseList(data["dslitemlotsetup"]["loottypeweights"]),
                WeaponTypeWeights = ParseList(data["dslitemlotsetup"]["weapontypeweights"]),
                DropChanceMultiplier = float.Parse(data["dslitemlotsetup"]["dropchancemultiplier"]),
                NpcIds = ParseNestedList(data["dslitemlotsetup"]["npc_ids"]),
                NpcItemLotIds = ParseNestedList(data["dslitemlotsetup"]["npc_itemlotids"])
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    protected DslItemLotSetup()
    {

    }

    public int Id { get; set; }
    public string Realname { get; set; }
    public int Enabled { get; set; }
    public List<int> ItemLotIdsEarly { get; set; }
    public List<int> ItemLotIdsMid { get; set; }
    public List<int> ItemLotIdsLate { get; set; }
    public List<int> ItemLotIdsEnd { get; set; }
    public List<int> AllowedRaritiesEarly { get; set; }
    public List<int> AllowedRaritiesMid { get; set; }
    public List<int> AllowedRaritiesLate { get; set; }
    public List<int> AllowedRaritiesEnd { get; set; }
    public int GuaranteedDrop { get; set; }
    public int OneTimePickup { get; set; }
    public List<int> LootTypeWeights { get; set; }
    public List<int> WeaponTypeWeights { get; set; }
    public float DropChanceMultiplier { get; set; }
    public List<List<int>> NpcIds { get; set; }
    public List<List<int>> NpcItemLotIds { get; set; }

    static List<int> ParseList(string input)
    {
        input = input.Trim('[', ']');
        List<int> result = [];
        if (!string.IsNullOrEmpty(input))
        {
            foreach (string item in input.Split(','))
            {
                string preppedItem = item.Trim(new[] { '[', ']' });
                if (!string.IsNullOrWhiteSpace(preppedItem))
                {
                    result.Add(int.Parse(preppedItem));
                }
            }
        }
        return result;
    }

    static List<List<int>> ParseNestedList(string input)
    {
        input = input.Trim('[', ']');
        List<List<int>> result = [];
        if (!string.IsNullOrEmpty(input))
        {
            foreach (string item in input.Split(new[] { "], [" }, StringSplitOptions.None))
            {
                result.Add(ParseList(item));
            }
        }
        return result;
    }
}
