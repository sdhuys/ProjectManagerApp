using System;
namespace MauiApp1.Models;

using MauiApp1.Views;
using Newtonsoft.Json;
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
            string json = File.ReadAllText(filePath);
            var settings = JsonConvert.DeserializeObject<(List<string>, List<string>, List<Agent>)>(json);
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
