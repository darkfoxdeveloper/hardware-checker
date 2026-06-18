namespace HardwareChecker.Models;

public sealed class GpuResult
{
    public string Name { get; init; } = "GPU no detectada";
    public double? DedicatedMemoryGb { get; init; }
    public double? UsagePercent { get; init; }
    public double? TemperatureCelsius { get; init; }
    public DiagnosticStatus Status { get; init; } = DiagnosticStatus.Unknown;
    public List<Alert> Alerts { get; init; } = [];
    public string Notes { get; init; } = string.Empty;
}
