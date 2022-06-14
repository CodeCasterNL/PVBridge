using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeCaster.GoodWe.Json
{
    /// <summary>
    /// The Code property from a GoodWe API response can be an int (100001: No access, please login.), an int in a string ("0"), or an error string ("innerexception").
    /// 
    /// This converter converts them all to string. Without it, System.Text.Json complains about an int (100001, 1000002, 100005, ...) not being a string.
    /// </summary>
    public class CodeConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString() ?? throw new ArgumentNullException(null, "Unexpected empty JSON token");
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetInt32().ToString();
            }

            throw new JsonException($"Cannot convert token type {reader.TokenType} to string or int");
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}
