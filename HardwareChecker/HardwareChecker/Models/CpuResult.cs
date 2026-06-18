namespace HardwareChecker.Models;

public sealed class CpuResult
{
    public string Name { get; init; } = "CPU no detectada";
    public double UsagePercent { get; init; }
    public double? TemperatureCelsius { get; init; }
    public int Cores { get; init; }
    public int LogicalProcessors { get; init; }
    public double? FrequencyMhz { get; init; }
    public DiagnosticStatus Status { get; init; } = DiagnosticStatus.Unknown;
    public List<Alert> Alerts { get; init; } = [];
    public string Notes { get; init; } = string.Empty;
}
