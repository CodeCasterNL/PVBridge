using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
    ///
    /// It's the bridge between the input and the output(s).
    ///
    /// Knows about provider types (<see cref="IInputProvider"/>, <see cref="IOutputWriter"/>, <see cref="CachingSummaryProvider"/>) and gets called with input configurations to issue requests to those providers and writers.
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

            var provider = GetProviderByType<IInputProvider>(input.Type);

            var snapshotResponse = await provider.GetCurrentStatusAsync(input, cancellationToken);

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

            var summaries = await GetProviderByType<IDataProvider>(providerConfig.Type)
                .GetSummariesAsync(providerConfig, since, until, cancellationToken);

            if (!summaries.IsSuccessful)
            {
                _logger.LogWarning("Could not retrieve input summaries from {input} between {since} and {until}", providerConfig.NameOrType, since, until);
            }

            return summaries;
        }

        public async Task<ApiResponse<Snapshot>> WriteSnapshotAsync(string inputNameOrType, DataProviderConfiguration outputConfig, Snapshot currentStatus, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Syncing status '{currentStatus}' from {input} to {output}", currentStatus, inputNameOrType, outputConfig.NameOrType);

            var outputType = outputConfig.Type;

            var outputResponse = await GetProviderByType<IOutputWriter>(outputType)
                .WriteStatusAsync(outputConfig, currentStatus, cancellationToken);

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

        public async IAsyncEnumerable<(DateOnly, ApiResponse<DaySummary>)> SyncPeriodAsync(DataProviderConfiguration inputConfig, DataProviderConfiguration outputConfig, DateTime since, DateTime until, IReadOnlyCollection<DaySummary>? inputSummaries, IReadOnlyCollection<DaySummary>? outputSummaries, [EnumeratorCancellation]CancellationToken stoppingToken)
        {
            var days = since.GetDaysUntil(until).ToList();

            _logger.LogDebug("Syncing backlog from {since} to {until} ({days})", since, until, days.Count.SIfPlural("day"));

            // Flagged when the final response is an error.
            var rateLimitHit = false;

            foreach (var day in days)
            {
                // Don't allow calls after an rate limit hit.
                if (rateLimitHit) yield break;

                var dayStart = day;

                // TODO: IClock
                if (days.Count > 1 && day != days.First() && day.Date == DateTime.Today)
                {
                    // When we shut down yesterday or earlier and continue today, start syncing today at 00:00.
                    dayStart = DateTime.Today;
                }

                // TODO: when any of the days is null, let caller get latest output(s) from that day as cheaply as possible and get the total from there. See #23.
                // TODO: pass `until` so this works from command line (`sync 2022-07-23T08:00 2022-07-23T09:00`) and for today's backlog.
                var dayResponse = await SyncDayAsync(inputConfig, outputConfig, dayStart, inputSummaries, outputSummaries, stoppingToken);
                
                if (dayResponse.IsRateLimited)
                {
                    rateLimitHit = true;
                }
                
                yield return (DateOnly.FromDateTime(day), dayResponse);
            }

            _logger.LogDebug("Synced backlog from {since} to {until} ({days})", since, until, days.Count.SIfPlural("day"));
        }

        /// <summary>
        /// Get snapshot data for a period, usually an entire day or today up to now.
        /// </summary>
        public async Task<ApiResponse<IReadOnlyCollection<Snapshot>>> SyncPeriodDetailsAsync(DataProviderConfiguration inputConfig, DataProviderConfiguration outputConfig, DateTime day, bool force, CancellationToken cancellationToken)
        {
            var writer = GetProviderByType<IOutputWriter>(outputConfig.Type);

            var loggableDay = day.LoggableDayName();

            if (!CanWriteDetails(outputConfig, day))
            {
                _logger.LogWarning("Provider {output} can't write on {loggableDay}, {action}", outputConfig.NameOrType, loggableDay, force ? "forcing" : "skipping");

                if (!force)
                {
                    return new ApiResponse<IReadOnlyCollection<Snapshot>>(new List<Snapshot>());
                }
            }

            var reader = GetProviderByType<IInputProvider>(inputConfig.Type);

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
                // Error or no data.
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

            // TODO: return with SyncedAt just as in WriteSummaries
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
            return GetProviderByType<IOutputWriter>(outputConfig.Type).CanWriteDetails(outputConfig, day);
        }

        public bool CanWriteSummary(DataProviderConfiguration outputConfig, DateTime day)
        {
            return GetProviderByType<IOutputWriter>(outputConfig.Type).CanWriteSummary(outputConfig, day);
        }

        private async Task<ApiResponse<DaySummary>> SyncDayAsync(DataProviderConfiguration inputConfig, DataProviderConfiguration outputConfig, DateTime day, IReadOnlyCollection<DaySummary>? inputSummaries, IReadOnlyCollection<DaySummary>? outputSummaries, CancellationToken stoppingToken)
        {
            DaySummary? inputSummary = null;
            DaySummary? outputSummary = null;

            // Cannot get summaries for today or yesterday up till like 06:00 the next day, with some slack.
            var shouldHaveSummary = day.AddHours(33) < DateTime.Now;
            if (shouldHaveSummary)
            {
                inputSummary = inputSummaries?.FirstOrDefault(s => s.Day == day.Date);
                outputSummary = outputSummaries?.FirstOrDefault(s => s.Day == day.Date);

                // When input and output are (pretty much) equal, don't sync this day
                if (inputSummary?.DailyGeneration != null && outputSummary?.DailyGeneration != null
                    && Math.Abs(inputSummary.DailyGeneration.Value - outputSummary.DailyGeneration.Value) < 0.1d)
                {
                    _logger.LogDebug("{day} is already synced ({wH} on both sides), skipping", day.LoggableDayName(), inputSummary.DailyGeneration.Value.FormatWattHour());

                    return outputSummary;
                }
            }

            // Otherwise, sync from `day`, which can be today at any time < now - main loop interval, or any earlier day.
            var dayResult = await SyncPeriodDetailsAsync(inputConfig, outputConfig, day, force: false, stoppingToken);

            if (!dayResult.IsSuccessful)
            {
                return new(dayResult);
            }

            var newestSnapshot = dayResult.Response.OrderByDescending(s => s.TimeTaken).First();

            // When before today, report the summary (or last status), then continue.
            if (inputSummary == null || inputSummary.DailyGeneration < newestSnapshot.DailyGeneration)
            {
                inputSummary = new DaySummary
                {
                    Day = newestSnapshot.TimeTaken.Date,
                    DailyGeneration = newestSnapshot.DailyGeneration,
                    SyncedAt = DateTime.Now,
                };
            }

            var summaryResponse = await WriteDaySummaryAsync(outputConfig, inputSummary, outputSummary, stoppingToken);

            if (!summaryResponse.IsSuccessful)
            {
                _logger.LogError("Failed to write summary for {day}: {status}", day.LoggableDayName(), summaryResponse.Status);
            }

            return summaryResponse;
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
        /// PVOutput generates nightly summaries (or does it?), but why not send it when we have it?
        /// </summary>
        public async Task<ApiResponse<DaySummary>> WriteDaySummaryAsync(DataProviderConfiguration outputConfig, DaySummary inputSummary, DaySummary? outputSummary, CancellationToken cancellationToken)
        {
            var writer = GetProviderByType<IOutputWriter>(outputConfig.Type);

            if (inputSummary.DailyGeneration > 0 && outputSummary?.DailyGeneration > 0
                && Math.Abs(inputSummary.DailyGeneration.Value - outputSummary.DailyGeneration.Value) < 0.1d)
            {
                _logger.LogInformation("{summary} already known at {output} (synced at {syncedAt})", inputSummary, outputConfig.NameOrType, outputSummary.SyncedAt);

                return outputSummary;
            }

            if (!writer.CanWriteSummary(outputConfig, inputSummary.Day))
            {
                _logger.LogWarning("Cannot write {summary} to {output}", inputSummary, outputConfig.NameOrType);

                return outputSummary ?? new DaySummary
                {
                    Day = inputSummary.Day,
                    DailyGeneration = inputSummary.DailyGeneration,
                    SyncedAt = null,
                };
            }

            _logger.LogInformation("Writing {summary} to {output}", inputSummary, outputConfig.NameOrType);

            // TODO: actually batch, but we can only sync a summary after syncing an entire day, what if we're out of API calls and get shut down?
            var summaries = await writer.WriteDaySummariesAsync(outputConfig, new[] { inputSummary }, cancellationToken);

            return summaries.IsSuccessful
                ? summaries.Response.First()
                : new ApiResponse<DaySummary>(summaries);
        }

        /// <summary>
        /// Gets a cached provider for a given input or output type (e.g. "GoodWe", "PVOutput"), or throws.
        /// </summary>
        private T GetProviderByType<T>(string type)
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
