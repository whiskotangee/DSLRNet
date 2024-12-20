namespace DSLRNet.Converters;

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

public class HexStringToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string hexString)
        {
            try
            {
                if (!hexString.StartsWith("#"))
                {
                    hexString = $"#{hexString}";
                }

                return (Color)ColorConverter.ConvertFromString(hexString);
            }
            catch (FormatException)
            {
                return Colors.Transparent; // Default color in case of invalid input
            }
        }
        return Colors.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Color color)
        {
            return $"{color.R:X2}{color.G:X2}{color.B:X2}";
        }
        return string.Empty;
    }
}
