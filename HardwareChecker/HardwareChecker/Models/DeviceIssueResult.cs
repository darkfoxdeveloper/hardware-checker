namespace HardwareChecker.Models;

public sealed class DeviceIssueResult
{
    public string Name { get; init; } = string.Empty;
    public string DeviceId { get; init; } = string.Empty;
    public int ErrorCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public string DriverVersion { get; init; } = "No disponible";
    public DateTime? DriverDate { get; init; }
    public DiagnosticStatus Status { get; init; } = DiagnosticStatus.Warning;
}
