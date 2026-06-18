namespace HardwareChecker.Models;

public sealed record CategorySummary(
    string Name,
    DiagnosticStatus Status,
    string PrimaryValue,
    string SecondaryValue,
    string Icon = "i",
    string Description = "");
