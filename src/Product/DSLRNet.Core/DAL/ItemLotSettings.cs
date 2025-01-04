namespace DSLRNet.Core.DAL;
using IniParser;

public enum GameStage { Early, Mid, Late, End }

public class GameStageConfig
{
    public GameStage Stage { get; set; }

    public HashSet<int> ItemLotIds { get; set; } = [];

    public HashSet<int> AllowedRarities { get; set; } = [];
}

public class ItemLotSettings
{
    public void Save(string file)
    {
        DslItemLotSetup setup = JsonConvert.DeserializeObject<DslItemLotSetup>(JsonConvert.SerializeObject(this)) ?? throw new Exception($"Could not deserialize item lot settings into DslItemLotSetup");
        setup.AllowedRaritiesEarly = this.GameStageConfigs[GameStage.Early].AllowedRarities.ToList();
        setup.AllowedRaritiesMid = this.GameStageConfigs[GameStage.Mid].AllowedRarities.ToList();
        setup.AllowedRaritiesLate = this.GameStageConfigs[GameStage.Late].AllowedRarities.ToList();
        setup.AllowedRaritiesEnd = this.GameStageConfigs[GameStage.End].AllowedRarities.ToList();

        setup.ItemLotIdsEarly = this.GameStageConfigs[GameStage.Early].ItemLotIds.ToList();
        setup.ItemLotIdsMid = this.GameStageConfigs[GameStage.Mid].ItemLotIds.ToList();
        setup.ItemLotIdsLate = this.GameStageConfigs[GameStage.Late].ItemLotIds.ToList();
        setup.ItemLotIdsEnd = this.GameStageConfigs[GameStage.End].ItemLotIds.ToList();

        setup.LootTypeWeights = this.LootWeightsByType.Select(d => d.Weight).ToList();
        setup.WeaponTypeWeights = this.WeaponWeightsByType.Select(d => d.Weight).ToList();

        setup.Save(file);
    }

