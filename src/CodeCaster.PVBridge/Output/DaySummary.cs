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

        /// <summary>
        /// When it was synced, context-dependent.
        /// </summary>
        public DateTime? SyncedAt { get; set; }

        public override string ToString()
        {
            return $"DaySummary: D: {Day.LoggableDayName()}, Generation: " + (DailyGeneration?.FormatWattHour() ?? "(null)");
        }
    }
}
