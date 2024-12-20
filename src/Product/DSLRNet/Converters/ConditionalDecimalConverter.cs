using System;
using System.Globalization;
using System.Windows.Data;

namespace DSLRNet.Converters
{
    public class ConditionalDecimalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                // Check if the value is an integer
                if (doubleValue == Math.Truncate(doubleValue))
                {
                    return doubleValue.ToString("F0", culture); // No decimal places
                }
                else
                {
                    return doubleValue.ToString("F2", culture); // Two decimal places
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
