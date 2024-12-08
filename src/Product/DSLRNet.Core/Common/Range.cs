namespace DSLRNet.Core.Common;

public class FloatValueRange(float min, float max)
{
    public float Min { get; set; } = min;

    public float Max { get; set; } = max;
}

public class IntValueRange(int min, int max)
{
    public static IntValueRange PercentRange = new(0, 101);

    public int Min { get; set; } = min;

    public int Max { get; set; } = max;

    public static IntValueRange operator +(IntValueRange value1, IntValueRange value2)
    {
        return new IntValueRange(value1.Min + value2.Min, value1.Max + value2.Max);
    }
}
