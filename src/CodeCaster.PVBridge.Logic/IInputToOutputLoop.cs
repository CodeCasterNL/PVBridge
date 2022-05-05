using System;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCaster.PVBridge.Logic
{
    public interface IInputToOutputLoop
    {
        /// <summary>
        /// Called once per input and output combination, as long as the service runs.
        /// </summary>
        /// <param name="stoppingToken"></param>
        Task RunAsync(CancellationToken stoppingToken);

        void Suspend();

        void Resume();
        
        void StatusSyncRequested(object? sender, EventArgs e);
    }
}