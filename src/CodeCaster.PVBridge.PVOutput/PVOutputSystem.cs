using PVOutput.Net.Objects;

namespace CodeCaster.PVBridge.PVOutput
{
    public record PVOutputSystem(int Id, ISystem system)
    {
        public string Description = $"{system.SystemName} ({system.SystemSize} WP)";

        public string DisplayString => $"{system.SystemName}, " +
                               $"inverter: {system.InverterBrand}, " +
                               $"panels: {system.PanelBrand} " +
                               $"({system.SystemSize} WP), " +
                               $"installed {system.InstallDate}";
    }
}
