using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MauiApp1.Models
{
    internal class AgentToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Agent agent)
            {
                var percentage = agent.FeeDecimal * 100;
                return $"{agent.Name}: {percentage:F1}%";  // Use format to display percentage with 2 decimal places
            }

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
                    var feeDecimal = percentage / 100.0m;  // Calculate fee decimal
                    return new Agent(name, feeDecimal);
                }
            }

            // Return null or handle conversion failure accordingly
            return null;
        }
    }
}
