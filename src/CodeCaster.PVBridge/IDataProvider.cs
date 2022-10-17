using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CodeCaster.PVBridge.Configuration;
using CodeCaster.PVBridge.Output;

namespace CodeCaster.PVBridge
{
    /// <summary>
    /// A provider that can read and/or write PV data to or from an API, a file, ...
    /// </summary>
    public interface IDataProvider
    {
        /// <summary>
        /// The name or type of the writer. Must be unique among all IOutputWriter implementations.
        /// </summary>
        string Type { get; }

        /// <summary>
        /// Returns a record for each day in the given time period, date ascending, setting its output to null if no data is present for that day.
        ///
        /// Can be called for 31 days at most.
        /// </summary>
        Task<ApiResponse<IReadOnlyCollection<DaySummary>>> GetSummariesAsync(DataProviderConfiguration configuration, DateTime since, DateTime until, CancellationToken cancellationToken = default);
    }
}
