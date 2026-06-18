namespace HardwareChecker.Models;

public sealed class MemoryResult
{
    public double TotalGb { get; init; }
    public double UsedGb { get; init; }
    public double FreeGb { get; init; }
    public double UsagePercent { get; init; }
    public DiagnosticStatus Status { get; init; } = DiagnosticStatus.Unknown;
    public List<Alert> Alerts { get; init; } = [];
    public string Notes { get; init; } = string.Empty;
}
