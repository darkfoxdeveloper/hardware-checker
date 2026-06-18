namespace HardwareChecker.Models;

public sealed record Alert(
    DiagnosticStatus Status,
    string Category,
    string Message,
    string Recommendation = "");
