namespace HardwareChecker.Models;

public sealed class SystemResult
{
    public string Manufacturer { get; init; } = "No disponible";
    public string Model { get; init; } = "No disponible";
    public string BiosVersion { get; init; } = "No disponible";
    public DateTime? BiosDate { get; init; }
    public string TpmStatus { get; init; } = "No disponible";
    public string SecureBootStatus { get; init; } = "No disponible";
    public DiagnosticStatus Status { get; init; } = DiagnosticStatus.Unknown;
    public List<Alert> Alerts { get; init; } = [];
}
