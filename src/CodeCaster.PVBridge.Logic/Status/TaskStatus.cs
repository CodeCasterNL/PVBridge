using System;
using System.Linq;
using System.Threading;
using CodeCaster.PVBridge.Configuration;
using CodeCaster.PVBridge.Utils;
using Microsoft.Extensions.Logging;

namespace CodeCaster.PVBridge.Logic.Status
{
    /// <summary>
    /// TODO: magic numbers to consts
    /// </summary>
    public abstract class TaskStatus
    {
        private readonly int _taskId;
        protected readonly IClock Clock;
        protected readonly ILogger Logger;

        /// <summary>
        /// Only to be called from the constructor, use <see cref="_taskId"/>.
        /// </summary>
        private static int _refTaskId;

        /// <summary>
        /// Five minutes for now.
        /// </summary>
        protected readonly TimeSpan StatusResolution;

        private DateTime? _lastSuspend;
        private DateTime? _lastResume;

        /// <summary>
        /// Set after error or rate limit hit.
        /// </summary>
        protected DateTime? ContinueAt;

        /// <summary>
        /// Counts errors, works the same as stale data, but that's successful, which sets us to 0.
        /// </summary>
        private int _successiveErrors;

        /// <summary>
        /// Artificial delay to prevent connectivity errors. TODO: fix that.
        /// </summary>
        private readonly TimeSpan _resumeDelay = TimeSpan.FromMinutes(2);

        protected TaskStatus(ILogger logger,
            IClock clock,
            TimeSpan statusResolution)
        {
            _taskId = Interlocked.Increment(ref _refTaskId);

            Logger = logger;
            Clock = clock;
            StatusResolution = statusResolution;
        }

        /// <summary>
        /// This is called first in a loop and checks whether we need to wait or start syncing, either the backlog (old data) or the live status, or both.
        /// </summary>
        /// <returns></returns>
        public State GetState()
        {
            Logger.LogTrace("Updating old task status: {taskStatus}", ToString());

            // We're being suspended, computer is shutting down.
            if (_lastSuspend.HasValue && !_lastResume.HasValue)
            {
                Logger.LogDebug("Task {taskId} suspended at {lastSuspend}, not resuming", _taskId, _lastSuspend);

                return State.Wait(Clock.Now + TimeSpan.FromMinutes(5));
            }

            // Rate limited, error or stale statuses received by a previous call.
            if (ContinueAt.HasValue)
            {
                if (Clock.Now >= ContinueAt.Value)
                {
                    Logger.LogDebug("Continuing after waiting");

                    ContinueAt = null;
                }
                else
                {
                    Logger.LogDebug("Waiting for next attempt until {continueAt}", ContinueAt);

                    return State.Wait(ContinueAt.Value);
                }
            }

            // We will end up here some time after a shutdown/hibernate.
            if (_lastResume.HasValue)
            {
                Logger.LogDebug("Task {taskId} resumed from suspension, suspended: {lastSuspend}", _taskId, _lastSuspend.ToIsoStringOrDefault("never"));

                _lastSuspend = null;
                _lastResume = null;
            }

            var state = UpdateState();

            Logger.LogDebug("Task status: {taskStatus}", ToString());

            return state;
        }

        /// <summary>
        /// Called when <see cref="ContinueAt"/> isn't set, to handle status-specific retry policies.
        /// </summary>
        /// <returns></returns>
        protected abstract State UpdateState();

        protected static DateTime GetMaxStatusAge(DateTime since, int intervalMinutes)
        {
            var remainder = since.Minute % -intervalMinutes;

            // Can become negative, so add later.
            // TODO: what if we want sub-minute data intervals?
            var minutes = since.Minute - (remainder == 0 ? intervalMinutes : remainder);

            return new DateTime(since.Year, since.Month, since.Day, since.Hour, 0, second: 0).AddMinutes(minutes);
        }

        /// <summary>
        /// Notify of suspension. May dender, doesn't trigger anything.
        /// </summary>
        public void Suspend()
        {
            _lastSuspend ??= Clock.Now;
        }

        /// <summary>
        /// Notify we were resumed. May dender, doesn't trigger anything.
        /// </summary>
        public void Resume()
        {
            _lastResume = Clock.Now;

            // Reset the stale/error counters to remove the delay they introduce, no need to wait an hour if we were shut down after dark and now booted the next day.
            // Even if the downtime was short, perhaps there was a connectivity error that's now fixed.
            _successiveErrors = 0;

            ResetErrorCounters();

            // When we resume from hibernation, we can't retry immediately, because then the Internet connectivity seems to be inconsistent.
            // TODO: fix? We do take a dependency on TcpIp...
            ContinueAt = Clock.Now.Add(_resumeDelay);

            Logger.LogDebug("Resuming at {continueAt}", ContinueAt);
        }

        /// <summary>
        /// Called after a resume (shutdown & boot).
        /// </summary>
        protected abstract void ResetErrorCounters();

        /// <summary>
        /// Handles rate limit and error responses (except <see cref="ApiResponseStatus.BadRequest"/>).
        /// </summary>
        /// TODO: only public for testing, should be protected
        public State HandleApiResponse(DataProviderConfiguration provider, ApiResponse response, bool badRequestIsBad = true)
        {
            ContinueAt = null;
            
            if (response.IsRateLimited)
            {
                return RateLimited(response.RetryAfter.Value.ToLocalTime());
            }

            if (response.IsSuccessful)
            {
                _successiveErrors = 0;
            }
            
            if (response.Status == ApiResponseStatus.Failed)
            {
                return ErrorReceived();
            }

            if (badRequestIsBad && response.Status == ApiResponseStatus.BadRequest)
            {
                return ErrorReceived();
            }

            return ContinueAt.HasValue
                ? State.Wait(ContinueAt.Value)
                : State.Success;
        }

        protected State ErrorReceived()
        {
            _successiveErrors++;

            // TODO: exponentiallish backoff, like 5, 10, 15, 30, 45, 60.
            var minutesToWait = Math.Min(120, 10 * _successiveErrors);

            var retryAt = Clock.Now.AddMinutes(minutesToWait);

            ContinueAt = new[] { ContinueAt, retryAt }.Max()!;

            Logger.LogInformation("{apiErrorCount} occurred, waiting until {continueAt}, in {minutesToWait}", _successiveErrors.SIfPlural("API error"), ContinueAt, minutesToWait.SIfPlural("minute"));
       
            return State.Wait(ContinueAt.Value);
        }

        protected State RateLimited(DateTime retryAfter)
        {
            var waitSpan = retryAfter - Clock.Now;

            ContinueAt = new[] { ContinueAt, retryAfter }.Max()!;

            Logger.LogInformation("API rate limited, waiting until {continueAt}, in {waitSpan}", ContinueAt, waitSpan);

            return State.Wait(ContinueAt.Value);
        }

        public override string ToString()
        {
            return $"Id: {_taskId}, " +
                   $"ContinueAt: {ContinueAt.ToIsoStringOrDefault("never")}, " +
                   $"LastSuspend: {_lastSuspend.ToIsoStringOrDefault("never")}, " +
                   $"LastResume: {_lastResume.ToIsoStringOrDefault("never")}, " +
                   ToLogString();
        }

        protected abstract string ToLogString();
    }
}
