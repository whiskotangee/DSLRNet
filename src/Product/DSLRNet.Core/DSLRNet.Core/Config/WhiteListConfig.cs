namespace DSLRNet.Core.Config;

public class WhiteListConfigItem
{
    public int Id { get; set; }

    public string RealName { get; set; }

    public LootIds LootIds { get; set; }

    public LootWeights LootWeights { get; set; }
}

public class LootWeights
{
    public List<int> Weapon { get; set; } = [];

    public List<int> Armor { get; set; } = [];

    public List<int> Talisman { get; set; } = [];
}

public class LootIds
{
    public List<int> Weapon { get; set; } = [];

    public List<int> Armor { get; set; } = [];

    public List<int> Talisman { get; set; } = [];
}

public class AllowListConfig
{
    public List<WhiteListConfigItem> Configs { get; set; }
}
