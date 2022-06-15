using System;
using System.Text.Json.Serialization;

namespace CodeCaster.GoodWe.Json
{
    public class DateTimeFormatAttribute : JsonConverterAttribute
    {
        private readonly string _format;

        public DateTimeFormatAttribute(string format)
        {
            _format = format;
        }

        public override JsonConverter CreateConverter(Type typeToConvert)
        {
            if (typeToConvert == typeof(DateTime?))
            {
                return new NullableDateTimeConverter(this._format);
            }
            else if (typeToConvert == typeof(DateTime))
            {
                return new DateTimeConverter(this._format);
            }

            throw new ArgumentException("Cannot create converter for type " + typeToConvert, nameof(typeToConvert));
        }
    }
}
