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
        bool CanWriteDetails(DateTime day);

        /// <summary>
        /// Write a single snapshot to the output.
        /// </summary>
        Task<ApiResponse> WriteStatusAsync(DataProviderConfiguration output, Snapshot snapshot, CancellationToken cancellationToken);

        /// <summary>
        /// Write a batch of snapshots (i.e. details for each n seconds/minutes) to the output.
        /// </summary>
        Task<ApiResponse> WriteStatusesAsync(DataProviderConfiguration output, IReadOnlyCollection<Snapshot> snapshots, CancellationToken cancellationToken);

        /// <summary>
        /// Write a batch of day summaries to the output.
        /// </summary>
        Task<ApiResponse> WriteDaySummariesAsync(DataProviderConfiguration output, IReadOnlyCollection<DaySummary> summaries, CancellationToken cancellationToken);
    }
}
