using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using CodeCaster.WindowsServiceExtensions.Service;
using GrpcDotNetNamedPipes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CodeCaster.PVBridge.Service.Grpc
{
    internal class GrpcService : WindowsServiceBackgroundService
    {
        private NamedPipeServer? _server;
        
        private PVBridgeGrpcService _grpcService;

        public GrpcService(ILoggerFactory loggerFactory, IClientAndServiceMessageBroker messageBroker, IHostLifetime hostLifetime)
            : base(loggerFactory.CreateLogger<GrpcService>(), hostLifetime)
        {
            _grpcService = new PVBridgeGrpcService(loggerFactory.CreateLogger<PVBridgeGrpcService>(), messageBroker);
        }

        protected override Task TryExecuteAsync(CancellationToken serviceStopToken)
        {
            // TODO: UI can't talk to service, wait for https://github.com/cyanfish/grpc-dotnet-namedpipes/pull/39 to be merged and released.
            _server = new NamedPipeServer("PVBRIDGE", new NamedPipeServerOptions
            {
                CurrentUserOnly = false,
            });

            PVBridge.Grpc.PVBridgeService.BindService(_server.ServiceBinder, _grpcService);
            
            _server.Start();
        
            return Task.Delay(-1, serviceStopToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _server!.Kill();

            return base.StopAsync(cancellationToken);
        }

        public override void OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            // TODO: recreate _grpcService, see PvBridgeService
        }

        public override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            // TODO: recreate _grpcService, see PvBridgeService
        }
    }
}
