using CodeCaster.PVBridge.Grpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CodeCaster.PVBridge.Service.Grpc
{
    /// <summary>
    /// Talks gRPC to the UI.
    /// </summary>
    public class PVBridgeGrpcService : PVBridgeService.PVBridgeServiceBase
    {
        private readonly ILogger<PVBridgeGrpcService> _logger;
        private readonly IClientAndServiceMessageBroker _messageBroker;

        public PVBridgeGrpcService(ILogger<PVBridgeGrpcService> logger, IClientAndServiceMessageBroker messageBroker)
        {
            _logger = logger;
            _messageBroker = messageBroker;
        }

        public override async Task<Empty> StartSync(Empty request, ServerCallContext context)
        {
            _logger.LogInformation("StartSync() called");

            await _messageBroker.SyncCurrentStatusAsync(context.CancellationToken);

            return new Empty();
        }

        public override Task<Summary> GetSummary(Empty request, ServerCallContext context)
        {
            throw new NotImplementedException();
        }

        public override Task SubscribeSnapshots(Empty request, IServerStreamWriter<Snapshot> responseStream, ServerCallContext context)
        {
            _logger.LogInformation("SubscribeSnapshots() called");

            _messageBroker.SubscribeSnapshots(responseStream, context.CancellationToken);

            return Task.Delay(-1, context.CancellationToken);
        }

        public override Task SubscribeSummaries(Empty request, IServerStreamWriter<Summary> responseStream, ServerCallContext context)
        {
            _logger.LogInformation("SubscribeSummaries() called");

            _messageBroker.SubscribeSummaries(responseStream, context.CancellationToken);

            return Task.Delay(-1, context.CancellationToken);
        }
    }
}
