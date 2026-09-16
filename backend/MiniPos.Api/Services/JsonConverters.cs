using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MiniPos.Api.Services
{
    public class DecimalJsonConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetDecimal();
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        {
            var rounded = Math.Round(value, 3, MidpointRounding.ToZero);
            writer.WriteNumberValue(rounded);
        }
    }

    public class NullableDecimalJsonConverter : JsonConverter<decimal?>
    {
        public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            return reader.GetDecimal();
        }

        public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                var rounded = Math.Round(value.Value, 3, MidpointRounding.ToZero);
                writer.WriteNumberValue(rounded);
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}
