using Newtonsoft.Json;
using MauiApp1.Converters;

namespace MauiApp1.Models;

internal static class ProjectManager
{
    private static readonly string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "projects.json");
    //private static readonly string filePath = Path.Combine(FileSystem.AppDataDirectory, "projects.json");

    public static List<Project> AllProjects = new List<Project>();

    public static void SaveProjects(ICollection<Project> projects)
    {
        string json = JsonConvert.SerializeObject(projects);
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
