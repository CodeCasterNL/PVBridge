using CodeCaster.PVBridge.Utils;
using System;
using System.Diagnostics;

namespace CodeCaster.PVBridge.Logic.Status
{
    public enum DayState
    {
        NeedsSyncing,
        Wait,
        FullySynced,
    }

    [DebuggerDisplay("{ToString(),nq}")]
    public class BacklogDayStatus
    {
        public BacklogDayStatus(DateOnly day, DayState state)
        {
            Day = day;
            State = state;
        }

        public BacklogDayStatus(BacklogDayStatus other)
            : this(other.Day, other.State)
        {
            ContinueAt = other.ContinueAt;
            SyncedAt = other.SyncedAt;
            _errorCount = other._errorCount;
        }

        public DateOnly Day { get; }

        public DateTime? ContinueAt { get; private set; }

        public DateTime? SyncedAt { get; private set; }

        public DayState State { get; private set; }

        private int _errorCount;

        public void SyncNow()
        {
            State = DayState.NeedsSyncing;
            ContinueAt = null;
        }

        public State Wait(DateTime continueAt)
        {
            State = DayState.Wait;
            ContinueAt = continueAt;

            return Status.State.Wait(ContinueAt.Value);
        }

        public void Synced(DateTime now)
        {
            State = DayState.FullySynced;
            SyncedAt = now;
        }

        /// <summary>
        /// Up the error counter, setting <see cref="ContinueAt"/> when <paramref name="now"/> is not <c>null</c>.
        /// </summary>
        public State AddError(DateTime now)
        {
            _errorCount++;

            // TODO: exponentiallish backoff, like 5, 10, 15, 30, 45, 60.
            var minutesToWait = Math.Min(120, 10 * _errorCount);

            return Wait(now.AddMinutes(minutesToWait));
        }

        public override string ToString()
        {
            return $"DayStatusy: D: {Day.LoggableDayName()}, " +
                   $"State: {State}, " +
                   $"SyncedAt: {SyncedAt.ToIsoStringOrDefault("(never)")}, " +
                   $"NextSync: {ContinueAt.ToIsoStringOrDefault("(never)")}, " +
                   $"Errors: {_errorCount}";
        }
    }
}
