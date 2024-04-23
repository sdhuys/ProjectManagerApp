using System.Globalization;
using MauiApp1.Models;

namespace MauiApp1.Converters;
internal class AgentToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Agent agent)
        {
            if (parameter is string param && param == "nameOnly")
            {
                return agent.Name;
            }
            else
            {
                var percentage = agent.FeeDecimal * 100;
                return $"{agent.Name}: {percentage:G2}%";
            }
        }

        else if (value == null)
        {
            return "None";
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
