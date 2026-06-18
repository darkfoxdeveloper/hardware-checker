using HardwareChecker.Helpers;
using HardwareChecker.Models;

namespace HardwareChecker.Services;

public sealed class GpuDiagnosticService
{
    public Task<List<GpuResult>> AnalyzeAsync() => Task.Run(() =>
    {
        var gpus = WmiHelper.Query("SELECT Name, AdapterRAM, Status FROM Win32_VideoController");
        if (gpus.Count == 0)
        {
            return new List<GpuResult>
            {
                new()
                {
                    Status = DiagnosticStatus.Unknown,
                    Notes = "Windows no devolvio informacion de GPU mediante WMI."
                }
            };
        }

        return gpus.Select(gpu =>
        {
            var adapterRam = WmiHelper.GetValue<uint>(gpu, "AdapterRAM");
            var windowsStatus = WmiHelper.GetValue<string>(gpu, "Status") ?? "No disponible";
            var status = windowsStatus.Equals("OK", StringComparison.OrdinalIgnoreCase)
                ? DiagnosticStatus.Ok
                : DiagnosticStatus.Warning;
            var alerts = new List<Alert>();

            if (status == DiagnosticStatus.Warning)
            {
                alerts.Add(new Alert(status, "GPU", $"Windows informa estado de GPU: {windowsStatus}."));
            }

            return new GpuResult
            {
                Name = WmiHelper.GetValue<string>(gpu, "Name") ?? "GPU no detectada",
                DedicatedMemoryGb = adapterRam > 0 ? FormatHelper.BytesToGb(adapterRam) : null,
                UsagePercent = null,
                TemperatureCelsius = null,
                Status = status,
                Alerts = alerts,
                Notes = "Uso y temperatura de GPU dependen del proveedor y no estan disponibles en WMI estandar."
            };
        }).ToList();
    });
}
