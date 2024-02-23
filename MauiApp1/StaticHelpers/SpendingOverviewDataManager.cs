using MauiApp1.Models;
using Newtonsoft.Json;

namespace MauiApp1.StaticHelpers;

// Class that handles (de)serialization of data for SpendingOverviewViewModel
public static class SpendingOverviewDataManager
{
#if DEBUG
    private static readonly string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "spending.json");
#else
    private static readonly string filePath = Path.Combine(FileSystem.AppDataDirectory, "spending.json");
#endif

    // Writes SpendingCategories on the first line
    // SpendingCategories on the second line
    // Dictionary for FinalizedMonthsDictionary on the third line
    public static async Task WriteToJson(IEnumerable<SpendingCategory> spendings, IEnumerable<SpendingCategory> savings, Dictionary<string, bool> dict)
    {
        string spendingsJson = JsonConvert.SerializeObject(spendings);
        string savingsJson = JsonConvert.SerializeObject(savings);
        string dictJson = JsonConvert.SerializeObject(dict);

        // Combine spending and savings JSON into a single string with a newline separator
        string combinedJson = $"{spendingsJson}{Environment.NewLine}{savingsJson}{Environment.NewLine}{dictJson}";

        await File.WriteAllTextAsync(filePath, combinedJson);
    }

    public static (IEnumerable<SpendingCategory> spendings, IEnumerable<SpendingCategory> savings, Dictionary<string, bool> dict) LoadFromJson()
    {
        if (!File.Exists(filePath))
            return (Enumerable.Empty<SpendingCategory>(), Enumerable.Empty<SpendingCategory>(), new());

        var lines = File.ReadLines(filePath).Take(3).ToArray();

        return (
            DeserializeCategories(lines.ElementAtOrDefault(0)),
            DeserializeCategories(lines.ElementAtOrDefault(1)),
            DeserializeDictionary(lines.ElementAtOrDefault(2))
        );

        IEnumerable<SpendingCategory> DeserializeCategories(string json)
        {
            return string.IsNullOrWhiteSpace(json) ? Enumerable.Empty<SpendingCategory>() : JsonConvert.DeserializeObject<IEnumerable<SpendingCategory>>(json);
        }

        Dictionary<string, bool> DeserializeDictionary(string json)
        {
            return string.IsNullOrWhiteSpace(json) ? new Dictionary<string, bool>() : JsonConvert.DeserializeObject<Dictionary<string, bool>>(json);
        }
    }
}
