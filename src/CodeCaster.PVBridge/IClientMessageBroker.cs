using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CodeCaster.PVBridge.Output;

namespace CodeCaster.PVBridge
{
    /// <summary>
    /// Classes in this assembly call an IClientMessageBroker to send messages to connected clients.
    /// </summary>
    public interface IClientMessageBroker
    {
        /// <summary>
        /// Client can request sync, subscribe to this event to start or continue working.
        /// </summary>
        event EventHandler StatusSyncRequested;

        /// <summary>
        /// Called by <see cref="IInputToOutputWriter"/> implementations when a <see cref="Snapshot"/> is read (either succesfully or not, see <see cref="ApiResponse"/>).
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        Task SnapshotReceivedAsync(ApiResponse<Snapshot> response);

        /// <summary>
        /// Called by <see cref="IInputToOutputWriter"/> implementations when a <see cref="DaySummary"/> is read (either succesfully or not, see <see cref="ApiResponse"/>).
        /// </summary>
        /// <param name="daySummaries"></param>
        /// <returns></returns>
        Task SummariesReceivedAsync(ApiResponse<IReadOnlyCollection<DaySummary>> daySummaries);
    }
}
