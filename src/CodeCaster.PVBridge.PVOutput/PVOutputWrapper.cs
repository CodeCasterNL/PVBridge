using System;
using PVOutput.Net;
using PVOutput.Net.DependencyInjection;

namespace CodeCaster.PVBridge.PVOutput
{
    // ReSharper disable once InconsistentNaming
    public class PVOutputWrapper
    {
        public PVOutputClient ApiClient { get; }

        public PVOutputWrapper(PVOutputConfiguration configuration)
        {
            if (!int.TryParse(configuration.SystemId, out var systemId))
            {
                throw new ArgumentException($"{nameof(configuration.SystemId)} value of '{configuration.SystemId}' must be an integer.");
            }

            var options = new PVOutputClientOptions
            {
                ApiKey = configuration.Key,
                OwnedSystemId = systemId,
            };
            
            ApiClient = new PVOutputClient(options);
        }
    }
}