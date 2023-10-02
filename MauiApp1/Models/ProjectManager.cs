using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Enumeration;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace MauiApp1.Models;

internal static class ProjectManager
{
    private static readonly string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "projects.json");
    //private static readonly string filePath = Path.Combine(FileSystem.AppDataDirectory, "projects.json");

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

        return JsonConvert.DeserializeObject<List<Project>>(json);
    }
}
