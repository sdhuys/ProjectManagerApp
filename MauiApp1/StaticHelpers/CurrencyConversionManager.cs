using MauiApp1.Models;
using Newtonsoft.Json;

namespace MauiApp1.StaticHelpers;

public static class CurrencyConversionManager
{
#if DEBUG
    private static readonly string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "currencyConversions.json");
#else
    private static readonly string filePath = Path.Combine(FileSystem.AppDataDirectory, "currencyConversions.json");
#endif

    public static List<CurrencyConversion> AllConversions { get; set; } = new();

    public static List<CurrencyConversion> LoadFromJson()
    {
        if (!File.Exists(filePath))
            return new List<CurrencyConversion>();

        string json = File.ReadAllText(filePath);

        if (string.IsNullOrWhiteSpace(json))
            return new List<CurrencyConversion>();

        AllConversions = JsonConvert.DeserializeObject<IEnumerable<CurrencyConversion>>(json).OrderBy(x => x.Date).ToList();
        return AllConversions;
    }

    public static void WriteToJson()
    {
        string json = JsonConvert.SerializeObject(AllConversions);
        File.WriteAllText(filePath, json);
    }
}
