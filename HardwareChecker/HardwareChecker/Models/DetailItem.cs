namespace HardwareChecker.Models;

public sealed record DetailItem(
    string Label,
    string Value,
    DiagnosticStatus Status = DiagnosticStatus.Unknown,
    string Note = "");
