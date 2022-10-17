using System;
using System.Linq;
using CodeCaster.PVBridge.Configuration;
using CodeCaster.PVBridge.Output;
using CodeCaster.PVBridge.Utils;
using Microsoft.Extensions.Logging;

namespace CodeCaster.PVBridge.Logic.Status
{
    public class LiveStatus : TaskStatus
    {
        private Snapshot? _lastStatus;

        /// <summary>
        /// Upped when receiving old or zero-power statuses, increase main loop interval.
        /// </summary>
        private int _staleDataReceived;

        private DateTime _lastSyncAttempt;

        public LiveStatus(ILogger logger, IClock clock, TimeSpan statusResolution)
            : base(logger, clock, statusResolution)
        {
        }

        protected override State UpdateState()
        {
            if (_lastStatus == null)
            {
                Logger.LogDebug("No earlier succesful sync attempts, syncing immediately");

                return State.Success;
            }

            var now = Clock.Now;

            var intervalMinutes = (int)Math.Ceiling(StatusResolution.TotalMinutes);

            var maxStatusAge = GetMaxStatusAge(now, intervalMinutes);

            // Truncate for easier comparison.
            var lastSnapshotTaken = _lastStatus.TimeTaken.Truncate(TimeSpan.FromMinutes(1));

            // If lastSnapshotTaken = 16:40, and it's 16:41, wait until :45.
            var continueSync = maxStatusAge.AddMinutes(intervalMinutes);

            if (lastSnapshotTaken >= maxStatusAge)
            {
                Logger.LogDebug("Last sync at {lastSnapshotTaken}, waiting until {continueSync}", lastSnapshotTaken, continueSync);

                ContinueAt = continueSync;

                return State.Wait(ContinueAt.Value);
            }

            // But if lastSnapshotTaken = 16:37 and it's 16:40, sync now, to try and hit the 5-minute mark.
            // Give them a grace period if we missed one. So it's okay to report a missing :50 at :51.
            // Not reporting on the dot can show rounding differences on the 5-minute marks between GoodWe and PVOutput,
            // though totals and general shape of graph should match. That's okay, we'll backlog-sync it the next day anyway.
            var lastSyncAge = now - _lastSyncAttempt;
            
            var halfInterval = TimeSpan.FromMinutes(intervalMinutes / 2f);
            
            var minStatusAge = maxStatusAge.Add(-halfInterval);
            
            if (continueSync <= now || lastSnapshotTaken <= minStatusAge && lastSyncAge > halfInterval)
            {
                Logger.LogDebug("Last sync at {lastSnapshotTaken} (maxAge: {maxBacklogAge}), syncing status", lastSnapshotTaken, maxStatusAge);

                ContinueAt = null;

                return State.Success;
            }

            var waitUntil = maxStatusAge + StatusResolution;

            Logger.LogDebug("Last sync at {lastSnapshotTaken}, waiting until {waitUntil}", lastSnapshotTaken, waitUntil);

            ContinueAt = waitUntil;

            return State.Wait(waitUntil);
        }

        public State HandleSnapshotReadResponse(DataProviderConfiguration inputProvider, ApiResponse<Snapshot> snapshotResponse)
        {
            var now = Clock.Now;
            _lastSyncAttempt = now;

            var state = HandleApiResponse(inputProvider, snapshotResponse);

            if (state.ShouldRetry)
            {
                return state;
            }

            var currentStatus = snapshotResponse.Response;

            if (currentStatus == null)
            {
                Logger.LogDebug("Received no data, skipping: {currentStatus}", currentStatus);
            
                return ErrorReceived();
            }

            // Ignore yesterday's status, GoodWe can report a stale state for hours after shutdown.
            if (currentStatus.TimeTaken.Date < now.Date)
            {
                Logger.LogDebug("Received old data, skipping: {currentStatus}", currentStatus);

                return StaleDataReceived(currentStatus.TimeTaken);
            }

            if (currentStatus.TimeTaken < now.Add(-StatusResolution))
            {
                Logger.LogDebug("Received stale data, skipping: {currentStatus}", currentStatus);

                return StaleDataReceived(currentStatus.TimeTaken);
            }

            // We've seen that one before, the inverter is probably off.
            if (_lastStatus?.TimeTaken == currentStatus.TimeTaken)
            {
                Logger.LogDebug("Status is equal to the previous, skipping: {currentStatus}", currentStatus);

                return StaleDataReceived(currentStatus.TimeTaken);
            }
            
            if (currentStatus.ActualPower is null or 0)
            {
                Logger.LogDebug("No actual power, might be either stale, dark or disconnected: {currentStatus}", currentStatus);

                _lastStatus ??= currentStatus;

                return StaleDataReceived(currentStatus.TimeTaken);
            }

            _lastStatus = currentStatus;

            return State.Success;
        }

        public State HandleSnapshotWriteResponse(DataProviderConfiguration outputProvider, ApiResponse<Snapshot> statusResponse)
        {
            var now = Clock.Now;
            _lastSyncAttempt = now;

            var state = HandleApiResponse(outputProvider, statusResponse);

            if (state.ShouldRetry)
            {
                return state;
            }
            
            // TODO: can go from 12:04:59 to 12:05:00, which might already be earlier than the _current_ time... caller handles the limits!
            var nextStatusPoll = now.Truncate(StatusResolution).Add(StatusResolution);

            return State.Wait(nextStatusPoll);
        }

        /// <summary>
        /// Call(ed) when a live status or backlog sync for today yielded old records.
        /// </summary>
        private State StaleDataReceived(DateTime snapshotDateTime)
        {
            _staleDataReceived++;

            // TODO: exponentiallish backoff, like 5, 10, 15, 30, 45, 60.
            var minutesToWait = Math.Min(120, 10 * _staleDataReceived);

            Logger.LogInformation("Stale data received for {day} ({times}), waiting {minutes}.", snapshotDateTime.LoggableDayName(), _staleDataReceived.SIfPlural("time"), minutesToWait.SIfPlural("minute"));

            var retryAt = Clock.Now.AddMinutes(minutesToWait);

            ContinueAt = new[] { ContinueAt, retryAt }.Max();

            return State.Wait(ContinueAt!.Value);
        }

        protected override void ResetErrorCounters()
        {
            Logger.LogDebug("Resetting stale counter from {staleData}", _staleDataReceived);

            _staleDataReceived = 0;
        }

        protected override string ToLogString()
        {
            return $"StaleData: {_staleDataReceived}";
        }
    }
}
