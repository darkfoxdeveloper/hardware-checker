using HardwareChecker.Helpers;
using HardwareChecker.Models;

namespace HardwareChecker.Services;

public sealed class BatteryDiagnosticService
{
    public Task<BatteryResult> AnalyzeAsync() => Task.Run(() =>
    {
        var battery = WmiHelper.Query("SELECT EstimatedChargeRemaining, BatteryStatus, DesignCapacity, FullChargeCapacity FROM Win32_Battery").FirstOrDefault();
        if (battery is null)
        {
            return new BatteryResult
            {
                IsPresent = false,
                Status = DiagnosticStatus.Ok,
                PowerState = "Equipo sin bateria detectada",
                Health = "No aplica"
            };
        }

        var charge = (int)WmiHelper.GetValue<ushort>(battery, "EstimatedChargeRemaining");
        var statusText = BatteryStatusToText(WmiHelper.GetValue<ushort>(battery, "BatteryStatus"));
        var designCapacity = WmiHelper.GetValue<uint>(battery, "DesignCapacity");
        var fullChargeCapacity = WmiHelper.GetValue<uint>(battery, "FullChargeCapacity");
        var health = "No disponible";
        var alerts = new List<Alert>();
        var status = DiagnosticStatus.Ok;

        if (designCapacity > 0 && fullChargeCapacity > 0)
        {
            var healthPercent = fullChargeCapacity / (double)designCapacity * 100;
            health = $"{healthPercent:0}%";
            if (healthPercent < 50)
            {
                status = DiagnosticStatus.Critical;
                alerts.Add(new Alert(status, "Bateria", $"Salud de bateria critica: {health}."));
            }
            else if (healthPercent < 70)
            {
                status = DiagnosticStatus.Warning;
                alerts.Add(new Alert(status, "Bateria", $"Salud de bateria reducida: {health}."));
            }
        }

        if (charge < 10 && !statusText.Contains("Cargando", StringComparison.OrdinalIgnoreCase))
        {
            status = DiagnosticStatus.Critical;
            alerts.Add(new Alert(status, "Bateria", $"Nivel de bateria critico: {charge}%."));
        }
        else if (charge < 20 && status != DiagnosticStatus.Critical)
        {
            status = DiagnosticStatus.Warning;
            alerts.Add(new Alert(status, "Bateria", $"Nivel de bateria bajo: {charge}%."));
        }

        return new BatteryResult
        {
            IsPresent = true,
            ChargePercent = charge,
            PowerState = statusText,
            Health = health,
            Status = status,
            Alerts = alerts
        };
    });

    private static string BatteryStatusToText(ushort status) => status switch
    {
        1 => "Descargando",
        2 => "Conectado",
        3 => "Cargada",
        4 => "Baja",
        5 => "Critica",
        6 => "Cargando",
        7 => "Cargando y alta",
        8 => "Cargando y baja",
        9 => "Cargando y critica",
        10 => "Indefinido",
        11 => "Parcialmente cargada",
        _ => "No disponible"
    };
}
