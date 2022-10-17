using CodeCaster.PVBridge.Configuration;
using CodeCaster.PVBridge.Output;
using CodeCaster.PVBridge.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CodeCaster.PVBridge.Logic.Status
{
    public class BacklogStatus : TaskStatus
    {
        /// <summary>
        /// The start datetime from when to start syncing. Configurable on the provider (but ignored for now). See <see cref="Configuration.InputToOutputConfiguration.SyncStart"/>
        /// </summary>
        private readonly DateTime _syncStart;

        /// <summary>
        /// Holds the _last sync_ datetime as value for the day indicated by the key.
        /// </summary>
        private readonly Dictionary<DateOnly, BacklogDayStatus> _syncedDays = new();

        private DateTime? _lastSync;

        public BacklogStatus(ILogger logger, IClock clock, DateTime syncStart, TimeSpan statusResolution)
            : base(logger, clock, statusResolution)
        {
            _syncStart = syncStart;
        }

        public (State BacklogState, ICollection<BacklogDayStatus> DaysToSync) GetBacklog()
        {
            var (state, days) = GetBacklogState();

            if (state.ShouldRetry)
            {
                return (state, Array.Empty<BacklogDayStatus>());
            }

            return (state, days);
        }

        public State HandleSummariesReadResponse(DataProviderConfiguration dataProvider, ApiResponse<IReadOnlyCollection<DaySummary>> daySummaries)
        {
            // Input summaries aren't available until after 03:00 ~ 04:00 (local) the next day.
            // Output summaries are optional. "No data" (no summary for today or yesterday) gets returned as BadRequest,
            // so only check for rate limit or error here.
            return HandleApiResponse(dataProvider, daySummaries, badRequestIsBad: false);
        }

        /// <summary>
        /// When a summary is successfully written with output, we consider the day synced.
        /// </summary>
        public State HandleDayWrittenResponse(DataProviderConfiguration outputProvider, DateOnly day, ApiResponse<DaySummary> daySummary)
        {
            var dayStatus = GetDayStatus(day);

            // In case of an error, this also triggers the provider.
            var state = HandleApiResponse(outputProvider, daySummary);

            // If we get an error trying to write the response, perhaps the day has invalid data. Or they're down. 
            if (state.ShouldRetry)
            {
                return dayStatus.AddError(Clock.Now);
            }

            if (!daySummary.IsSuccessful || !daySummary.Response.SyncedAt.HasValue)
            {
                // Couldn't or didn't write, try again later.
                return dayStatus.AddError(Clock.Now);
            }

            if (daySummary.Response.DailyGeneration.GetValueOrDefault() == 0)
            {
                // Mark stale/zero data as error on that day, we shouldn't believe those and keep retrying (slowly) as long as possible.
                // If it doesn't work the next day, then trying once or twice a day for as long as it's reachable sounds reasonable?
                dayStatus.AddError(Clock.Now);

                return UpdateState();
            }

            // Report as synced, so we don't report the same day twice.
            DaySynced(day);

            return UpdateState();
        }

        protected override State UpdateState()
            => GetBacklogState().State;

        /// <summary>
        /// Keep fast and simple.
        /// </summary>
        private (State State, ICollection<BacklogDayStatus> DaysToSync) GetBacklogState()
        {
            var backlogStart = new[] { _lastSync, _syncStart }.Max()!.Value;

            var days = backlogStart.GetDaysUntil(Clock.Now).ToList();

            var syncableBacklog = new List<BacklogDayStatus>();

            DateTime? waitUntil = null;

            foreach (var dayTime in days)
            {
                var day = DateOnly.FromDateTime(dayTime);

                var dayStatus = GetDayStatus(day);

                switch (dayStatus.State)
                {
                    case DayState.FullySynced:
                        Logger.LogTrace("Day {day} fully synced at {syncDateTime}", day.LoggableDayName(), dayStatus.SyncedAt);

                        continue;
                    case DayState.NeedsSyncing:
                        Logger.LogTrace("Day {day} missing, starting sync as soon as possible", day.LoggableDayName());

                        syncableBacklog.Add(dayStatus);

                        continue;
                    case DayState.Wait:
                        if (dayStatus.ContinueAt == null || dayStatus.ContinueAt <= Clock.Now)
                        {
                            dayStatus.SyncNow();

                            Logger.LogTrace("Day {day} is up for retry, starting sync as soon as possible", day.LoggableDayName());

                            syncableBacklog.Add(dayStatus);

                            continue;
                        }

                        if (waitUntil == null || dayStatus.ContinueAt < waitUntil)
                        {
                            waitUntil = dayStatus.ContinueAt;
                        }
                        continue;
                }

                //// When a day (including today) was synced on itself, sync it again, so we can complete yesterday's data when resuming.
                //Logger.LogTrace("{day} was last synced on {backlogSync}, continuing there", dayTime.LoggableDayName(), lastDaySnapshot);

                //_backlogStart = lastDaySnapshot;

                //return;
            }

            var state = waitUntil.HasValue
                ? State.Wait(waitUntil.Value)
                : State.Success;

            // Clone, don't let caller modify our state
            var daysToSync = syncableBacklog.Select(d => new BacklogDayStatus(d)).ToList();

            return (state, daysToSync);
        }

        /// <summary>
        /// Backlog processing done for one day, or a live status was synced.
        /// </summary>
        /// <param name="day">The day.</param>
        private void DaySynced(DateOnly day)
        {
            var dayStatus = GetDayStatus(day);

            dayStatus.Synced(Clock.Now);

            var today = DateOnly.FromDateTime(Clock.Now.Date);

            if (day != today)
            {
                return;
            }

            // For today, try to sync in a while again, assume LiveStatus keeps up.
            var hour = TimeSpan.FromHours(1);
            var nextAttempt = Clock.Now.Truncate(hour).Add(hour * 2);
            
            dayStatus.Wait(nextAttempt);
        }

        private BacklogDayStatus GetDayStatus(DateOnly day)
            => _syncedDays.TryGetValue(day, out var dayStatus)
                ? dayStatus
                : _syncedDays[day] = new BacklogDayStatus(day, DayState.NeedsSyncing);

        /// <inheritdoc />
        protected override void ResetErrorCounters()
        {
            Debugger.Break();
            
            // TODO: only reset non-synced days.
            _syncedDays.Clear();
        }

        protected override string ToLogString()
        {
            DateOnly? lastDay = null;
            DateTime? lastDaySynced = null;

            if (_syncedDays.Keys.Count > 0)
            {
                lastDay = _syncedDays.Keys.Last();
                lastDaySynced = _syncedDays[lastDay.Value].SyncedAt;
            }

            return $"BacklogStart: {_lastSync:O}, " +
                   $"most recently synced day: {lastDay.ToStringOrDefault("O", "never")} " +
                   $"at {lastDaySynced.ToIsoStringOrDefault("never")}";
        }
    }
}
