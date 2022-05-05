using System.Linq;
using System.Threading.Tasks;
using CodeCaster.PVBridge.Configuration;
using CodeCaster.PVBridge.Configuration.Protection;
using Microsoft.Extensions.Options;

namespace CodeCaster.PVBridge.Service.CommandLine
{
    /// <summary>
    /// Reads config to pass to an <see cref="IInputToOutputWriter"/>.
    /// </summary>
    internal class ProviderProvider
    {
        private readonly BridgeConfiguration _configuration;

        public ProviderProvider(IOptions<BridgeConfiguration> options)
        {
            _configuration = options.Value;
        }

        internal async Task<(DataProviderConfiguration? input, DataProviderConfiguration? output)> GetConfigurationAsync(string? inputNameOrType, string? outputNameOrType)
        {
            await ConfigurationProtector.UnprotectAsync(_configuration);

            var loopConfigurations = _configuration.ReadConfiguration();

            if (loopConfigurations.Count == 0)
            {
                // TODO: that's an error

                return default;
            }

            var (input, outputs) = loopConfigurations.Count == 1 && (inputNameOrType == null || outputNameOrType == null)
                ? loopConfigurations.First()
                : default;

            if (input == null)
            {
                if (!string.IsNullOrWhiteSpace(inputNameOrType))
                {
                    input = _configuration.FindProvider(inputNameOrType);
                }
                else
                {
                    // TODO: that's an error
                
                    return default;
                }
            }

            DataProviderConfiguration output;

            if (outputs is { Length: 1 })
            {
                output = outputs[0];
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(outputNameOrType))
                {
                    output = _configuration.FindProvider(outputNameOrType);
                }
                else
                {
                    // TODO: that's an error

                    return default;
                }
            }

            return (input, output);
        }
    }
}
