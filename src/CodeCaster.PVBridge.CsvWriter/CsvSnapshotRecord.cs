using CsvHelper.Configuration.Attributes;
using System;

namespace CodeCaster.PVBridge.CsvWriter
{
    internal class CsvSnapshotRecord
    {
        [Name("Date and time")]
        public DateTime DateTime { get; set; }

        [Name("Daily generation (kWh)")]
        public int DailyGeneration { get; set; }
        
        [Name("Actual power (W)")]
        public int ActualPower { get; set; }
        
        [Name("Volt (AC)")]
        public double VoltAc { get; set; }
        
        [Name("Temperature (C)")]
        public double Temperature { get; set; }
    }
}
