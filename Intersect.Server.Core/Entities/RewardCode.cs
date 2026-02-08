using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Intersect.Server.Entities
{
    public class RewardCode
    {
        public bool Active { get; set; }

        [Newtonsoft.Json.JsonConverter(typeof(NullableDateOnlyConverter))]
        public DateTime? ExpireDate { get; set; }

        public string Code { get; set; }
        public List<RewardItem> Items { get; set; }
        public int? Limit { get; set; }
        public List<string> LastUsages { get; set; } = new List<string>();
    }

    public class RewardItem
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
    }
}

public class NullableDateOnlyConverter : Newtonsoft.Json.JsonConverter<DateTime?>
{
    private const string Format = "dd-MM-yyyy";

    public override void WriteJson(JsonWriter writer, DateTime? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteValue(value.Value.ToString(Format, CultureInfo.InvariantCulture));
    }

    public override DateTime? ReadJson(
        JsonReader reader,
        Type objectType,
        DateTime? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        if (DateTime.TryParseExact(
            reader.Value?.ToString(),
            Format,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date))
        {
            return date;
        }

        throw new JsonSerializationException($"Invalid date format. Expected {Format}");
    }
}
