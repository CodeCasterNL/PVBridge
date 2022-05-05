using CsvHelper.Configuration.Attributes;
using System;

namespace CodeCaster.PVBridge.CsvWriter
{
    internal class CsvSummaryRecord
    {
        public DateTime Day { get; set; }

        [Name("Daily generation (kWh)")]
        public int DailyGeneration { get; set; }
    }
}