    public static ItemLotSettings Create(string file, Category category)
    {
        DslItemLotSetup? setup = DslItemLotSetup.Create(file) ?? throw new Exception($"Could not load ini file from {file}");
        ItemLotSettings? obj = JsonConvert.DeserializeObject<ItemLotSettings>(JsonConvert.SerializeObject(setup)) ?? throw new Exception($"Could not deserialize item lot settings file {file}");

        obj.GameStageConfigs = [];
        obj.GameStageConfigs.Add(GameStage.Early, new GameStageConfig
        {
            Stage = GameStage.Early,
            AllowedRarities = [.. setup.AllowedRaritiesEarly],
            ItemLotIds = [.. setup.ItemLotIdsEarly]
        });

        obj.GameStageConfigs.Add(GameStage.Mid, new GameStageConfig
        {
            Stage = GameStage.Mid,
            AllowedRarities = [.. setup.AllowedRaritiesMid],
            ItemLotIds = [.. setup.ItemLotIdsMid]
        });

        obj.GameStageConfigs.Add(GameStage.Late, new GameStageConfig
        {
            Stage = GameStage.Late,
            AllowedRarities = [.. setup.AllowedRaritiesLate],
            ItemLotIds = [.. setup.ItemLotIdsLate]
        });

        obj.GameStageConfigs.Add(GameStage.End, new GameStageConfig
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
        return GameStageConfigs[stage];
    }

    public ItemLotCategory Category { get; set; }

    public ParamNames ParamName { get; set; }

    public string NpcParamCategory { get; set; } = string.Empty;

    [JsonConverter(typeof(BoolToIntConverter))]
    public bool IsForBosses { get; set; } = false;

    public int ID { get; set; }
    public string Realname { get; set; } = string.Empty;
    public int Enabled { get; set; }

    [JsonConverter(typeof(BoolToIntConverter))]
    public bool GuaranteedDrop { get; set; }
    public float DropChanceMultiplier { get; set; }
    public Dictionary<GameStage, GameStageConfig> GameStageConfigs { get; set; } = [];
    public List<WeightedValue<LootType>> LootWeightsByType { get; set; } = [];
    public List<WeightedValue<WeaponTypes>> WeaponWeightsByType { get; set; } = [];
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
                LootTypeWeights = ParseList(data["dslitemlotsetup"]["loottypeweights"]),
                WeaponTypeWeights = ParseList(data["dslitemlotsetup"]["weapontypeweights"]),
                DropChanceMultiplier = float.Parse(data["dslitemlotsetup"]["dropchancemultiplier"]),
            };
        }
        catch (Exception)
        {
            return null;
        }
    }
    public void Save(string file)
    {
        if (string.IsNullOrEmpty(file))
        {
            throw new ArgumentException(nameof(file));
        }

        FileIniDataParser iniParser = new();
        IniParser.Model.IniData data = new();

        data["dslitemlotsetup"]["id"] = Id.ToString();
        data["dslitemlotsetup"]["realname"] = Realname;
        data["dslitemlotsetup"]["enabled"] = Enabled.ToString();
        data["dslitemlotsetup"]["itemlotids_early"] = ListToString(ItemLotIdsEarly);
        data["dslitemlotsetup"]["itemlotids_mid"] = ListToString(ItemLotIdsMid);
        data["dslitemlotsetup"]["itemlotids_late"] = ListToString(ItemLotIdsLate);
        data["dslitemlotsetup"]["itemlotids_end"] = ListToString(ItemLotIdsEnd);
        data["dslitemlotsetup"]["allowedrarities_early"] = ListToString(AllowedRaritiesEarly);
        data["dslitemlotsetup"]["allowedrarities_mid"] = ListToString(AllowedRaritiesMid);
        data["dslitemlotsetup"]["allowedrarities_late"] = ListToString(AllowedRaritiesLate);
        data["dslitemlotsetup"]["allowedrarities_end"] = ListToString(AllowedRaritiesEnd);
        data["dslitemlotsetup"]["guaranteeddrop"] = GuaranteedDrop.ToString();
        data["dslitemlotsetup"]["loottypeweights"] = ListToString(LootTypeWeights);
        data["dslitemlotsetup"]["weapontypeweights"] = ListToString(WeaponTypeWeights);
        data["dslitemlotsetup"]["dropchancemultiplier"] = DropChanceMultiplier.ToString();

        iniParser.WriteFile(file, data);
    }

    public DslItemLotSetup()
    {

    }

    public int Id { get; set; }
    public string Realname { get; set; } = string.Empty;
    public int Enabled { get; set; }
    public List<int> ItemLotIdsEarly { get; set; } = [];
    public List<int> ItemLotIdsMid { get; set; } = [];
    public List<int> ItemLotIdsLate { get; set; } = [];
    public List<int> ItemLotIdsEnd { get; set; } = [];
    public List<int> AllowedRaritiesEarly { get; set; } = [];
    public List<int> AllowedRaritiesMid { get; set; } = [];
    public List<int> AllowedRaritiesLate { get; set; } = [];
    public List<int> AllowedRaritiesEnd { get; set; } = [];
    public int GuaranteedDrop { get; set; }
    public List<int> LootTypeWeights { get; set; } = [];
    public List<int> WeaponTypeWeights { get; set; } = [];
    public float DropChanceMultiplier { get; set; }

    static List<int> ParseList(string input)
    {
        input = input.Trim('[', ']');
        List<int> result = [];
        if (!string.IsNullOrEmpty(input))
        {
            foreach (string item in input.Split(','))
            {
                string preppedItem = item.Trim(['[', ']']);
                if (!string.IsNullOrWhiteSpace(preppedItem))
                {
                    result.Add(int.Parse(preppedItem));
                }
            }
        }
        return result;
    }

    static string ListToString(List<int> list)
    {
        return $"[{string.Join(", ", list)}]";
    }
}

public class BoolToIntConverter : JsonConverter<bool>
{
    public override void WriteJson(JsonWriter writer, bool value, JsonSerializer serializer)
    {
        writer.WriteValue(value ? 1 : 0);
    }

    public override bool ReadJson(JsonReader reader, Type objectType, bool existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return reader.Value is int intValue && intValue == 1;
    }
}
