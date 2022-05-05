using System.Collections.Generic;
using System.Text.Json.Serialization;
using CodeCaster.PVBridge.Configuration;

namespace CodeCaster.PVBridge.PVOutput
{
    public class PVOutputConfiguration : DataProviderConfiguration
    {
        public PVOutputConfiguration()
        {
            Type = "PVOutput";
        }

        public PVOutputConfiguration(DataProviderConfiguration config) : base(config) { }

        [JsonIgnore]
        public string? SystemId
        {
            get => Options.GetValueOrDefault(nameof(SystemId));
            set => Options[nameof(SystemId)] = value;
        }
    }
}
