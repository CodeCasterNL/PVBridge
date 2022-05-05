using System;

namespace CodeCaster.PVBridge.Logic
{
    internal class DayStatus
    {
        public DayStatus(DateOnly day)
        {
            Day = day;
        }

        public DateOnly Day { get; set; }
        
        public DateTime? SyncedAt { get; set; }
    }
}