namespace HardwareChecker.Models;

public sealed class NetworkAdapterResult
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string LocalIp { get; init; } = "No disponible";
    public double? SpeedMbps { get; init; }
    public DiagnosticStatus DiagnosticStatus { get; init; } = DiagnosticStatus.Unknown;
    public List<Alert> Alerts { get; init; } = [];
}
