namespace DSLRNet.Core.Common;

public class Range<T>(T min, T max)
{
    public T Min { get; set; } = min;

    public T Max { get; set; } = max;
}
