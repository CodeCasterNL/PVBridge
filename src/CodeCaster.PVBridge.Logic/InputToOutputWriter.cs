using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeCaster.PVBridge.Configuration;
using CodeCaster.PVBridge.Output;
using CodeCaster.PVBridge.Utils;
using Microsoft.Extensions.Logging;

namespace CodeCaster.PVBridge.Logic
{
    /// <summary>
    /// This is the core business logic that gets DI'd once per app run.
    /// </summary>
    public class InputToOutputWriter : IInputToOutputWriter
    {
        private readonly ILogger<InputToOutputWriter> _logger;

        private readonly Dictionary<string, IDataProvider> _providers;

        private readonly IClientMessageBroker _messageBroker;

        public InputToOutputWriter(
            ILoggerFactory loggerFactory,
            IClientMessageBroker messageBroker,
            IEnumerable<IDataProvider> providers
        )
        {
            _logger = loggerFactory.CreateLogger<InputToOutputWriter>();
            _messageBroker = messageBroker;

            _providers = new CaseInsensitiveDictionary<IDataProvider>(providers.ToDictionary(p => p.Type));
        }

        public async Task<ApiResponse<Snapshot>> GetLiveSnapshotAsync(DataProviderConfiguration input, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Getting current status from {input}", input.NameOrType);

            var snapshotResponse = await GetProvider<IInputProvider>(input.Type).GetCurrentStatusAsync(input, cancellationToken);

            await _messageBroker.SnapshotReceivedAsync(snapshotResponse);

            _logger.LogTrace("Status status: {status}", snapshotResponse.Status);

            if (snapshotResponse.Status != ApiResponseStatus.Succeeded || snapshotResponse.Response == null)
            {
                _logger.LogWarning("Received error from {input}: {status}", input.NameOrType, snapshotResponse.Status);
            }

            return snapshotResponse;
        }

        public async Task<ApiResponse<IReadOnlyCollection<DaySummary>>> GetSummariesAsync(DataProviderConfiguration providerConfig, DateTime since, DateTime until, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Getting {input} summaries from {since} until {until}", providerConfig.NameOrType, since, until);

            if (since.Date == DateTime.Today || since.Date == DateTime.Today.AddDays(-1) && DateTime.Now.Hour < 6)
            {
                // Cannot get summaries for today or yesterday up till like 06:00 the next day.
                return new ApiResponse<IReadOnlyCollection<DaySummary>>(new List<DaySummary>());
            }

            var reader = GetProvider<IDataProvider>(providerConfig.Type);

            var summaries = await reader.GetSummariesAsync(providerConfig, since, until, cancellationToken);

            if (!summaries.IsSuccessful)
            {
                _logger.LogWarning("Could not retrieve input summaries from {input} between {since} and {until}", providerConfig.NameOrType, since, until);
            }

            return summaries;
        }

        public async Task<ApiResponse> WriteSnapshotAsync(string inputNameOrType, DataProviderConfiguration outputConfig, Snapshot currentStatus, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Syncing status '{currentStatus}' from {input} to {output}", currentStatus, inputNameOrType, outputConfig.NameOrType);

            var outputType = outputConfig.Type;

            var outputResponse = await GetProvider<IOutputWriter>(outputType).WriteStatusAsync(outputConfig, currentStatus, cancellationToken);

            if (outputResponse.IsSuccessful)
            {
                _logger.LogDebug("Wrote status '{currentStatus}' to {outputType}", currentStatus, outputConfig.NameOrType);
            }
            else
            {
                _logger.LogWarning("Writing status to {outputType} failed", outputConfig.NameOrType);
            }

            return outputResponse;
        }


        /// <summary>
        /// Get snapshot data for a period, usually 24 hours or today up to now.
        /// </summary>

