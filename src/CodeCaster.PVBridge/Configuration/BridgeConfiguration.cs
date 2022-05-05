using System;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable 8618 // nullable checks and instantiation are done by the configuration system
// ReSharper disable PropertyCanBeMadeInitOnly.Global, AutoPropertyCanBeMadeGetOnly.Global - config class, remove when covered by tests
namespace CodeCaster.PVBridge.Configuration
{
    public class BridgeConfiguration
    {
        public const string SectionName = "PVBridge";

        public string GrpcAddress { get; set; }

        public List<DataProviderConfiguration> Providers { get; set; } = new();

        public List<InputToOutputConfiguration> InputToOutput { get; set; } = new();

        /// <summary>
        /// Fills in the provider details according to the <see cref="InputToOutput"/> configuration.
        ///
        /// TODO: return new type ProviderConfigured (input, output[], string, option[], ...), breaking
        /// </summary>
        /// <returns></returns>
        public IReadOnlyCollection<(DataProviderConfiguration, DataProviderConfiguration[])> ReadConfiguration()
        {
            var inputToOutputs = new List<(DataProviderConfiguration, DataProviderConfiguration[])>();

            foreach (var io in InputToOutput)
            {
                var inputProvider = FindProvider(io.Input);

                var outputProviders = io.Outputs.Select(FindProvider).ToArray();

                inputToOutputs.Add((inputProvider, outputProviders));
            }

            return inputToOutputs;
        }

        public DataProviderConfiguration FindProvider(string nameOrType)
        {
            var providers = Providers.Where(p =>
                    p.Type.Equals(nameOrType, StringComparison.InvariantCultureIgnoreCase)
                    || p.Name != null && p.Name.Equals(nameOrType, StringComparison.InvariantCultureIgnoreCase)
                    ).ToList();

            if (providers.Count != 1)
            {
                throw new ArgumentException($"Provider \"{nameOrType}\" could not be found or was ambiguous, specify the proper type or name.");
            }

            return providers[0];
        }
    }

    /// <summary>
    /// One input to one or more outputs.
    /// </summary>
    public class InputToOutputConfiguration
    {
        public string Input { get; set; }

        public List<string> Outputs { get; set; } = new();

        /// <summary>
        /// The installation date of the system, or how far back we go for data.
        /// </summary>
        public DateTime? SyncStart { get; set; }

    }
}
#pragma warning restore 8618
