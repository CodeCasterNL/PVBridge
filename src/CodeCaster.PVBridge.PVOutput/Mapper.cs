using System;
using CodeCaster.PVBridge.Output;
using PVOutput.Net.Builders;
using PVOutput.Net.Objects;

namespace CodeCaster.PVBridge.PVOutput
{
    internal static class Mapper
    {
        public static TStatusPost Map<TStatusPost>(StatusPostBuilder<TStatusPost> builder, Snapshot snapshot)
            where TStatusPost : class, IBatchStatusPost
        {
            builder.SetTimeStamp(snapshot.TimeTaken)
                   .SetGeneration((int?)snapshot.DailyGeneration, (int?)snapshot.ActualPower);

            if (snapshot.Temperature.HasValue)
            {
                builder.SetTemperature((decimal)snapshot.Temperature);
            }

            if (snapshot.VoltAC.HasValue)
            {
                builder.SetVoltage((decimal)snapshot.VoltAC);
            }

            return builder.BuildAndReset();
        }

        public static IOutputPost Map(OutputPostBuilder builder, DaySummary snapshot)
        {
            if (!snapshot.DailyGeneration.HasValue)
            {
                throw new ArgumentException($"Cannot report a null {nameof(snapshot.DailyGeneration)}", nameof(snapshot));
            }
            
            // TODO: more stats
            return builder.SetDate(snapshot.Day)
                          .SetEnergyGenerated((int)snapshot.DailyGeneration.Value)
                          .BuildAndReset();
        }

        public static Snapshot Map(IStatusHistory status)
        {
            return new()
            {
                TimeTaken = status.StatusDate,
                ActualPower = status.InstantaneousPower,
                DailyGeneration = status.EnergyGeneration,
                Temperature = (double?)status.Temperature
            };
        }

        internal static DaySummary Map(IOutputPost summary)
        {
            return new()
            {
                Day = summary.OutputDate.Date,
                DailyGeneration = summary.EnergyGenerated,
            };
        }
    }
}
