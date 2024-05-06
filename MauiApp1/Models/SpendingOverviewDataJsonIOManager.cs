using MauiApp1.Converters;
using Newtonsoft.Json;

namespace MauiApp1.Models;

// Class that handles (de)serialization of data for SpendingOverviewViewModel
public class SpendingOverviewDataJsonIOManager
{
#if DEBUG
    private readonly string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "spending.json");
#else
    private readonly string filePath = Path.Combine(FileSystem.AppDataDirectory, "spending.json");
#endif

    // Writes SpendingCategories on the first line
    // SpendingCategories on the second line
    // Dictionary for FinalizedMonthsDictionary on the third line
    public async Task WriteToJsonAsync(IEnumerable<SpendingCategory> spendings, IEnumerable<SpendingCategory> savings, Dictionary<string, (bool, decimal)> dict)
    {
        string spendingsJson = JsonConvert.SerializeObject(spendings);
        string savingsJson = JsonConvert.SerializeObject(savings);
        string dictJson = JsonConvert.SerializeObject(dict);

        // Combine spending and savings JSON into a single string with a newline separator
        string combinedJson = $"{spendingsJson}{Environment.NewLine}{savingsJson}{Environment.NewLine}{dictJson}";

        await File.WriteAllTextAsync(filePath, combinedJson);
    }

    public (IEnumerable<SpendingCategory> spendings, IEnumerable<SpendingCategory> savings, Dictionary<string, (bool, decimal)> dict) LoadFromJson()
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
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new TransferTransactionJsonConverter() }
            };

            return string.IsNullOrWhiteSpace(json) ? Enumerable.Empty<SpendingCategory>() : JsonConvert.DeserializeObject<IEnumerable<SpendingCategory>>(json, settings);
        }

        Dictionary<string, (bool, decimal)> DeserializeDictionary(string json)
        {
            return string.IsNullOrWhiteSpace(json) ? new Dictionary<string, (bool, decimal)>() : JsonConvert.DeserializeObject<Dictionary<string, (bool, decimal)>>(json);
        }
    }
    public async Task<(IEnumerable<SpendingCategory> spendings, IEnumerable<SpendingCategory> savings, Dictionary<string, (bool, decimal)> dict)> LoadFromJsonAsync()
    {
        if (!File.Exists(filePath))
            return (Enumerable.Empty<SpendingCategory>(), Enumerable.Empty<SpendingCategory>(), new());

        var lines = await File.ReadAllLinesAsync(filePath);

        return (
            lines.Length > 0 ? await DeserializeCategories(lines[0]) : null,
            lines.Length > 1 ? await DeserializeCategories(lines[1]) : null,
            lines.Length > 2 ? await DeserializeDictionary(lines[2]) : null
        );

        async Task<IEnumerable<SpendingCategory>> DeserializeCategories(string json)
        {
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new TransferTransactionJsonConverter() }
            };

            return string.IsNullOrWhiteSpace(json) ? Enumerable.Empty<SpendingCategory>() : await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<IEnumerable<SpendingCategory>>(json, settings));
        }

        async Task<Dictionary<string, (bool, decimal)>> DeserializeDictionary(string json)
        {
            return string.IsNullOrWhiteSpace(json) ? new Dictionary<string, (bool, decimal)>() : await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<Dictionary<string, (bool, decimal)>>(json));
        }
    }

}
