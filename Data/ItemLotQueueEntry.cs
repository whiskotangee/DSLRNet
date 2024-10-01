using IniParser;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace DSLRNet.Data;

public enum GameStage { Early, Mid, Late, End }

public class ItemLotIdCollection
{
    public GameStage Stage { get; set; }
    public List<int> Ids { get; set; }
}

public class GameStageConfig
{
    public GameStage Stage { get; set; }
    public List<int> ItemLotIds { get; set; }

    public List<int> AllowedRarities { get; set; }

    public int OverrideType { get; set; }
}

public class ItemLotQueueEntry
{
    public static ItemLotQueueEntry Create(string file, string category)
    {
        var setup = DslItemLotSetup.Create(file);

        var obj = JsonConvert.DeserializeObject<ItemLotQueueEntry>(JsonConvert.SerializeObject(setup));
        obj.GameStageConfigs = [];
        obj.GameStageConfigs.Add(new GameStageConfig
        {
            Stage = GameStage.Early,
            AllowedRarities = setup.AllowedRaritiesEarly,
            ItemLotIds = setup.ItemLotIdsEarly,
            OverrideType = setup.OverrideTypeEarly,
        });

        obj.GameStageConfigs.Add(new GameStageConfig
        {
            Stage = GameStage.Mid,
            AllowedRarities = setup.AllowedRaritiesMid,
            ItemLotIds = setup.ItemLotIdsMid,
            OverrideType = setup.OverrideTypeMid,
        });

        obj.GameStageConfigs.Add(new GameStageConfig
        {
            Stage = GameStage.Late,
            AllowedRarities = setup.AllowedRaritiesLate,
            ItemLotIds = setup.ItemLotIdsLate,
            OverrideType = setup.OverrideTypeLate,
        });

        obj.GameStageConfigs.Add(new GameStageConfig
        {
            Stage = GameStage.End,
            AllowedRarities = setup.AllowedRaritiesEnd,
            ItemLotIds = setup.ItemLotIdsEnd,
            OverrideType = setup.OverrideTypeEnd,
        });

        obj.BlackListIds = File.ReadAllLines($"{Path.GetDirectoryName(file)}\\ItemlotIDBlacklist.txt").Where(d => !string.IsNullOrWhiteSpace(d)).Select(long.Parse).ToList();
        obj.Category = category;

        return obj;
    }

    protected ItemLotQueueEntry()
    {
           
    }

    public List<long> BlackListIds { get; set; }
    public string Category { get; set; }
    public int Id { get; set; }
    public string Realname { get; set; }
    public int Enabled { get; set; }
    public List<int> Whitelistedlootids { get; set; }
    public bool GuaranteedDrop { get; set; }
    public int OneTimePickup { get; set; }
    public double DropChanceMultiplier { get; set; }
    public List<GameStageConfig> GameStageConfigs { get; set; }
    public List<int> LootTypeWeights { get; set; }
    public List<int> WeaponTypeWeights { get; set; }
    public List<List<int>> NpcIds { get; set; }
    public List<List<int>> NpcItemlotids { get; set; }
    public List<int> ClearItemlotids { get; set; }
}

class DslItemLotSetup
{
    public static DslItemLotSetup Create(string file)
    {
        var iniParser = new FileIniDataParser();
        var data = iniParser.ReadFile(file);
        return new DslItemLotSetup
        {
            Id = int.Parse(data["dslitemlotsetup"]["id"]),
            Realname = data["dslitemlotsetup"]["realname"],
            Enabled = int.Parse(data["dslitemlotsetup"]["enabled"]),
            WhitelistedLootIds = ParseList(data["dslitemlotsetup"]["whitelistedlootids"]),
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
            DropChanceMultiplier = double.Parse(data["dslitemlotsetup"]["dropchancemultiplier"]),
            NpcIds = ParseNestedList(data["dslitemlotsetup"]["npc_ids"]),
            NpcItemLotIds = ParseNestedList(data["dslitemlotsetup"]["npc_itemlotids"]),
            OverrideTypeEarly = int.Parse(data["dslitemlotsetup"]["overridetype_early"]),
            OverrideTypeMid = int.Parse(data["dslitemlotsetup"]["overridetype_mid"]),
            OverrideTypeLate = int.Parse(data["dslitemlotsetup"]["overridetype_late"]),
            OverrideTypeEnd = int.Parse(data["dslitemlotsetup"]["overridetype_end"]),
            ClearItemLotIds = ParseList(data["dslitemlotsetup"]["clearitemlotids"])
        };
    }

    protected DslItemLotSetup()
    {
        
    }

    public int Id { get; set; }
    public string Realname { get; set; }
    public int Enabled { get; set; }
    public List<int> WhitelistedLootIds { get; set; }
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
    public double DropChanceMultiplier { get; set; }
    public List<List<int>> NpcIds { get; set; }
    public List<List<int>> NpcItemLotIds { get; set; }
    public int OverrideTypeEarly { get; set; }
    public int OverrideTypeMid { get; set; }
    public int OverrideTypeLate { get; set; }
    public int OverrideTypeEnd { get; set; }
    public List<int> ClearItemLotIds { get; set; }

    static List<int> ParseList(string input)
    {
        input = input.Trim('[', ']');
        var result = new List<int>();
        if (!string.IsNullOrEmpty(input))
        {
            foreach (var item in input.Split(','))
            {
                var preppedItem = item.Trim(new[] { '[', ']' });
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
        var result = new List<List<int>>();
        if (!string.IsNullOrEmpty(input))
        {
            foreach (var item in input.Split(new[] { "], [" }, StringSplitOptions.None))
            {
                result.Add(ParseList(item));
            }
        }
        return result;
    }
}
