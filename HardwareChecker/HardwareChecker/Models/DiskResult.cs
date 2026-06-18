namespace HardwareChecker.Models;

public sealed class DiskResult
{
    public string Name { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string FileSystem { get; init; } = string.Empty;
    public double TotalGb { get; init; }
    public double UsedGb { get; init; }
    public double FreeGb { get; init; }
    public double UsagePercent { get; init; }
    public string DriveType { get; init; } = "Desconocido";
    public string SmartStatus { get; init; } = "No disponible";
    public DiagnosticStatus Status { get; init; } = DiagnosticStatus.Unknown;
    public List<Alert> Alerts { get; init; } = [];
}
