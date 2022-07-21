using System;
using System.Collections.Generic;
using System.Linq;
using CodeCaster.PVBridge.Utils;
using Microsoft.Extensions.Logging;

namespace CodeCaster.PVBridge.Logic
{
    public class InputToOutputLoopStatus
    {
        private readonly ILogger _logger;

        private readonly int _taskId;

        private StateMachine _state;

        /// <summary>
        /// Configurable on the provider (but ignored for now). See <see cref="PVBridge.Configuration.InputToOutputConfiguration.SyncStart"/>
        /// </summary>
        private readonly DateTime _syncStart;

        private readonly TimeSpan _maxStatusAge;

        private DateTime? _backlogStart;

        private DateTime? _lastSuspend;
        private DateTime? _lastResume;

        /// <summary>
        /// { day, DateTimeSynced }
        /// </summary>
        private readonly Dictionary<DateOnly, DateTime> _syncedDays = new();

        /// <summary>
        /// Set after error, rate limit hit .
        /// </summary>
        private DateTime? _continueAt;

        /// <summary>
        /// Upped when receiving old or zero-power statuses, increase main loop interval.
        /// </summary>
        private int _staleDataReceived;

        /// <summary>
        /// Counts errors, works the same as stale data, but that's successful, which sets us to 0.
        /// </summary>
        private int _successiveErrors;

        public InputToOutputLoopStatus(
            ILogger logger,
            int taskId,
            DateTime syncStart,
            TimeSpan maxStatusAge
        )
        {
            _logger = logger;
            _taskId = taskId;
            _syncStart = syncStart;
            _maxStatusAge = maxStatusAge;

            _state = StateMachine.SyncBacklog;
        }

        /// <summary>
        /// Callable when the state is SyncBacklog.
        /// </summary>
        public DateTime GetBacklogStart()
        {
            return _state switch
            {
                StateMachine.SyncBacklog => _backlogStart!.Value,

                StateMachine.SyncLiveStatus => _backlogStart!.Value,

                StateMachine.Wait => throw new InvalidOperationException("Cannot calculate backlog age while waiting"),

                _ => throw new InvalidOperationException("Unknown state"),
            };
        }

        /// <summary>
        /// Backlog processing done for one day, or a live status was synced.
        /// </summary>
        /// <param name="snapshotTaken">The last snapshot date of the day.</param>
        public void DataSynced(DateTime snapshotTaken)
        {
            var day = DateOnly.FromDateTime(snapshotTaken);

            if (snapshotTaken.Date != DateTime.Today)
            {
                _syncedDays[day] = DateTime.Now;

                _state = StateMachine.SyncBacklog;

                return;
            }

            _syncedDays[day] = snapshotTaken;

            if (snapshotTaken > DateTime.Now - _maxStatusAge)
            {
                _staleDataReceived = 0;

                return;
            }

            StaleDataReceived();
        }

        /// <summary>
        /// Call(ed) when a live status or backlog sync for today yielded old records.
        /// </summary>
        public void StaleDataReceived()
        {
            _staleDataReceived++;

            // TODO: exponential backoff, like 5, 10, 15, 30, 45, 60.

            var minutesToWait = 10 * _staleDataReceived;

            _logger.LogInformation("Stale data received, waiting {minutes}.", minutesToWait.SIfPlural("minute"));

            var retryAt = DateTime.Now.AddMinutes(minutesToWait);

            _continueAt = new[] { _continueAt, retryAt }.Max();
        }

        /// <summary>
        /// Notify of suspension. May dender, doesn't trigger anything.
        /// </summary>
        public void Suspend()
        {
            _lastSuspend ??= DateTime.Now;
        }

        /// <summary>
        /// Notify we were resumed. May dender, doesn't trigger anything.
        /// </summary>
        public void Resume()
        {
            _lastResume ??= DateTime.Now;

            // Reset the stale/error counters to remove the delay they introduce, no need to wait an hour if we were shut down after dark and now booted the next day.
            // Even if the downtime was short, perhaps there was a connectivity error that's now fixed.
            _staleDataReceived = 0;
            _successiveErrors = 0;

            if (!_continueAt.HasValue || _continueAt < DateTime.Now)
            {
                // When we boot up, don't retry immediately, because the Internet seems to be unreachable when we start too soon.
                // TODO: fix? We do take a dependency on TcpIp...
                _continueAt = DateTime.Now.Add(_maxStatusAge);
            }

            _logger.LogDebug("Resuming: stale: {staleData}, errors: {errors}, continue at: {continueAt}", _staleDataReceived, _successiveErrors, _continueAt);
        }

