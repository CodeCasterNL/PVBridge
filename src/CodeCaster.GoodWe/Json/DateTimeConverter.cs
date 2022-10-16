using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

// How does one convert DateTime and DateTime? in a generic way? Through copy-paste it seems.
namespace CodeCaster.GoodWe.Json
{ 
    public class NullableDateTimeConverter : JsonConverter<DateTime?>
    {
        // The API seems to accept this format despite user settings.
        private const string DefaultWriteFormat = "MM'/'dd'/'yyyy";

        private readonly string[] _formats;

        public NullableDateTimeConverter(params string[] formats)
        {
            _formats = formats;
        }

        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dateString = reader.GetString();

            return string.IsNullOrWhiteSpace(dateString)
                ? null
                : DateTime.ParseExact(dateString, _formats, CultureInfo.InvariantCulture, DateTimeStyles.None);
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value?.ToUniversalTime().ToString(DefaultWriteFormat));
        }
    }

    public class DateTimeConverter : JsonConverter<DateTime>
    {
        // The API seems to accept this format despite user settings.
        private const string DefaultWriteFormat = "MM'/'dd'/'yyyy";

        private readonly string[] _formats;

        public DateTimeConverter(params string[] formats)
        {
            _formats = formats;
        }

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dateString = reader.GetString();

            return string.IsNullOrWhiteSpace(dateString)
                ? throw new ArgumentNullException(null, "Unexpected empty token")
                : DateTime.ParseExact(dateString, _formats, CultureInfo.InvariantCulture, DateTimeStyles.None);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToUniversalTime().ToString(DefaultWriteFormat));
        }
    }
}
