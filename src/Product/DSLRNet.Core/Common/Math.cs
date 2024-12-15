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

    public static Dictionary<float, int> MapToRange(List<float> values, int targetMin, int targetMax)
    {
        var sortedValues = values.Distinct().OrderBy(v => v).ToList(); 

        var valueToQuantile = new Dictionary<float, int>(); 
        int numQuantiles = targetMax - targetMin + 1; 
        int numValues = sortedValues.Count; 

        for (int i = 0; i < numValues; i++) 
        { 
            int quantile = targetMin + (i * numQuantiles / numValues); valueToQuantile[sortedValues[i]] = quantile;
        } 
        
        return valueToQuantile;
    }
}
