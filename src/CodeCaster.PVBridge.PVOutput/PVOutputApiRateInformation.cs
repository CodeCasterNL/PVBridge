using System;

namespace CodeCaster.PVBridge.PVOutput
{
    // ReSharper disable once InconsistentNaming
    internal class PVOutputApiRateInformation
    {
        /// <summary>
        /// When this info was last received.
        /// </summary>
        public DateTime? Received { get; set; }

        /// <summary>
        /// When a request was made whose response didn't contain rate limit info.
        /// </summary>
        public DateTime? MarkedStale { get; set; }

        /// <summary>
        /// When the rate limiter kicked in.
        /// </summary>
        public DateTime? Tripped { get; set; }

        /// <summary>Hourly limit currently being enforced on account.</summary>
        public int? CurrentLimit { get; set; }

        /// <summary>Remaining API calls for the hour.</summary>
        public int? LimitRemaining { get; set; }

        /// <summary>Timestamp when the limit will be reset.</summary>
        public DateTime? LimitResetAt { get; set; }
    }
}
