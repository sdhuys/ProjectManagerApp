using Newtonsoft.Json;

namespace MauiApp1.Models;

public static class SpendingCategoryManager
{
#if DEBUG
    private static readonly string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "spending.json");
#else
    private static readonly string filePath = Path.Combine(FileSystem.AppDataDirectory, "spending.json");
#endif

    // Writes SpendingCategories for SpendingCategoryViewModels on the first line
    // SpendingCategories for SavingsCategoryViewModels on the second line
    public static void WriteToJson(IEnumerable<SpendingCategory> spendings, IEnumerable<SpendingCategory> savings)
    {
        var settings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new DecimalJsonConverter() }
        };

        string spendingsJson = JsonConvert.SerializeObject(spendings, settings);
        string savingsJson = JsonConvert.SerializeObject(savings, settings);

        // Combine spending and savings JSON into a single string with a newline separator
        string combinedJson = $"{spendingsJson}{Environment.NewLine}{savingsJson}";

        File.WriteAllText(filePath, combinedJson);
    }

    public static (IEnumerable<SpendingCategory> spendings, IEnumerable<SpendingCategory> savings) LoadFromJson()
    {
        if (!File.Exists(filePath))
            return (Enumerable.Empty<SpendingCategory>(), Enumerable.Empty<SpendingCategory>());

        var lines = File.ReadLines(filePath).Take(2).ToArray();

        return (
            DeserializeCategories(lines.ElementAtOrDefault(0)),
            DeserializeCategories(lines.ElementAtOrDefault(1))
        );

        IEnumerable<SpendingCategory> DeserializeCategories(string json)
        {
            return string.IsNullOrWhiteSpace(json)
                ? Enumerable.Empty<SpendingCategory>()
                : JsonConvert.DeserializeObject<IEnumerable<SpendingCategory>>(json);
        }
    }
}