        public StateMachine UpdateState()
        {
            _logger.LogDebug("Task status: {taskStatus}", this.ToString());

            if (_lastSuspend.HasValue && !_lastResume.HasValue)
            {
                _logger.LogDebug("Task {taskId} suspended at {lastSuspend}, not resuming", _taskId, _lastSuspend);

                return _state = StateMachine.Wait;
            }

            // Rate limited, error or stale statuses received by a previous call.
            if (_continueAt.HasValue)
            {
                if (DateTime.Now > _continueAt.Value)
                {
                    _logger.LogDebug("Continuing after waiting");

                    _continueAt = null;
                }
                else
                {
                    _logger.LogDebug("Waiting for next attempt until {continueAt}", _continueAt);

                    return _state = StateMachine.Wait;
                }
            }

            // We will end up here some time after a shutdown/hibernate.
            if (_lastResume.HasValue)
            {
                _logger.LogDebug("Task {taskId} resumed from suspension, suspended: {lastSuspend}", _taskId, _lastSuspend.ToIsoStringOrDefault("never"));

                _lastSuspend = null;
                _lastResume = null;
            }

            _backlogStart = CalculateBacklogStart();

            // Last sync was earlier than today's last supposed status age.
            var now = DateTime.Now;

            // TODO: 5 for now, what if we want sub-minute data intervals?
            var intervalMinutes = (int)Math.Ceiling(_maxStatusAge.TotalMinutes);

            var remainder = now.Minute % -intervalMinutes;

            // Can become negative, so add later.
            var minutes = now.Minute - (remainder == 0 ? intervalMinutes : remainder);

            var maxBacklogAge = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, second: 0).AddMinutes(minutes);

            // Give them a grace period if we missed one. So it's okay to report a missing :50 at :51, or so we hope.
            if (_backlogStart.Value < maxBacklogAge.AddMinutes(-intervalMinutes))
            {
                _logger.LogDebug("Last sync at {backlogStart} (maxAge: {maxAge}), syncing backlog", _backlogStart, maxBacklogAge);

                return _state = StateMachine.SyncBacklog;
            }

            // If _backlogStart = 16:40, and it's 16:41, wait until :45.
            // But if _backlogStart = 16:37 and it's 16:40, sync now.
            if (_backlogStart.Value >= maxBacklogAge && now < maxBacklogAge.AddMinutes(intervalMinutes))
            {
                _logger.LogDebug("Last sync at {backlogStart}, waiting until {maxStatusAge}", _backlogStart, maxBacklogAge.AddMinutes(intervalMinutes));
                
                return _state = StateMachine.Wait;
            }
            
            _logger.LogDebug("Last sync at {backlogStart} (max: {maxAge}), syncing live status", _backlogStart, maxBacklogAge);

            return _state = StateMachine.SyncLiveStatus;
        }

        private DateTime CalculateBacklogStart()
        {
            var backlogStart = _syncStart;

            var days = (int)Math.Ceiling((DateTime.Now - backlogStart).TotalDays) + 1;

            // Loop over all days to sync, oldest first. The first one that doesn't have a cache hit, gets returned. 
            // Some special cases for today and yesterday.

            for (int i = 0; i < days; i++)
            {
                var dayTime = backlogStart.AddDays(i);

                var day = DateOnly.FromDateTime(dayTime);

                if (!_syncedDays.TryGetValue(day, out var lastDaySnapshot))
                {
                    _logger.LogTrace("Day {day} missing, starting sync at {syncDateTime}", day, dayTime);

                    return (_backlogStart = dayTime).Value;
                }

                _logger.LogDebug("Day {day} synced at {syncDateTime}", day, lastDaySnapshot);

                // Assume a later sync fully synced that day.
                if (lastDaySnapshot.Date > dayTime.Date)
                {
                    continue;
                }

                // When a day (including today) was synced on itself, sync it again, so we can complete yesterday's data when resuming.
                _logger.LogDebug("{day} was last synced on {backlogSync}, continuing there", dayTime.LoggableDayName(), lastDaySnapshot);

                return (_backlogStart = lastDaySnapshot).Value;
            }

            // Should not happen
            // TODO: test that
            throw new InvalidOperationException($"Checking {days.SIfPlural("day")} since {backlogStart:O} did not yield a sync start time");
        }

        public void HandleApiResponse(ApiResponse response)
        {
            _continueAt = null;

            if (response.IsSuccessful)
            {
                _successiveErrors = 0;
            }

            if (response.Status == ApiResponseStatus.RateLimited)
            {
                var waitSpan = response.RetryAfter!.Value - DateTime.Now;

                _continueAt = response.RetryAfter.Value;

                _logger.LogInformation("API rate limited, waiting until {continueAt}, in {waitSpan}", _continueAt, waitSpan);
            }
            else if (response.Status == ApiResponseStatus.Failed)
            {
                _successiveErrors++;

                var minutesToWait = 10 * _successiveErrors;

                var retryAt = DateTime.Now.AddMinutes(minutesToWait);

                _continueAt = new[] { _continueAt, retryAt }.Max();

                _logger.LogInformation("API error occurred, waiting until {continueAt}, in {minutesToWait}", _continueAt, minutesToWait.SIfPlural("minute"));
            }
        }

        public override string ToString()
        {
            DateOnly? lastDay = null;
            DateTime? lastDaySynced = null;

            if (_syncedDays.Keys.Count > 0)
            {
                lastDay = _syncedDays.Keys.Last();
                lastDaySynced = _syncedDays[lastDay.Value];
            }

            return $"Id: {_taskId}, " +
                   $"State: {_state}, " +
                   $"BacklogStart: {_backlogStart.ToIsoStringOrDefault("never")}, " +
                   $"ContinueAt: {_continueAt.ToIsoStringOrDefault("never")}, " +
                   $"LastSuspend: {_lastSuspend.ToIsoStringOrDefault("never")}, " +
                   $"LastResume: {_lastResume.ToIsoStringOrDefault("never")}, " +
                   $"most recently synced day: {lastDay.ToStringOrDefault("O", "never")} " +
                   $"at {lastDaySynced.ToIsoStringOrDefault("never")}";
        }
    }
}
