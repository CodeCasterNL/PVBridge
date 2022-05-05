using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CodeCaster.PVBridge.Configuration;
using CodeCaster.PVBridge.Output;

namespace CodeCaster.PVBridge
{
    /// <summary>
    /// Reads PV snapshots (<see cref="Snapshot"/>) to or from an API, a file, ...
    /// </summary>
    public interface IInputProvider : IDataProvider
    {
        /// <summary>
        /// Obtain the current (or last known) status of an installation.
        /// </summary>
        Task<ApiResponse<Snapshot>> GetCurrentStatusAsync(DataProviderConfiguration configuration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all snapshots (<see cref="Snapshot"/>) for the given period.
        /// </summary>
        Task<ApiResponse<IReadOnlyCollection<Snapshot>>> GetSnapshotsAsync(DataProviderConfiguration configuration, DateTime since, DateTime? until = null, CancellationToken cancellationToken = default);
    }
}
