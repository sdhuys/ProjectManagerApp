using Newtonsoft.Json;

namespace MauiApp1.Models;

public class CurrencyConversionsJsonIOManager
{
#if DEBUG
    private readonly string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "currencyConversions.json");
#else
    private readonly string filePath = Path.Combine(FileSystem.AppDataDirectory, "currencyConversions.json");
#endif

    public List<CurrencyConversion> AllConversions { get; set; } = new();

    public List<CurrencyConversion> LoadFromJson()
    {
        if (!File.Exists(filePath))
            return new List<CurrencyConversion>();

        string json = File.ReadAllText(filePath);

        if (string.IsNullOrWhiteSpace(json))
            return new List<CurrencyConversion>();

        AllConversions = JsonConvert.DeserializeObject<IEnumerable<CurrencyConversion>>(json).OrderBy(x => x.Date).ToList();
        return AllConversions;
    }

    public void WriteToJson()
    {
        string json = JsonConvert.SerializeObject(AllConversions);
        File.WriteAllText(filePath, json);
    }
}
