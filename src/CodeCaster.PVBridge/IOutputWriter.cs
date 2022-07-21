using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CodeCaster.PVBridge.Configuration;
using CodeCaster.PVBridge.Output;

namespace CodeCaster.PVBridge
{
    public interface IOutputWriter : IDataProvider
    {
        /// <summary>
        /// Returns whether snapshots for the given day can be written to the configured output.
        /// </summary>
        bool CanWriteDetails(DataProviderConfiguration outputConfig, DateTime day);

        /// <summary>
        /// Returns whether a summary for the given day can be written to the configured output.
        /// </summary>
        bool CanWriteSummary(DataProviderConfiguration outputConfig, DateTime day);

        /// <summary>
        /// Write a single snapshot to the output.
        /// </summary>
        Task<ApiResponse> WriteStatusAsync(DataProviderConfiguration outputConfig, Snapshot snapshot, CancellationToken cancellationToken);

        /// <summary>
        /// Write a batch of snapshots (i.e. details for each n seconds/minutes) to the output.
        /// </summary>
        Task<ApiResponse> WriteStatusesAsync(DataProviderConfiguration outputConfig, IReadOnlyCollection<Snapshot> snapshots, CancellationToken cancellationToken);

        /// <summary>
        /// Write a batch of day summaries to the output.
        /// </summary>
        Task<ApiResponse> WriteDaySummariesAsync(DataProviderConfiguration outputConfig, IReadOnlyCollection<DaySummary> summaries, CancellationToken cancellationToken);
    }
}
