using CodeCaster.PVBridge.Output;

namespace CodeCaster.PVBridge.CsvWriter
{
    internal static class Mapper
    {
        internal static CsvSummaryRecord Map(DaySummary summary)
        {
            return new CsvSummaryRecord
            {
                Day = summary.Day,
                DailyGeneration = (int)summary.DailyGeneration.GetValueOrDefault(),
            };
        }
        internal static CsvSnapshotRecord Map(Snapshot snapshot)
        {
            return new CsvSnapshotRecord
            {
                DateTime = snapshot.TimeTaken,
                ActualPower = (int)snapshot.ActualPower.GetValueOrDefault(),
                DailyGeneration = (int)snapshot.DailyGeneration.GetValueOrDefault(),
                Temperature = snapshot.Temperature.GetValueOrDefault(),
                VoltAc = snapshot.VoltAC.GetValueOrDefault(),
            };
        }
    }
}
