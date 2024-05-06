using Newtonsoft.Json;
using MauiApp1.Converters;

namespace MauiApp1.Models;

public class ProjectJsonIOManager
{
#if DEBUG
    private readonly string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "projects.json");
#else
    private static readonly string filePath = Path.Combine(FileSystem.AppDataDirectory, "projects.json");
#endif

    public void SaveProjects(IEnumerable<Project> projects)
    {
        string json = JsonConvert.SerializeObject(projects);
        File.WriteAllText(filePath, json);
    }

    public IEnumerable<Project> LoadProjects()
    {
        if (!File.Exists(filePath))
            return Enumerable.Empty<Project>();

        string json = File.ReadAllText(filePath);

        if (string.IsNullOrWhiteSpace(json))
            return Enumerable.Empty<Project>();

        // Use custom converter that returns existing Agent if found
        // Prevents mismatch between Agent property of Project and List<Agent> of Settings
        var settings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new AgentJsonConverter(), new ProjectExpenseJsonConverter() }
        };

        return JsonConvert.DeserializeObject<List<Project>>(json, settings);
    }
}
