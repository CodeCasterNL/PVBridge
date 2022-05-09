using CodeCaster.PVBridge.Output;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCaster.PVBridge.Logic
{
    /// <summary>
    /// Runs when started with --idle, or with an empty configuration. Waits on Ctrl+C forever.
    /// </summary>
    public class IdleInputToOutputLoop : IInputToOutputLoop
    {
        private readonly ILogger<IInputToOutputLoop> _logger;
        private readonly IClientMessageBroker _messageBroker;

        public IdleInputToOutputLoop(ILogger<IInputToOutputLoop> logger, IClientMessageBroker messageBroker)
        {
            _logger = logger;
            _messageBroker = messageBroker;
        }

        public Task RunAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Service running idle");

            Console.WriteLine("Running idle.");

            return Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public async void StatusSyncRequested(object? sender, EventArgs e)
        {
            _logger.LogInformation("Syncing fake current status");

            Console.WriteLine("Running idle.");

            // Fake that the service received our request and sends a response (in reality, this call will return directly, the service will call SnapshotReceivedAsync() after the API call finishes).
            await _messageBroker.SnapshotReceivedAsync(new ApiResponse<Snapshot>(new Snapshot
            {
                TimeTaken = DateTime.Now,
                ActualPower = 42,
                DailyGeneration = 2545,
                Temperature = 70,
                VoltAC = 230
            }));
        }

        public void Suspend() { }

        public void Resume() { }
    }
}
