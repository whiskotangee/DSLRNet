namespace DSLRNet.Core.Common;

public static class MathFunctions
{
    public static double RoundToXDecimalPlaces(double value, int decimalPlaces = 2)
    {
        double multiplier = (double)Math.Pow(10, decimalPlaces);
        return (double)Math.Round(value * multiplier) / multiplier;
    }

    public static int NextPowerOfTwo(int value)
    {
        if (value < 1)
            return 1;
        value--;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        return value + 1;
    }

    public static Dictionary<float, int> MapToRange(List<double> values, int targetMin, int targetMax)
    {
        double sourceMin = values.Min();
        double sourceMax = values.Max();
        return values.Distinct().ToDictionary(t => (float)t, t => MapValue(t, sourceMin, sourceMax, targetMin, targetMax));
    }

    public static int MapValue(double value, double sourceMin, double sourceMax, int targetMin, int targetMax)
    {
        return (int)((value - sourceMin) / (sourceMax - sourceMin) * (targetMax - targetMin) + targetMin);
    }
}
