namespace DSLRNet.Core.Common;
public class FloatValueRange(float min, float max)
{
    public float Min { get; set; } = min;

    public float Max { get; set; } = max;
}

public class IntValueRange
{
    public static IntValueRange PercentRange = new(0, 101);

    public IntValueRange(int min, int max)
    {
        Min = min; Max = max;
    }

    public int Min { get; set; }

    public int Max { get; set; }

    public bool Contains(int value)
    {
        return Min <= value && value <= Max;
    }

    public static IntValueRange CreateFrom(IEnumerable<int> values)
    {
        return new IntValueRange(values.Min(), values.Max());
    }

    public IEnumerable<int> ToRangeOfValues()
    {
        if (Min == Max)
        {
            return [Min];
        }
        
        return Enumerable.Range(Min, Max - Min + 1);
    }

    public static IntValueRange operator +(IntValueRange value1, IntValueRange value2)
    {
        return new IntValueRange(value1.Min + value2.Min, value1.Max + value2.Max);
    }
}
