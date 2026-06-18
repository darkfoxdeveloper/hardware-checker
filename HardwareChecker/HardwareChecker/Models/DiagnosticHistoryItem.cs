namespace HardwareChecker.Models;

public sealed record DiagnosticHistoryItem(
    DateTime Timestamp,
    int HealthScore,
    DiagnosticStatus Status,
    int AlertCount,
    string DeltaText);
