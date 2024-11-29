namespace DSLRNet.Core.Common;

public static class MathFunctions
{
    public static double RoundToXDecimalPlaces(double value, int decimalPlaces = 2)
    {
        double multiplier = (double)Math.Pow(10, decimalPlaces);
        return (double)Math.Round(value * multiplier) / multiplier;
    }
}
