using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeCaster.GoodWe.Json
{
    public class SystemTextDateTimeConverter : JsonConverter<DateTime?>
    {
        private readonly string _format;

        public SystemTextDateTimeConverter() : this("MM'/'dd'/'yyyy") { }

        public SystemTextDateTimeConverter(string format) { _format = format; }

        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dateString = reader.GetString();
            
            if (!string.IsNullOrWhiteSpace(dateString)
                && DateTime.TryParseExact(dateString, _format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
            {
                return dateTime;
            }

            return null;
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value?.ToUniversalTime().ToString(_format));
        }
    }
}
