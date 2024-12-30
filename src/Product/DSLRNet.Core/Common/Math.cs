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

    public static float GetNearestValue(float value, IEnumerable<float> values)
    {
        if (values == null || values.Count() == 0)
        {
            throw new ArgumentException("The list of values cannot be null or empty.");
        }

        float nearestValue = values.First();
        float smallestDifference = Math.Abs(nearestValue - value);

        foreach (float v in values)
        {
            float difference = Math.Abs(v - value);
            if (difference < smallestDifference)
            {
                smallestDifference = difference;
                nearestValue = v;
            }
        }

        return nearestValue;
    }

    public static Dictionary<int, int> MapToRange<T>(IEnumerable<T> values, Func<T, float> getMeasurement, Func<T, int> getKey, int targetMin, int targetMax)
    {
        List<T> sortedValues = values.OrderBy(v => getMeasurement(v)).ToList();

        Dictionary<int, int> valueToQuantile = []; 
        int numQuantiles = targetMax - targetMin + 1; 
        int numValues = sortedValues.Count; 

        for (int i = 0; i < numValues; i++) 
        { 
            int quantile = targetMin + (i * numQuantiles / numValues);
            valueToQuantile[getKey(sortedValues[i])] = quantile;
        } 
        
        return valueToQuantile;
    }
}
