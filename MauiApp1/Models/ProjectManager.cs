using Newtonsoft.Json;
using MauiApp1.Converters;

namespace MauiApp1.Models;

internal static class ProjectManager
{
#if DEBUG
    private static readonly string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "projects.json");
#else
    private static readonly string filePath = Path.Combine(FileSystem.AppDataDirectory, "projects.json");
#endif

    public static List<Project> AllProjects = new List<Project>();

    public static void SaveProjects(IEnumerable<Project> projects)
    {
        var settings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new DecimalJsonConverter() }
        };

        string json = JsonConvert.SerializeObject(projects, settings);
        File.WriteAllText(filePath, json);
    }

    public static IEnumerable<Project> LoadProjects()
    {
        if (!File.Exists(filePath))
            return Enumerable.Empty<Project>();

        string json = File.ReadAllText(filePath);

        if (String.IsNullOrWhiteSpace(json))
            return Enumerable.Empty<Project>();

        // Use custom converter that returns existing Agent if found
        // Prevents mismatch between Agent property of Project and List<Agent> of Settings
        var settings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new AgentJsonConverter() }
        };

        return JsonConvert.DeserializeObject<List<Project>>(json, settings);
    }
}
