using System;

namespace CodeCaster.PVBridge.Utils
{
    public static class ValueTypeExtensions
    {
        public static string FormatWattHour(this double wattHour) => FormatWatt(wattHour) + "h";

        public static string FormatWatt(this double watt) =>
            watt < 1000
                ? $"{watt:0} W"
                : $"{watt / 1000:0.###} kW";
        
        public static string LoggableDayName(this IFormattable value, IFormatProvider? formatProvider = null) => value.ToString("yyyy-MM-dd (ddd)", formatProvider);

        public static string? ToStringOrDefault(this DateOnly? value, string format, string? defaultValue, IFormatProvider? formatProvider = null)
            => value?.ToString(format, formatProvider) ?? defaultValue;

        public static string? ToIsoStringOrDefault(this DateTime? value, string? defaultValue, IFormatProvider? formatProvider = null)
            => value?.ToString("O", formatProvider) ?? defaultValue;
    }
}
