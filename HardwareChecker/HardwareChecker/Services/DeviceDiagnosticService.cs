using HardwareChecker.Helpers;
using HardwareChecker.Models;

namespace HardwareChecker.Services;

public sealed class DeviceDiagnosticService
{
    public Task<List<DeviceIssueResult>> AnalyzeAsync() => Task.Run(() =>
    {
        var driverInfo = ReadDriverInfo();
        var devices = WmiHelper.Query("SELECT Name, DeviceID, ConfigManagerErrorCode FROM Win32_PnPEntity WHERE ConfigManagerErrorCode <> 0");

        var issues = devices.Select(device =>
        {
            var deviceId = WmiHelper.GetValue<string>(device, "DeviceID") ?? string.Empty;
            var errorCode = (int)WmiHelper.GetValue<uint>(device, "ConfigManagerErrorCode");
            driverInfo.TryGetValue(deviceId, out var driver);

            return new DeviceIssueResult
            {
                Name = WmiHelper.GetValue<string>(device, "Name") ?? "Dispositivo sin nombre",
                DeviceId = deviceId,
                ErrorCode = errorCode,
                Message = ExplainConfigManagerCode(errorCode),
                DriverVersion = string.IsNullOrWhiteSpace(driver.Version) ? "No disponible" : driver.Version,
                DriverDate = driver.Date,
                Status = DiagnosticStatus.Warning
            };
        }).ToList();

        AddPotentiallyOutdatedDrivers(issues);
        return issues;
    });

    private static Dictionary<string, (string Version, DateTime? Date)> ReadDriverInfo()
    {
        return WmiHelper.Query("SELECT DeviceID, DriverVersion, DriverDate FROM Win32_PnPSignedDriver")
            .Select(driver => new
            {
                DeviceId = WmiHelper.GetValue<string>(driver, "DeviceID") ?? string.Empty,
                Version = WmiHelper.GetValue<string>(driver, "DriverVersion") ?? "No disponible",
                Date = ParseWmiDate(WmiHelper.GetValue<string>(driver, "DriverDate"))
            })
            .Where(driver => !string.IsNullOrWhiteSpace(driver.DeviceId))
            .GroupBy(driver => driver.DeviceId)
            .ToDictionary(group => group.Key, group => (group.First().Version, group.First().Date));
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

    private static void AddPotentiallyOutdatedDrivers(List<DeviceIssueResult> issues)
    {
        var existingIds = issues.Select(issue => issue.DeviceId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var threshold = DateTime.Now.AddYears(-5);
        var oldDrivers = WmiHelper.Query("SELECT DeviceID, DeviceName, DriverVersion, DriverDate FROM Win32_PnPSignedDriver")
            .Select(driver => new
            {
                DeviceId = WmiHelper.GetValue<string>(driver, "DeviceID") ?? string.Empty,
                Name = WmiHelper.GetValue<string>(driver, "DeviceName") ?? "Dispositivo sin nombre",
                Version = WmiHelper.GetValue<string>(driver, "DriverVersion") ?? "No disponible",
                Date = ParseWmiDate(WmiHelper.GetValue<string>(driver, "DriverDate"))
            })
            .Where(driver => !string.IsNullOrWhiteSpace(driver.DeviceId))
            .Where(driver => driver.Date.HasValue && driver.Date.Value < threshold)
            .Where(driver => !existingIds.Contains(driver.DeviceId))
            .OrderBy(driver => driver.Date)
            .Take(8);

        foreach (var driver in oldDrivers)
        {
            issues.Add(new DeviceIssueResult
            {
                Name = driver.Name,
                DeviceId = driver.DeviceId,
                ErrorCode = 0,
                Message = "Driver antiguo detectado. Conviene revisar si existe una version mas reciente del fabricante.",
                DriverVersion = driver.Version,
                DriverDate = driver.Date,
                Status = DiagnosticStatus.Warning
            });
        }
    }

    private static string ExplainConfigManagerCode(int code) => code switch
    {
        1 => "Dispositivo no configurado correctamente.",
        10 => "El dispositivo no puede iniciar.",
        18 => "Reinstalar los controladores del dispositivo.",
        22 => "Dispositivo deshabilitado.",
        28 => "Controladores no instalados.",
        31 => "Windows no puede cargar los controladores.",
        43 => "Windows detuvo el dispositivo por un problema.",
        _ => $"Codigo de error del Administrador de dispositivos: {code}."
    };
}
