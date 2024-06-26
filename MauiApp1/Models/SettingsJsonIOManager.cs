﻿using MauiApp1.Converters;
using Newtonsoft.Json;

namespace MauiApp1.Models;
public class SettingsJsonIOManager
{
#if DEBUG
        private static readonly string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "settings.json");
#else
        private static readonly string filePath = Path.Combine(FileSystem.AppDataDirectory, "settings.json");
#endif

    public bool FileExists => File.Exists(filePath);

    public SettingsJsonIOManager()
    {
        LoadFromJson();
    }
    public (IEnumerable<string>, IEnumerable<string>, IEnumerable<Agent>) LoadFromJson()
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
            var settings = JsonConvert.DeserializeObject<(IEnumerable<string>, IEnumerable<string>, IEnumerable<Agent>)>(json, jsonSettings);
            return settings;
        }
        return (null, null, null);
    }

    public void Save(List<string> projectTypes, List<string> currencies, List<Agent> agents)
    {
        string json = JsonConvert.SerializeObject((projectTypes, currencies, agents));
        File.WriteAllText(filePath, json);
    }
}
