using System.Globalization;
namespace MauiApp1.Converters
{
    public class DecimalFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decimalValue)
            {
                // Check if the value has no decimal part
                if (decimalValue % 1 == 0)
                {
                    // Display without decimal point if it's .00
                    return decimalValue.ToString("N0");
                }
                else
                {
                    // Display with 2 digits after the decimal point
                    return decimalValue.ToString("N2");
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
