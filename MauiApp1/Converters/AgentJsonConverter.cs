using Newtonsoft.Json;
using MauiApp1.Models;
using Newtonsoft.Json.Linq;

namespace MauiApp1.Converters;

// Custom converter to return existing Agent objects instead of creating duplicates
internal class AgentJsonConverter : JsonConverter
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(Agent);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        // Check for null value before attempting to load into JObject
        if (reader.TokenType == JsonToken.Null)
        {
            reader.Skip();
            return null;
        }

        JObject jsonObject = JObject.Load(reader);

        string name = jsonObject["Name"]?.ToString();
        decimal feeDecimal = jsonObject["FeeDecimal"]?.Value<decimal>() ?? 0;

        Agent existingAgent = Agent.GetExistingAgent(name, feeDecimal);
        if (existingAgent != null)
        {
            return existingAgent;
        }

        // If no existing agent, create new object
        return new Agent(name, feeDecimal);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
    public override bool CanWrite => false; // Do not use this converter for writing
}
