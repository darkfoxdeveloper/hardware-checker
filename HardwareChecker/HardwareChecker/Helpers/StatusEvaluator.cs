using HardwareChecker.Models;

namespace HardwareChecker.Helpers;

public static class StatusEvaluator
{
    public static DiagnosticStatus Worst(params DiagnosticStatus[] statuses)
    {
        if (statuses.Contains(DiagnosticStatus.Critical))
        {
            return DiagnosticStatus.Critical;
        }

        if (statuses.Contains(DiagnosticStatus.Warning))
        {
            return DiagnosticStatus.Warning;
        }

        if (statuses.Contains(DiagnosticStatus.Unknown))
        {
            return DiagnosticStatus.Unknown;
        }

        return DiagnosticStatus.Ok;
    }

    public static DiagnosticStatus Worst(IEnumerable<DiagnosticStatus> statuses) => Worst(statuses.ToArray());
}
