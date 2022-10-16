using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CodeCaster.GoodWe.Json;
using CodeCaster.PVBridge.Configuration;
using CodeCaster.PVBridge.Output;
using Microsoft.Extensions.Logging;

namespace CodeCaster.PVBridge.GoodWe
{
    /// <summary>
    /// Reads JSON written from earlier API calls as input.
    /// </summary>
    public class GoodWeFileReader : CachingSummaryProvider, IInputProvider
    {

        public GoodWeFileReader(ILogger<GoodWeFileReader> logger, IClientMessageBroker messageBroker)
            : base("GoodWeFileReader", logger, messageBroker)
        {
        }
        
        public async Task<ApiResponse<Snapshot>> GetCurrentStatusAsync(DataProviderConfiguration configuration, CancellationToken cancellationToken)
        {
            var snapshotData = await DeserializeFileAsync<PowerStationMonitorData>(configuration, "monitorData", cancellationToken);
            
            // Fixup the date and time so the status won't be skipped for being stale.
            if (snapshotData?.inverter.Count >= 1)
            {
                snapshotData.inverter[0].last_refresh_time = DateTime.Now;
            }

            return new ApiResponse<Snapshot>(Mapper.Map(Logger, snapshotData));
        }

        protected override async Task<ApiResponse<IReadOnlyCollection<DaySummary>>> GetDaySummariesAsync(DataProviderConfiguration configuration, DateTime since, DateTime? until, CancellationToken cancellationToken)
        {
            var summaries = await DeserializeFileAsync<ReportData>(configuration, "reportData1-3", cancellationToken);
            return new ApiResponse<IReadOnlyCollection<DaySummary>>(Mapper.Map(summaries));
        }

        public async Task<ApiResponse<IReadOnlyCollection<Snapshot>>> GetSnapshotsAsync(DataProviderConfiguration configuration, DateTime since, DateTime? until = null, CancellationToken cancellationToken = default)
        {
            var snapshots = await DeserializeFileAsync<ChartData>(configuration, "stationHistoryData", cancellationToken);
            return new ApiResponse<IReadOnlyCollection<Snapshot>>(Mapper.Map(Logger, snapshots));
        }
        
        private async Task<T?> DeserializeFileAsync<T>(DataProviderConfiguration configuration, string key, CancellationToken cancellationToken)
        {
            if (!configuration.Options.TryGetValue(key, out var path))
            {
                throw new ArgumentException($"Could not find option '{key}' configuration under '{Type}'", nameof(key));
            }

            Logger.LogDebug("About to deserialize key '{key}' file '{path}' into a '{type}'", key, path, typeof(T).Name);

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException($"Configuration option for key '{key}' missing", nameof(configuration));
            }

            var json = await File.ReadAllTextAsync(path, cancellationToken);
            return System.Text.Json.JsonSerializer.Deserialize<T>(json);
        }
    }
}
