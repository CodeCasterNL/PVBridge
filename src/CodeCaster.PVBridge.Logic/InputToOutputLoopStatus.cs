using System;
using System.Collections.Generic;
using System.Linq;
using CodeCaster.PVBridge.Utils;
using Microsoft.Extensions.Logging;

namespace CodeCaster.PVBridge.Logic
{
    // TODO: magic numbers to consts
    public class InputToOutputLoopStatus
    {
        private readonly ILogger _logger;

        private readonly int _taskId;

        private StateMachine _state;

        /// <summary>
        /// The start datetime from when to start syncing. Configurable on the provider (but ignored for now). See <see cref="PVBridge.Configuration.InputToOutputConfiguration.SyncStart"/>
        /// </summary>
        private readonly DateTime _syncStart;

        /// <summary>
        /// Five minutes for now.
        /// </summary>
        private readonly TimeSpan _statusResolution;

        private DateTime _backlogStart;

        private DateTime? _lastSuspend;
        private DateTime? _lastResume;

        /// <summary>
        /// Holds the _last sync_ datetime as value for the day indicated by the key.
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
            TimeSpan statusResolution
        )
        {
            _logger = logger;
            _taskId = taskId;
            _syncStart = syncStart;
            _backlogStart = syncStart;
            _statusResolution = statusResolution;

            _state = StateMachine.SyncBacklog;
        }

        /// <summary>
        /// Callable when <see cref="UpdateState"/> returns <see cref="StateMachine.SyncBacklog"/>.
        /// </summary>
        public DateTime GetBacklogStart()
        {
            return _state switch
            {
                StateMachine.SyncBacklog => _backlogStart,

                StateMachine.SyncLiveStatus => _backlogStart,

                StateMachine.Wait => throw new InvalidOperationException("Cannot calculate backlog age while waiting"),

                _ => throw new InvalidOperationException("Unknown state"),
            };
        }

        /// <summary>
        /// Callable when <see cref="UpdateState"/> returns <see cref="StateMachine.Wait"/>.
        /// </summary>
        /// <returns></returns>
        public DateTime GetWaitTimeAsync()
        {
            // Error/rate limit/stale data occurred, wait.
            if (_continueAt.HasValue)
            {
                return _continueAt.Value;
            }

            // Today not synced yet? Sync now!
            if (!_syncedDays.TryGetValue(DateOnly.FromDateTime(DateTime.Today), out var lastDaySnapshot))
            {
                return DateTime.Now;
            }

            // 13:57 -> 14:00.
            return GetMaxBacklogAge(lastDaySnapshot, (int)Math.Ceiling(_statusResolution.TotalMinutes)) + _statusResolution;
        }

        /// <summary>
        /// Backlog processing done for one day, or a live status was synced.
        /// </summary>
        /// <param name="day">The day.</param>
        /// <param name="snapshotTaken">The last snapshot date of the day.</param>
        public void DataSynced(DateOnly day, DateTime snapshotTaken)
        {
            _syncedDays[day] = snapshotTaken;
            
            var today = DateOnly.FromDateTime(DateTime.Today);
            
            if (day != today)
            {
                return;
            }
            
            if (snapshotTaken > DateTime.Now - _statusResolution)
            {
                _staleDataReceived = 0;

                return;
            }

            StaleDataReceived(snapshotTaken);
        }

        /// <summary>
        /// Call(ed) when a live status or backlog sync for today yielded old records.
        /// </summary>
        public void StaleDataReceived(DateTime? day)
        {
            _staleDataReceived++;

            // TODO: exponential backoff, like 5, 10, 15, 30, 45, 60.

            var minutesToWait = 10 * _staleDataReceived;

            _logger.LogInformation("Stale data received for {day}, waiting {minutes}.", day?.LoggableDayName() ?? "", minutesToWait.SIfPlural("minute"));

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
                _continueAt = DateTime.Now.Add(_statusResolution);
            }

