using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
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
        private readonly JsonSerializerOptions _serializerOptions;

        public GoodWeFileReader(ILogger<GoodWeFileReader> logger, IClientMessageBroker messageBroker)
            : base("GoodWeFileReader", logger, messageBroker)
        {
            // TODO: refactor that DateTime mess, Json.Parse() to read from response? When present?
            // May need localization based on your account settings.
            const string dateTimeFormat = "yyyy/MM/dd HH:mm:ss";
            const string dateFormat = "yyyy/MM/dd";

            _serializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters =
                {
                    new DateTimeConverter(dateTimeFormat, dateFormat),
                    new NullableDateTimeConverter(dateTimeFormat, dateFormat),
                }
            };
        }
        
        public async Task<ApiResponse<Snapshot>> GetCurrentStatusAsync(DataProviderConfiguration configuration, CancellationToken cancellationToken)
        {
            var snapshotData = await DeserializeFileAsync<PowerStationMonitorData>(configuration, "currentStatus", cancellationToken);
            
            // Fixup the date and time so the status won't be skipped for being stale.
            if (snapshotData?.inverter.Count >= 1)
            {
                snapshotData.inverter[0].last_refresh_time = DateTime.Now;
            }

            return new ApiResponse<Snapshot>(Mapper.Map(Logger, snapshotData));
        }

        protected override async Task<ApiResponse<IReadOnlyCollection<DaySummary>>> GetDaySummariesAsync(DataProviderConfiguration configuration, DateTime since, DateTime? until, CancellationToken cancellationToken)
        {
            var summaries = await DeserializeFileAsync<ReportData>(configuration, "summaries_14d", cancellationToken);

            foreach (var row in summaries.rows)
            {
                row.date = new DateTime(since.Year, since.Month, since.Day, row.date.Hour, row.date.Minute, row.date.Second, row.date.Millisecond, row.date.Kind);
            }

            return new ApiResponse<IReadOnlyCollection<DaySummary>>(Mapper.Map(summaries));
        }

        public async Task<ApiResponse<IReadOnlyCollection<Snapshot>>> GetSnapshotsAsync(DataProviderConfiguration configuration, DateTime since, DateTime? until = null, CancellationToken cancellationToken = default)
        {
            var snapshots = await DeserializeFileAsync<ChartData>(configuration, "dayDetails", cancellationToken);
            return new ApiResponse<IReadOnlyCollection<Snapshot>>(Mapper.Map(Logger, snapshots));
        }
        
        private async Task<T> DeserializeFileAsync<T>(DataProviderConfiguration configuration, string key, CancellationToken cancellationToken)
            where T : class
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

            var response = JsonSerializer.Deserialize<ResponseBase<T>>(json, _serializerOptions);

            return response?.Data ?? throw new ArgumentException($"Could not deseriazlie {key} into a {typeof(T).Name}", nameof(key));
        }
    }
}
