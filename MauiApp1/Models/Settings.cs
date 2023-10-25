using MauiApp1.Converters;
using Newtonsoft.Json;

namespace MauiApp1.Models;
public static class Settings
{
    private static readonly string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "settings.json");
    //private static readonly string filePath = Path.Combine(FileSystem.AppDataDirectory, "settings.json");

    public static List<string> ProjectTypes { get; set; } = new();
    public static List<string> Currencies { get; set; } = new();
    public static List<Agent> Agents { get; set; } = new();

    public static bool AreSet
    {
        get
        {
            return File.Exists(filePath);
        }
    }

    static Settings()
    {
        LoadFromJson();
    }
    public static void LoadFromJson()
    {
        if (File.Exists(filePath))
        {
            // Use custom converter that returns existing Agent if found
            // Prevents mismatch between Agent property of Project and List<Agent> of Settings
            var jsonSettings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new AgentJsonConverter() }
            };

            string json = File.ReadAllText(filePath);
            var settings = JsonConvert.DeserializeObject<(List<string>, List<string>, List<Agent>)>(json, jsonSettings);
            ProjectTypes = settings.Item1;
            Currencies = settings.Item2;
            Agents = settings.Item3;
        }
    }

    public static void Save(List<string> projectTypes, List<string> currencies, List<Agent> agents)
    {
        string json = JsonConvert.SerializeObject((projectTypes, currencies, agents));
        File.WriteAllText(filePath, json);

        ProjectTypes = projectTypes;
        Currencies = currencies;
        Agents = agents;
    }
}
