using Newtonsoft.Json;

namespace MauiApp1.Models;

public static class CurrencyConversionManager
{
#if DEBUG
    private static readonly string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "currencyConversions.json");
#else
    private static readonly string filePath = Path.Combine(FileSystem.AppDataDirectory, "currencyConversions.json");
#endif

    public static IEnumerable<CurrencyConversion> LoadFromJson()
    {
        if (!File.Exists(filePath))
            return Enumerable.Empty<CurrencyConversion>();

        string json = File.ReadAllText(filePath);

        if (String.IsNullOrWhiteSpace(json))
            return Enumerable.Empty<CurrencyConversion>();

        return JsonConvert.DeserializeObject<IEnumerable<CurrencyConversion>>(json);
    }

    public static void WriteToJson(IEnumerable<CurrencyConversion> expenses)
    {
        string json = JsonConvert.SerializeObject(expenses);
        File.WriteAllText(filePath, json);
    }
}
