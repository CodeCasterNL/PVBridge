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
    /// TODO: implement cache invalidation beyond retention limit (14 or 90 days) for long uptimes.
    /// TODO: save in JSON in AppData for all other occasions, see #10.
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
                    return summariesResponse;
                }

                foreach (var day in summariesResponse.Response.OrderBy(d => d.Day))
                {
                    Logger.LogDebug("Retrieved {provider} summary: {summary}", configuration.NameOrType, day);

                    // TODO: if today or yesterday and not like 06:00, reset generation to null.
                    summaries.Add(day);
                    providerSummaryCache[DateOnly.FromDateTime(day.Day)] = day;
                }
            }

            summaries = summaries.OrderBy(d => d.Day).ToList();

            return summaries;
        }

        private (List<DaySummary> cachedSummaries, DateTime? missingStart, DateTime? missingEnd) GetMissingDays(DataProviderConfiguration configuration, DateTime since, DateTime? until, ConcurrentDictionary<DateOnly, DaySummary> providerSummaryCache)
        {
            var fromDay = since.Date;
            var days = (int)Math.Ceiling(((until ?? DateTime.Now) - fromDay).TotalDays);

            if (days > 31)
            {
                throw new ArgumentException("Cannot request more than 31 days of data at once.");
            }

            var summaries = new List<DaySummary>();

            var missingDays = new List<DateTime>();

            for (int i = 0; i < days; i++)
            {
                var day = fromDay.AddDays(i);

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

            if (missingDays.Any())
            {
                var missingStart = missingDays.Min(d => d.Date);
                var missingEnd = missingDays.Max(d => d.Date);
                
                if (missingEnd.Date == missingStart)
                {
                    missingEnd = missingStart.AddDays(1).AddMinutes(-1);
                }

                return (summaries, missingStart, missingEnd);
            }
            
            // TODO: if today's summary is older than resolution, return last sync up till now.
            return (summaries, null, null);
        }

        protected abstract Task<ApiResponse<IReadOnlyCollection<DaySummary>>> GetDaySummariesAsync(DataProviderConfiguration configuration, DateTime since, DateTime? until, CancellationToken cancellationToken);
    }
}
