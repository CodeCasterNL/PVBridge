using CodeCaster.PVBridge.Output;
using CodeCaster.PVBridge.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeCaster.PVBridge.Configuration;

namespace CodeCaster.PVBridge
{
    /// <summary>
    /// Caches day summaries per configuration.
    /// 
    /// TODO: implement cache invalidation beyond retention limit (14 or 90 days).
    /// </summary>
    public abstract class CachingSummaryProvider : IDataProvider
    {
        public string Type { get; }

        private readonly IClientMessageBroker _messageBroker;
        protected readonly ILogger<CachingSummaryProvider> Logger;

        private readonly ConcurrentDictionary<int, ConcurrentDictionary<DateOnly, DaySummary>> _summaryCache = new();

        protected CachingSummaryProvider(string type, ILogger<CachingSummaryProvider> logger, IClientMessageBroker messageBroker)
        {
            Type = type;
            Logger = logger;
            _messageBroker = messageBroker;
        }

        /// <summary>
        /// Returns cached days for the requested period, requesting summaries for missing periods.
        /// </summary>
        public async Task<ApiResponse<IReadOnlyCollection<DaySummary>>> GetSummariesAsync(DataProviderConfiguration configuration, DateTime since, DateTime? until = null, CancellationToken cancellationToken = default)
        {
            if (!_summaryCache.TryGetValue(configuration.GetHashCode(), out var providerSummaryCache))
            {
                providerSummaryCache = _summaryCache[configuration.GetHashCode()] = new ConcurrentDictionary<DateOnly, DaySummary>();
            }

            var (summaries, missingStart, missingEnd) = GetMissingDays(configuration, since, until, providerSummaryCache);

            if (missingStart.HasValue)
            {
                Logger.LogDebug("Getting {provider} summary data from {since} to {until}", configuration.NameOrType, missingStart, missingEnd);

                var summariesResponse = await GetDaySummariesAsync(configuration, missingStart.Value, missingEnd, cancellationToken);

                // Report result or error to UI
                // TODO: process response in UI
                await _messageBroker.SummariesReceivedAsync(summariesResponse);

                if (summariesResponse.Status != ApiResponseStatus.Succeeded || summariesResponse.Response == null)
                {
                    // Caller will handle.
                    return summariesResponse;
                }

                // TODO: validate output. Ignore days outside our requested range.
                foreach (var daySummary in summariesResponse.Response.OrderBy(d => d.Day))
                {
                    Logger.LogDebug("Retrieved {provider} summary: {summary}", configuration.NameOrType, daySummary);

                    daySummary.SyncedAt = DateTime.Now;

                    // TODO: if today or yesterday and not like 06:00, reset generation to null to trigger re-sync.
                    // TODO: fix overlap
                    summaries.Add(daySummary);
                    providerSummaryCache[DateOnly.FromDateTime(daySummary.Day)] = daySummary;
                }
            }

            var daySummaries = new List<DaySummary>();

            var days = since.GetDaysUntil(until ?? DateTime.Now);

            foreach (var day in days)
            {
                var daySummary = summaries.FirstOrDefault(s => s.Day == day);

                // Treat 0 as null, so we try to re-sync as long as possible, but not too often.
                if (daySummary == null || daySummary.DailyGeneration.GetValueOrDefault() == 0)
                {
                    daySummary = new DaySummary
                    {
                        Day = day,
                        DailyGeneration = null,
                    };
                }

                daySummaries.Add(daySummary);
            }

            return daySummaries;
        }

        private (List<DaySummary> cachedSummaries, DateTime? missingStart, DateTime? missingEnd) GetMissingDays(DataProviderConfiguration configuration, DateTime since, DateTime? until, ConcurrentDictionary<DateOnly, DaySummary> providerSummaryCache)
        {
            var days = since.GetDaysUntil(until ?? DateTime.Now);

            var summaries = new List<DaySummary>();

            var missingDays = new List<DateTime>();

            foreach (var day in days)
            {
                if (providerSummaryCache.TryGetValue(DateOnly.FromDateTime(day), out var summary))
                {
                    Logger.LogTrace("Adding {provider} {summary} from cache", configuration.NameOrType, summary);

                    summaries.Add(summary);
                }
                else
                {
                    Logger.LogTrace("Missing {provider} summary data for {day}", configuration.NameOrType, day.LoggableDayName());

                    missingDays.Add(day);
                }
            }

            // TODO: this can overlap (1-0-1-0-1).
            if (missingDays.Any())
            {
                var missingStart = missingDays.Min(d => d.Date);
                var missingEnd = missingDays.Max(d => d.Date);

                return (summaries, missingStart, missingEnd);
            }

            // TODO: if today's summary is older than resolution, return last sync up till now.
            return (summaries, null, null);
        }

        protected abstract Task<ApiResponse<IReadOnlyCollection<DaySummary>>> GetDaySummariesAsync(DataProviderConfiguration configuration, DateTime since, DateTime? until, CancellationToken cancellationToken);
    }
}
