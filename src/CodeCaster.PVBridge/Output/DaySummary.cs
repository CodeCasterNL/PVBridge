using System;
using System.Diagnostics;
using CodeCaster.PVBridge.Utils;

namespace CodeCaster.PVBridge.Output
{
    [DebuggerDisplay("{ToString(),nq}")]
    public class DaySummary : OutputBase
    {
        /// <summary>
        /// The day this summary is for.
        ///
        /// TODO: DateOnly
        /// </summary>
        public DateTime Day { get; init; }
        
        public override string ToString()
        {
            return $"DaySummary: D: {Day.LoggableDayName()}, Generation: " + (DailyGeneration?.FormatWattHour() ?? "(null)");
        }
    }
}
