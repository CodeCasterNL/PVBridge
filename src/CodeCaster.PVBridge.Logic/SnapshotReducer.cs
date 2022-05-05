using System;
using System.Collections.Generic;
using System.Linq;
using CodeCaster.PVBridge.Output;

namespace CodeCaster.PVBridge.Logic
{
    public static class SnapshotReducer
    {
        /// <summary>
        /// Pigeonhole the input data to nicely rounded minutes according to the resolution of the output.
        /// </summary>
        public static IReadOnlyCollection<Snapshot> GetDataForResolution(IList<Snapshot> snapshots, DateTime start, DateTime end, int resolutionInMinutes = 5)
        {
            if ((end - start).TotalHours > 24.1)
            {
                throw new ArgumentException($"Start ({start:O}) and end ({end:O}) too far apart");
            }
            // TODO: #2
            // Report the last zero before the first non-zero of the day (which we want)
            // and report the first zero after the last non-zero.
            // So if there's downtime during the day, or when a range of zero-statuses is reported at the end of the day,
            // we report only the ones defining going down or up. 

            // For now, this skips zeroes only at the beginning of a range.
            var filteredData = snapshots.SkipWhile((snapshot, i) =>
            {
                var nextSnapshot = snapshots.Count > i + 2 ? snapshots[i + 1] : null;

                return snapshot.ActualPower == 0 && nextSnapshot?.ActualPower == 0;
            }).ToList();

            var result = new List<Snapshot>();

            var firstSnapshot = filteredData.FirstOrDefault();
            if (firstSnapshot == null)
            {
                // No data for today.
                return result;
            }

            var minuteTicks = TimeSpan.FromMinutes(1).Ticks;

            // Round start time down to resolution minutes (for 5: 12 -> 10, 7 -> 5, 15 -> 15, 4 -> 0).
            var startMinute = (start.Minute / resolutionInMinutes) * resolutionInMinutes;
            var startDateTime = start.AddMinutes(startMinute - start.Minute).AddTicks(-(start.Ticks % minuteTicks));

            // Round end time down up resolution minutes (for 5: 12 -> 15, 7 -> 10, 15 -> 15, 4 -> 5, 59 -> 60).
            var endMinute = ((end.Minute / resolutionInMinutes) * resolutionInMinutes + resolutionInMinutes);
            var endDateTime = end.AddMinutes(endMinute - end.Minute).AddTicks(-(end.Ticks % minuteTicks));

            // TODO: how did this work again?
            int minutesToAdd = 0;
            var totalMinutes = (endDateTime - startDateTime).TotalMinutes;
            do
            {
                var roundedDateTime = startDateTime.AddMinutes(minutesToAdd);
                var from = roundedDateTime.AddMinutes(-resolutionInMinutes);
                var until = roundedDateTime.AddMinutes(resolutionInMinutes);

                var nearbySnapshots = filteredData.Where(d => d.TimeTaken > from && d.TimeTaken < until)
                                                  .OrderBy(d => Math.Abs((d.TimeTaken - roundedDateTime).TotalSeconds))
                                                  .ToList();

                // Do we have any snapshot between (-resolution < N < +resolution)?
                var nearestSnapshot = nearbySnapshots.FirstOrDefault();
                if (nearestSnapshot != null)
                {
                    // TODO: interpolate if more data?
                    // TODO: if we roll over to the next day (or have any other period of 0 outputs), start looking for the first snapshot with power > 0 again?
                    result.Add(new Snapshot
                    {
                        TimeTaken = roundedDateTime,
                        ActualPower = nearestSnapshot.ActualPower,
                        DailyGeneration = nearestSnapshot.DailyGeneration,
                        VoltAC = nearestSnapshot.VoltAC,
                        Temperature = nearestSnapshot.Temperature,
                    });
                }

                minutesToAdd += resolutionInMinutes;
            }
            while (minutesToAdd <= totalMinutes);

            return result;
        }
    }
}
