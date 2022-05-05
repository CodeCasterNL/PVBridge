using System;
using System.Diagnostics;
using CodeCaster.PVBridge.Utils;

namespace CodeCaster.PVBridge.Output
{
    /// <summary>
    /// How the system performed at a certain point in time.
    /// </summary>
    [DebuggerDisplay("{ToString(),nq}")]
    public class Snapshot : OutputBase
    {
        /// <summary>
        /// When this status was reported from the device, or the day for the output.
        /// </summary>
        public DateTime TimeTaken { get; init; }
        
        /// <summary>
        /// The power in Watt at the moment of reporting.
        /// </summary>
        public double? ActualPower { get; set; }
        
        /// <summary>
        /// The temparature of the inverter, in degrees Celcius.
        /// </summary>
        public double? Temperature { get; set; }
        
        /// <summary>
        /// Volt Alternating Current.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public double? VoltAC { get; set; }

        public override string ToString()
        {
            return $"Snapshot: T: {TimeTaken:yyyy-MM-dd HH:mm:ss}, P: {ActualPower?.FormatWatt()}, DG: {DailyGeneration?.FormatWattHour()}, T: {Temperature:00.0}\u00B0, Vac: {VoltAC:000.00}V";
        }
    }
}
