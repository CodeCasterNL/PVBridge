using CodeCaster.PVBridge.Grpc;
using GrpcDotNetNamedPipes;

namespace CodeCaster.PVBridge.ConfigurationUI.WinForms.Grpc
{
    internal class GrpcClientFactory
    {
        private static NamedPipeChannel? _channel;

        public PVBridgeService.PVBridgeServiceClient CreateGrpcClient()
        {
            // Create one channel per application lifetime, it's clients you want to use short-lived.
            // TODO: get from config
            _channel ??= new NamedPipeChannel(".", "PVBRIDGE", new NamedPipeChannelOptions { ConnectionTimeout = 1 });

            return new PVBridgeService.PVBridgeServiceClient(_channel);
        }
    }
}
