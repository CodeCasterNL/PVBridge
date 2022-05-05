using CodeCaster.PVBridge.Grpc;
using Grpc.Core;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCaster.PVBridge.Service.Grpc
{
    public interface IClientAndServiceMessageBroker : IClientMessageBroker
    {
        Task SyncCurrentStatusAsync(CancellationToken cancellationToken);

        void SubscribeSnapshots(IServerStreamWriter<Snapshot> responseStream, CancellationToken cancellationToken);
        void SubscribeSummaries(IServerStreamWriter<Summary> responseStream, CancellationToken cancellationToken);
        
    }
}