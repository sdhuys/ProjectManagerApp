using System.Diagnostics;
using System.Globalization;
using MauiApp1.Models;
namespace MauiApp1.Converters;
internal class AgentToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Agent agent)
        {
            var percentage = agent.FeeDecimal * 100;
            return $"{agent.Name}: {percentage:F1}%";
        }

        else if (value == null)
            return "None";

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string stringValue)
        {
            // Split the string into parts assuming it's in the format: "Name: Percentage%"
            var parts = stringValue.Split(':');
            Debug.WriteLine(parts[1]);

            if (parts.Length == 2 && decimal.TryParse(parts[1].Trim().TrimEnd('%'), out decimal percentage))
            {
                var name = parts[0].Trim();
                var feeDecimal = percentage / 100.0m;
                return new Agent(name, feeDecimal);
            }
        }

        return null;
    }
}
