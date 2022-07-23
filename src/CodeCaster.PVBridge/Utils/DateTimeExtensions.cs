using System;
using System.Collections.Generic;

namespace CodeCaster.PVBridge.Utils
{
    public static class DateTimeExtensions
    {
        public static IEnumerable<DateTime> GetDaysUntil(this DateTime since, DateTime until)
        {
            var fromDay = since.Date;
            var days = (int)(until - fromDay).TotalDays + 1;
            
            if (days <= 0)
            {
                throw new ArgumentException("Since must be equal to or before until.");
            }

            if (days > 31)
            {
                throw new ArgumentException("Cannot request more than 31 days of data at once.");
            }

            int day = 0;

            do
            {
                yield return since.AddDays(day++);
            }
            while (day < days);
        }
    }
}
