namespace HardwareChecker.Models;

public sealed class BatteryResult
{
    public bool IsPresent { get; init; }
    public int? ChargePercent { get; init; }
    public string PowerState { get; init; } = "No disponible";
    public string Health { get; init; } = "No disponible";
    public DiagnosticStatus Status { get; init; } = DiagnosticStatus.Unknown;
    public List<Alert> Alerts { get; init; } = [];
    public string Notes { get; init; } = string.Empty;
}
