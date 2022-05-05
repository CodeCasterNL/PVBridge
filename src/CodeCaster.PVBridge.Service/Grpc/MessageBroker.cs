using CodeCaster.PVBridge.Grpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCaster.PVBridge.Service.Grpc
{
    //TODO: replace with something fancy
    public class MessageBroker : IClientAndServiceMessageBroker
    {
        private readonly ConcurrentDictionary<CancellationToken, IServerStreamWriter<Snapshot>> _snapshotStreams = new();
        private readonly ConcurrentDictionary<CancellationToken, IServerStreamWriter<Summary>> _summaryStreams = new();

        public event EventHandler StatusSyncRequested = delegate { };

        public void SubscribeSnapshots(IServerStreamWriter<Snapshot> responseStream, CancellationToken cancellationToken)
        {
            cancellationToken.Register(() => _snapshotStreams.Remove(cancellationToken, out _));
            _snapshotStreams.TryAdd(cancellationToken, responseStream);
        }

        public void SubscribeSummaries(IServerStreamWriter<Summary> responseStream, CancellationToken cancellationToken)
        {
            cancellationToken.Register(() => _summaryStreams.Remove(cancellationToken, out _));
            _summaryStreams.TryAdd(cancellationToken, responseStream);
        }

        public Task SyncCurrentStatusAsync(CancellationToken cancellationToken)
        {
            StatusSyncRequested?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        public async Task SnapshotReceivedAsync(ApiResponse<Output.Snapshot> response)
        {
            // Send the freshly fetched snapshot to the subscribed clients
            foreach (var (cts, stream) in _snapshotStreams)
            {
                if (cts.IsCancellationRequested)
                {
                    _snapshotStreams.TryRemove(cts, out _);
                    continue;
                }

                if (response.IsSuccessful)
                {
                    await stream.WriteAsync(new Snapshot
                    {
                        Timestamp = Timestamp.FromDateTime(response.Response.TimeTaken.ToUniversalTime()),
                        ActualPower = response.Response.ActualPower.GetValueOrDefault(),
                        Temperature = response.Response.Temperature.GetValueOrDefault(),
                        VoltAc = response.Response.VoltAC.GetValueOrDefault(),
                        DailyGeneration = response.Response.DailyGeneration.GetValueOrDefault()
                    });
                }
            }
        }

        /// <inheritdoc/>>
        public async Task SummariesReceivedAsync(ApiResponse<IReadOnlyCollection<Output.DaySummary>> response)
        {
            // Send the freshly fetched summaries to the subscribed clients
            foreach (var (cts, stream) in _summaryStreams)
            {
                if (cts.IsCancellationRequested)
                {
                    _snapshotStreams.TryRemove(cts, out _);
                    continue;
                }

                if (response.IsSuccessful)
                {
                    foreach (var summary in response.Response)
                    {
                        await stream.WriteAsync(new Summary
                        {
                            Day = Timestamp.FromDateTime(summary.Day.ToUniversalTime()),
                            DailyGeneration = summary.DailyGeneration.GetValueOrDefault()
                        });
                    }
                }
            }
        }
    }
}
