using System;
using System.Diagnostics.CodeAnalysis;

namespace CodeCaster.PVBridge.Logic.Status
{
    public record State
    {
        public DateTime? ContinueAt { get; }

        [MemberNotNullWhen(true, nameof(ContinueAt))]
        public bool ShouldRetry => ContinueAt.HasValue;

        public static State Success => new();

        public static State Wait(DateTime continueAt) => new(continueAt);

        private State(DateTime? continueAt = null)
        {
            ContinueAt = continueAt;
        }

        public virtual bool Equals(State? other) => other != null && ContinueAt == other.ContinueAt;

        public override int GetHashCode() => ContinueAt.GetHashCode();
    }
}
