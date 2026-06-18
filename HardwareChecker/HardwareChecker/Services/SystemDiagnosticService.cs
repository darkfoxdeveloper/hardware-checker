using Microsoft.Win32;
using HardwareChecker.Helpers;
using HardwareChecker.Models;

namespace HardwareChecker.Services;

public sealed class SystemDiagnosticService
{
    public Task<SystemResult> AnalyzeAsync() => Task.Run(() =>
    {
        var alerts = new List<Alert>();
        var computer = WmiHelper.Query("SELECT Manufacturer, Model FROM Win32_ComputerSystem").FirstOrDefault();
        var bios = WmiHelper.Query("SELECT SMBIOSBIOSVersion, ReleaseDate FROM Win32_BIOS").FirstOrDefault();
        var tpm = WmiHelper.Query("SELECT IsEnabled_InitialValue, IsActivated_InitialValue FROM Win32_Tpm", @"root\CIMV2\Security\MicrosoftTpm").FirstOrDefault();

        var secureBoot = ReadSecureBootStatus();
        var tpmStatus = ReadTpmStatus(tpm);
        var biosDate = ParseWmiDate(WmiHelper.GetValue<string>(bios, "ReleaseDate"));
        var status = DiagnosticStatus.Ok;

        if (secureBoot == "Desactivado")
        {
            status = DiagnosticStatus.Warning;
            alerts.Add(new Alert(
                status,
                "Sistema",
                "Secure Boot aparece desactivado.",
                "Activalo desde UEFI/BIOS si tu instalacion de Windows y tus dispositivos lo soportan."));
        }

        if (tpmStatus == "Desactivado")
        {
            status = DiagnosticStatus.Warning;
            alerts.Add(new Alert(
                status,
                "Sistema",
                "TPM aparece desactivado.",
                "Activa TPM/fTPM/PTT desde UEFI/BIOS si necesitas cifrado, Windows Hello o requisitos modernos de Windows."));
        }

        if (biosDate.HasValue && biosDate.Value < DateTime.Now.AddYears(-6))
        {
            status = DiagnosticStatus.Warning;
            alerts.Add(new Alert(
                status,
                "Sistema",
                $"La BIOS/UEFI parece antigua ({biosDate:yyyy-MM-dd}).",
                "Revisa la pagina del fabricante antes de actualizar BIOS; hazlo solo con bateria/corriente estable y siguiendo sus instrucciones."));
        }

        return new SystemResult
        {
            Manufacturer = WmiHelper.GetValue<string>(computer, "Manufacturer") ?? "No disponible",
            Model = WmiHelper.GetValue<string>(computer, "Model") ?? "No disponible",
            BiosVersion = WmiHelper.GetValue<string>(bios, "SMBIOSBIOSVersion") ?? "No disponible",
            BiosDate = biosDate,
            TpmStatus = tpmStatus,
            SecureBootStatus = secureBoot,
            Status = status,
            Alerts = alerts
        };
    });

    private static string ReadSecureBootStatus()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SecureBoot\State");
            var value = key?.GetValue("UEFISecureBootEnabled");
            return value switch
            {
                1 => "Activado",
                0 => "Desactivado",
                _ => "No disponible"
            };
        }
        catch
        {
            return "No disponible";
        }
    }

    private static string ReadTpmStatus(System.Management.ManagementObject? tpm)
    {
        if (tpm is null)
        {
            return "No disponible";
        }

        var enabled = WmiHelper.GetValue<bool>(tpm, "IsEnabled_InitialValue");
        var activated = WmiHelper.GetValue<bool>(tpm, "IsActivated_InitialValue");
        return enabled && activated ? "Activado" : "Desactivado";
    }

    private static DateTime? ParseWmiDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 8)
        {
            return null;
        }

        return DateTime.TryParseExact(value[..8], "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var date)
            ? date
            : null;
    }
}