            _logger.LogDebug("Resuming: stale: {staleData}, errors: {errors}, continue at: {continueAt}", _staleDataReceived, _successiveErrors, _continueAt);
        }

        public StateMachine UpdateState()
        {
            _logger.LogTrace("Updating old task status: {taskStatus}", this.ToString());
            
            UpdateStateImpl();
            
            _logger.LogDebug("Task status: {taskStatus}", this.ToString());

            return _state;
        }

        private void UpdateStateImpl()
        {
            if (_lastSuspend.HasValue && !_lastResume.HasValue)
            {
                _logger.LogDebug("Task {taskId} suspended at {lastSuspend}, not resuming", _taskId, _lastSuspend);

                _state = StateMachine.Wait;
                
                return;
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

                     _state = StateMachine.Wait;

                     return;
                }
            }

            // We will end up here some time after a shutdown/hibernate.
            if (_lastResume.HasValue)
            {
                _logger.LogDebug("Task {taskId} resumed from suspension, suspended: {lastSuspend}", _taskId, _lastSuspend.ToIsoStringOrDefault("never"));

                _lastSuspend = null;
                _lastResume = null;
            }

            UpdateBacklogStart();

            // Last sync was earlier than today's last supposed status age.
            var now = DateTime.Now;
            
            var intervalMinutes = (int)Math.Ceiling(_statusResolution.TotalMinutes); 
            
            var maxBacklogAge = GetMaxBacklogAge(now, intervalMinutes);

            // Give them a grace period if we missed one. So it's okay to report a missing :50 at :51, or so we hope.
            // Not reporting on the dot can show rounding differences on the 5-minute marks between GoodWe and PVOutput, totals and general shape of graph should match.
            if (_backlogStart < maxBacklogAge.AddMinutes(-intervalMinutes))
            {
                _logger.LogDebug("Last sync at {backlogStart} (maxAge: {maxAge}), syncing backlog", _backlogStart, maxBacklogAge);

                _state = StateMachine.SyncBacklog;
                
                return;
            }

            // If _backlogStart = 16:40, and it's 16:41, wait until :45.
            // But if _backlogStart = 16:37 and it's 16:40, sync now, to try and hit the 5-minute mark.
            var continueSync = maxBacklogAge.AddMinutes(intervalMinutes);
            if (_backlogStart >= maxBacklogAge && now < continueSync)
            {
                _logger.LogDebug("Last sync at {backlogStart}, waiting until {maxStatusAge}", _backlogStart, continueSync);

                _continueAt = continueSync;
                
                _state = StateMachine.Wait;

                return;
            }
            
            _logger.LogDebug("Last sync at {backlogStart} (max: {maxAge}), syncing live status", _backlogStart, maxBacklogAge);

            _state = StateMachine.SyncLiveStatus;
        }

        private static DateTime GetMaxBacklogAge(DateTime now, int intervalMinutes)
        {
            var remainder = now.Minute % -intervalMinutes;

            // Can become negative, so add later.
            // TODO: what if we want sub-minute data intervals?
            var minutes = now.Minute - (remainder == 0 ? intervalMinutes : remainder);

            return new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, second: 0).AddMinutes(minutes);
        }

        private void UpdateBacklogStart()
        {
            var backlogStart = new[] { _backlogStart, _syncStart }.Max();

            var days = backlogStart.GetDaysUntil(DateTime.Now).ToList();

            // Loop over all days to sync, oldest first. The first one that doesn't have a cache hit, gets returned. 
            // Some special cases for today and yesterday.

            foreach (var dayTime in days)
            {
                var day = DateOnly.FromDateTime(dayTime);

                if (!_syncedDays.TryGetValue(day, out var lastDaySnapshot))
                {
                    _logger.LogTrace("Day {day} missing, starting sync at {syncDateTime}", day.LoggableDayName(), dayTime);

                    _backlogStart = dayTime;

                    return;
                }

                _logger.LogTrace("Day {day} synced at {syncDateTime}", day.LoggableDayName(), lastDaySnapshot);

                // Assume a later sync fully synced that day.
                if (lastDaySnapshot.Date > dayTime.Date)
                {
                    continue;
                }

                // When a day (including today) was synced on itself, sync it again, so we can complete yesterday's data when resuming.
                _logger.LogTrace("{day} was last synced on {backlogSync}, continuing there", dayTime.LoggableDayName(), lastDaySnapshot);

                _backlogStart = lastDaySnapshot;

                return;
            }

            // Should not happen
            // TODO: test that
            throw new InvalidOperationException($"Checking {days.Count.SIfPlural("day")} since {backlogStart:O} did not yield a sync start time");
        }

        // TODO: pass config
        public void HandleApiResponse(ApiResponse response)
        {
            _continueAt = null;

            if (response.IsSuccessful)
            {
                _successiveErrors = 0;
            }

            if (response.IsRateLimited)
            {
                var waitSpan = response.RetryAfter.Value - DateTime.Now;

                _continueAt = response.RetryAfter.Value;

                _logger.LogInformation("API rate limited, waiting until {continueAt}, in {waitSpan}", _continueAt, waitSpan);
            }
            else if (response.Status == ApiResponseStatus.Failed)
            {
                _successiveErrors++;

                var minutesToWait = Math.Min(120, 10 * _successiveErrors);

                var retryAt = DateTime.Now.AddMinutes(minutesToWait);

                _continueAt = new[] { _continueAt, retryAt }.Max();

                _logger.LogInformation("{apiErrorCount} occurred, waiting until {continueAt}, in {minutesToWait}", _successiveErrors.SIfPlural("API error"), _continueAt, minutesToWait.SIfPlural("minute"));
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
                   $"BacklogStart: {_backlogStart:O}, " +
                   $"ContinueAt: {_continueAt.ToIsoStringOrDefault("never")}, " +
                   $"LastSuspend: {_lastSuspend.ToIsoStringOrDefault("never")}, " +
                   $"LastResume: {_lastResume.ToIsoStringOrDefault("never")}, " +
                   $"most recently synced day: {lastDay.ToStringOrDefault("O", "never")} " +
                   $"at {lastDaySynced.ToIsoStringOrDefault("never")}";
        }
    }
}
