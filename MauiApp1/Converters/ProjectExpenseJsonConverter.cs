using MauiApp1.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace MauiApp1.Converters
{
    public class ProjectExpenseJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(ProjectExpense));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jsonObject = JObject.Load(reader);
            string name = jsonObject["Name"]?.ToString();
            DateTime date = jsonObject["Date"]?.Value<DateTime>() ?? DateTime.MinValue;
            decimal amount = jsonObject["Amount"]?.Value<decimal>() ?? 0;

            if (jsonObject["IsRelative"] != null && (bool)jsonObject["IsRelative"])
            {
                bool isPaid = jsonObject["IsPaid"]?.Value<bool>() ?? false;
                decimal relFeePercent = jsonObject["RelativeFeeDecimal"]?.Value<decimal>() ?? 0;
                decimal expectedAmount = jsonObject["ExpectedAmount"]?.Value<decimal>() ?? 0;

                return new ProfitSharingExpense(name, relFeePercent, date, amount, expectedAmount);
            }

            else
            {
                return new ProjectExpense(name, amount, date);
            }
        }


        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
