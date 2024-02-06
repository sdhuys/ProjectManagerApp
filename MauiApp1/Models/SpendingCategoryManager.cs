using Newtonsoft.Json;

namespace MauiApp1.Models;

public static class SpendingCategoryManager
{
#if DEBUG
    private static readonly string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "spending.json");
#else
    private static readonly string filePath = Path.Combine(FileSystem.AppDataDirectory, "spending.json");
#endif

    public static IEnumerable<SpendingCategory> LoadFromJson()
    {
        if (!File.Exists(filePath))
            return Enumerable.Empty<SpendingCategory>();

        string json = File.ReadAllText(filePath);

        if (String.IsNullOrWhiteSpace(json))
            return Enumerable.Empty<SpendingCategory>();

        return JsonConvert.DeserializeObject<IEnumerable<SpendingCategory>>(json);
    }

    public static void WriteToJson(IEnumerable<SpendingCategory> expenses)
    {
        var settings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new DecimalJsonConverter() }
        };

        string json = JsonConvert.SerializeObject(expenses, settings);
        File.WriteAllText(filePath, json);
    }
}
