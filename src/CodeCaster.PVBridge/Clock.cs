using System;

namespace CodeCaster.PVBridge
{
    /// <summary>
    /// For unit testing time-sensitive code.
    /// </summary>
    public interface IClock
    {
        DateTime Now { get; }

        /// <summary>
        /// Returns a wall clock.
        /// </summary>
        public static IClock Default => new DefaultClock();
        
        private class DefaultClock : IClock
        {
            public DateTime Now => DateTime.Now;
        }
    }
}
