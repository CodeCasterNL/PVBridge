using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using CodeCaster.GoodWe.Json;
using CodeCaster.PVBridge.Output;
using Microsoft.Extensions.Logging;

namespace CodeCaster.PVBridge.GoodWe
{
    internal static class Mapper
    {
        internal static Snapshot? Map(ILogger logger, PowerStationMonitorData? monitorData)
        {
            if (monitorData == null)
            {
                return null;
            }

            var timeTaken = monitorData.inverter[0].TryGetLastRefreshDateTime();
            if (timeTaken == null)
            {
                logger.LogWarning("Cannot parse monitorData.inverter[0].last_refresh_time value of '{lastRefreshTime}' as DateTime", monitorData.inverter[0].last_refresh_time);
                return null;
            }
            
            float? actualPower = monitorData.kpi.pac;
            
            // Counters (esp. DailyGeneration) reset at midnight, don't post an all zeroes state for yesterday. Discard this result.
            if (timeTaken.Value.Date < DateTime.Now.Date)
            {
                logger.LogDebug("monitorData.inverter[0].last_refresh_time of {lastRefreshTime} is stale, discarding.", timeTaken);
                return new Snapshot
                {
                    TimeTaken = timeTaken.Value
                };
            }

            // Sometimes the last status reported for the day is 0, sometimes it isn't.
            // When stale enough (TODO: config), set to 0.
            if (timeTaken < DateTime.Now.AddMinutes(-5))
            {
                logger.LogDebug("monitorData.inverter[0].last_refresh_time of {lastRefreshTime} is stale, setting ActualPower to null.", timeTaken);
                actualPower = null;
            }

            return new Snapshot
            {
                TimeTaken = timeTaken.Value,
                ActualPower = actualPower,
                DailyGeneration = Math.Round(monitorData.kpi.power * 1000),
                Temperature = monitorData.inverter[0].tempperature,
                VoltAC = monitorData.inverter[0].d.vac1
            };
        }

        internal static IReadOnlyCollection<DaySummary>? Map(ReportData? reportData)
        {
            return reportData?.rows.Select(dp => 
                new DaySummary
                {
                    Day = dp.GetDate(), 
                    DailyGeneration = Math.Round(dp.generation * 1000)
                }).ToList();
        }

        internal static IReadOnlyCollection<Snapshot>? Map(ILogger logger, ChartData? chartData)
        {
            if (chartData == null)
            {
                return null;
            }
            
            // Each ChartTarget contains one data type and one list of data.
            var targets = chartData.Data[0].inverters[0].targets;

            var result = new Dictionary<string, Snapshot>(targets[0].datas.Length);

            // Or Zip, GroupBy, ToDictionary?

            foreach (var target in targets)
            {
                foreach (var data in target.datas)
                {
                    if (data.stat_date == null || data.value == null)
                    {
                        Debugger.Break();

                        continue;
                    }

                    if (!result.TryGetValue(data.stat_date, out var snapshot))
                    {
                        snapshot = new Snapshot
                        {
                            TimeTaken = DateTime.ParseExact(data.stat_date, "MM'/'dd'/'yyyy HH':'mm", null)
                        };
                        result[data.stat_date] = snapshot;
                    }

                    switch (target.target_key?.ToUpperInvariant())
                    {
                        case "PAC":
                            snapshot.ActualPower = float.Parse(data.value);
                            break;
                        case "EDAY":
                            snapshot.DailyGeneration = float.Parse(data.value, CultureInfo.InvariantCulture) * 1000;
                            break;
                        case "TEMPPERATURE":
                            snapshot.Temperature = float.Parse(data.value, CultureInfo.InvariantCulture);
                            break;
                        case "VAC1":
                            snapshot.VoltAC = float.Parse(data.value, CultureInfo.InvariantCulture);
                            break;
                        default:
                            logger.LogWarning("Unknown key '{target_key}' in chartData.Data[0].inverters[0].targets", target.target_key);
                            break;
                    }
                }
            }

            return result.Values;
        }
    }
}
