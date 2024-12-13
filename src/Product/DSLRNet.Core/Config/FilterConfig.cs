namespace DSLRNet.Core.Config;

public class Filter 
{ 
    public string Field { get; set; } 
    public FilterOperator Operator { get; set; } 
    public object Value { get; set; }
}

public enum FilterOperator
{
    GreaterThan,
    NotEqual,
    LessThan, 
    StartsWith, 
    EndsWith,
    NotInRange
}

