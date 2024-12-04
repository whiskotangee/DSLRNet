namespace DSLRNet.Core.Common;

public class Range<T>(T min, T max)
{
    public static Range<int> PercentRange = new(0, 101);

    public T Min { get; set; } = min;

    public T Max { get; set; } = max;
}