        public async Task<ApiResponse<IReadOnlyCollection<Snapshot>>> SyncPeriodDetailsAsync(DataProviderConfiguration inputConfig, DataProviderConfiguration outputConfig, DateTime day, CancellationToken cancellationToken)
        {
            var loggableDay = day.LoggableDayName();

            var writer = GetProvider<IOutputWriter>(outputConfig.Type);

            if (!writer.CanWriteDetails(outputConfig, day))
            {
                _logger.LogWarning("Provider {output} can't write on {loggableDay}", outputConfig.NameOrType, loggableDay);

                return new ApiResponse<IReadOnlyCollection<Snapshot>>(new List<Snapshot>());
            }

            var reader = GetProvider<IInputProvider>(inputConfig.Type);

            // TODO: if we were down for less than a couple of hours, try to update with as little calls as necessary.
            // First query the output:
            //
            // https://www.pvoutput.org/help/api_specification.html#get-output-service
            // "Where no date range is specified, the most recent outputs of the system will be retrieved."
            //
            // But there can be 185 in one call at most (every 5 minutes), but it can have gaps as well. Just do the 4~6 posts for the day anyway.
            //var outputSnapshotsResponse = await writer.GetSnapshotsAsync(outputConfig, day, null, cancellationToken);    

            var start = day.Date != DateTime.Today
                ? day.Date.Date
                : day;

            var end = day.Date != DateTime.Today
                ? day.Date.AddDays(1).AddMinutes(-1)
                : DateTime.Now;

            var snapshotResponse = await GetPeriodSnapshotsAsync(inputConfig, start, end, reader, loggableDay, cancellationToken);
            
            if (!snapshotResponse.IsSuccessful)
            {
                return snapshotResponse;
            }

            var inputSnapshots = snapshotResponse.Response.ToList();

            // TODO: Math.Max(input.Resolution, output.Resolution)
            var reducedSnapshots = SnapshotReducer.GetDataForResolution(inputSnapshots, start, end, resolutionInMinutes: 5);

            if (reducedSnapshots.Count == 0)
            {
                _logger.LogWarning("No snapshot data remaining for {input} on {day} after reducing {inputCount}, maintenance day?", inputConfig.NameOrType, loggableDay, inputSnapshots.Count);

                return new ApiResponse<IReadOnlyCollection<Snapshot>>(reducedSnapshots);
            }

            _logger.LogInformation("Syncing {loggableDay} ({firstSnapshot} until {lastSnapshot}) from {input} to {output}, {inputSnapshots} input snapshots, reduced to {reducedSnapshots}",
                                   loggableDay, inputSnapshots.First().TimeTaken, inputSnapshots.Last().TimeTaken, inputConfig.NameOrType, outputConfig.NameOrType, inputSnapshots.Count, reducedSnapshots.Count);

            var writeResponse = await writer.WriteStatusesAsync(outputConfig, reducedSnapshots, cancellationToken);

            if (writeResponse.Status != ApiResponseStatus.Succeeded)
            {
                _logger.LogWarning("Writing status to {output} failed: {status}", outputConfig.Type, writeResponse.Status);

                return new ApiResponse<IReadOnlyCollection<Snapshot>>(writeResponse);
            }

            return new ApiResponse<IReadOnlyCollection<Snapshot>>(reducedSnapshots);
        }

        public bool CanWriteDetails(DataProviderConfiguration outputConfig, DateTime day)
        {
            return GetProvider<IOutputWriter>(outputConfig.NameOrType).CanWriteDetails(outputConfig, day);
        }

        public bool CanWriteSummary(DataProviderConfiguration outputConfig, DateTime day)
        {
            return GetProvider<IOutputWriter>(outputConfig.NameOrType).CanWriteSummary(outputConfig, day);
        }

        private async Task<ApiResponse<IReadOnlyCollection<Snapshot>>> GetPeriodSnapshotsAsync(DataProviderConfiguration inputConfig, DateTime start, DateTime end, IInputProvider reader, string loggableDay, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Getting snapshots from {input} from {start} till {end}", inputConfig.NameOrType, start, end);

            var snapshotsResponse = await reader.GetSnapshotsAsync(inputConfig, start, end, cancellationToken);

            if (snapshotsResponse.IsSuccessful)
            {
                return snapshotsResponse;
            }

            // Explicitly check status (IsSuccessful also checks for non-empty content).
            // No snapshots for today after sundown is not an error.
            if (snapshotsResponse.Status == ApiResponseStatus.Succeeded
                && snapshotsResponse.Response?.Any() != true)
            {
                if (start.Date != DateTime.Today.Date)
                {
                    // TODO: report to UI

                    _logger.LogWarning("No input snapshot data from {input} on {loggableDay}. Inverter offline, snowy day, maintenance?", inputConfig.NameOrType, loggableDay);
                }

                return snapshotsResponse;
            }

            // TODO: report to UI

            _logger.LogError("Error getting input from {input} on {loggableDay}: {status}", inputConfig.NameOrType, loggableDay, snapshotsResponse.Status);

            return snapshotsResponse;
        }

        /// <summary>
        /// When a day(except today)'s status is synced or is too old to sync live status, upload the summary.
        ///
        /// PVOutput generates nightly summaries, but why not send it when we have it?
        /// </summary>
        public async Task<ApiResponse> WriteDaySummaryAsync(DataProviderConfiguration outputConfig, DaySummary inputSummary, DaySummary? outputSummary, CancellationToken cancellationToken)
        {
            var writer = GetProvider<IOutputWriter>(outputConfig.Type);

            if (inputSummary.DailyGeneration > 0
                && (outputSummary == null || outputSummary.DailyGeneration < inputSummary.DailyGeneration)
                && inputSummary.Day != DateTime.Today)
            {
                _logger.LogInformation("Writing summary {summary} for {day} to {output}", inputSummary, inputSummary.Day.LoggableDayName(), outputConfig.NameOrType);

                // TODO: actually batch
                return await writer.WriteDaySummariesAsync(outputConfig, new[] { inputSummary }, cancellationToken);
            }

            return ApiResponse.Succeeded;
        }

        /// <summary>
        /// Gets a cached provider for a given input or output type (e.g. "GoodWe", "PVOutput").
        /// </summary>
        private T GetProvider<T>(string type)
            where T : IDataProvider
        {
            if (!_providers.TryGetValue(type, out var provider))
            {
                throw new ArgumentException(null, nameof(type));
            }

            if (provider is not T value)
            {
                throw new ArgumentException(null, nameof(type));
            }

            return value;
        }
    }
}
