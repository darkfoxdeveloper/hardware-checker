using HardwareChecker.Helpers;
using HardwareChecker.Models;

namespace HardwareChecker.Services;

public sealed class MemoryDiagnosticService
{
    public Task<MemoryResult> AnalyzeAsync() => Task.Run(() =>
    {
        var os = WmiHelper.Query("SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem").FirstOrDefault();
        var totalGb = FormatHelper.KbToGb(WmiHelper.GetValue<ulong>(os, "TotalVisibleMemorySize"));
        var freeGb = FormatHelper.KbToGb(WmiHelper.GetValue<ulong>(os, "FreePhysicalMemory"));
        var usedGb = Math.Max(0, totalGb - freeGb);
        var usagePercent = totalGb > 0 ? usedGb / totalGb * 100 : 0;
        var alerts = new List<Alert>();
        var status = DiagnosticStatus.Ok;

        if (totalGb <= 0)
        {
            status = DiagnosticStatus.Unknown;
            alerts.Add(new Alert(status, "RAM", "No se pudo leer la memoria fisica mediante WMI."));
        }
        else if (usagePercent >= 95 || freeGb < 0.5)
        {
            status = DiagnosticStatus.Critical;
            alerts.Add(new Alert(status, "RAM", $"Memoria casi agotada: {freeGb:0.0} GB libres."));
        }
        else if (usagePercent >= 85 || freeGb < 1.5)
        {
            status = DiagnosticStatus.Warning;
            alerts.Add(new Alert(status, "RAM", $"Memoria disponible baja: {freeGb:0.0} GB libres."));
        }

        return new MemoryResult
        {
            TotalGb = totalGb,
            UsedGb = usedGb,
            FreeGb = freeGb,
            UsagePercent = usagePercent,
            Status = status,
            Alerts = alerts
        };
    });
}
