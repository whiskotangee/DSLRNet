namespace DSLRNet.Core.Config;

public class Filter 
{
    public string Field { get; set; } = string.Empty;
    public FilterOperator Operator { get; set; }
    public object Value { get; set; } = string.Empty;
}

public enum FilterOperator
{
    GreaterThan,
    NotEqual,
    LessThan, 
    StartsWith, 
    EndsWith,
    NotInRange,
    InRange
}

