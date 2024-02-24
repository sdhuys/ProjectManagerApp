using MauiApp1.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MauiApp1.Converters;

internal class TransferTransactionJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(TransferTransaction);
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

        string id = jsonObject["Id"]?.Value<string>();
        string source = jsonObject["Source"]?.Value<string>();
        string destination = jsonObject["Destination"]?.Value<string>();
        decimal amount = jsonObject["Amount"]?.Value<decimal>() ?? 0m;
        DateTime date = jsonObject["Date"]?.Value<DateTime>() ?? DateTime.MinValue;

        var existingTransfer = TransferTransaction.GetExistingTransfer(id);

        if (existingTransfer != null)
        {
            return existingTransfer;
        }

        // If no existing transfer, create new object
        return new TransferTransaction(id, source, amount, date, destination);

    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
    public override bool CanWrite => false; // Do not use this converter for writing

}
