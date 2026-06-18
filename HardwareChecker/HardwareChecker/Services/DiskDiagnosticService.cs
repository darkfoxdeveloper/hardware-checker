using System.IO;
using HardwareChecker.Helpers;
using HardwareChecker.Models;

namespace HardwareChecker.Services;

public sealed class DiskDiagnosticService
{
    public Task<List<DiskResult>> AnalyzeAsync() => Task.Run(() =>
    {
        var smartStatus = ReadSmartStatus();
        var physicalDisks = ReadPhysicalDiskTypes();
        var results = new List<DiskResult>();

        foreach (var drive in DriveInfo.GetDrives().Where(drive => drive.IsReady))
        {
            var totalGb = FormatHelper.BytesToGb(drive.TotalSize);
            var freeGb = FormatHelper.BytesToGb(drive.AvailableFreeSpace);
            var usedGb = Math.Max(0, totalGb - freeGb);
            var usagePercent = totalGb > 0 ? usedGb / totalGb * 100 : 0;
            var alerts = new List<Alert>();
            var status = DiagnosticStatus.Ok;

            if (freeGb < 2 || usagePercent >= 95)
            {
                status = DiagnosticStatus.Critical;
                alerts.Add(new Alert(status, "Disco", $"La unidad {drive.Name} tiene muy poco espacio libre."));
            }
            else if (freeGb < 10 || usagePercent >= 90)
            {
                status = DiagnosticStatus.Warning;
                alerts.Add(new Alert(status, "Disco", $"La unidad {drive.Name} esta cerca de llenarse."));
            }

            var smart = smartStatus.Count > 0 && smartStatus.Values.Any(failing => failing)
                ? "Posible fallo"
                : smartStatus.Count > 0 ? "Correcto" : "No disponible";

            if (smart == "Posible fallo")
            {
                status = DiagnosticStatus.Critical;
                alerts.Add(new Alert(status, "Disco", $"SMART informa posible fallo asociado a {drive.Name}."));
            }

            results.Add(new DiskResult
            {
                Name = drive.Name,
                Label = drive.VolumeLabel,
                FileSystem = drive.DriveFormat,
                TotalGb = totalGb,
                UsedGb = usedGb,
                FreeGb = freeGb,
                UsagePercent = usagePercent,
                DriveType = GuessDriveType(physicalDisks),
                SmartStatus = smart,
                Status = status,
                Alerts = alerts
            });
        }

        return results;
    });

    private static Dictionary<string, bool> ReadSmartStatus()
    {
        var result = new Dictionary<string, bool>();
        foreach (var item in WmiHelper.Query("SELECT InstanceName, PredictFailure FROM MSStorageDriver_FailurePredictStatus", @"root\WMI"))
        {
            var instance = WmiHelper.GetValue<string>(item, "InstanceName") ?? Guid.NewGuid().ToString();
            result[instance] = WmiHelper.GetValue<bool>(item, "PredictFailure");
        }

        return result;
    }

    private static List<string> ReadPhysicalDiskTypes()
    {
        return WmiHelper.Query("SELECT MediaType, Model, InterfaceType FROM Win32_DiskDrive")
            .Select(disk =>
            {
                var mediaType = WmiHelper.GetValue<string>(disk, "MediaType") ?? string.Empty;
                var model = WmiHelper.GetValue<string>(disk, "Model") ?? string.Empty;
                var interfaceType = WmiHelper.GetValue<string>(disk, "InterfaceType") ?? string.Empty;
                return $"{mediaType} {model} {interfaceType}";
            })
            .ToList();
    }

    private static string GuessDriveType(List<string> physicalDisks)
    {
        var text = string.Join(" ", physicalDisks).ToUpperInvariant();
        if (text.Contains("NVME"))
        {
            return "NVMe";
        }

        if (text.Contains("SSD") || text.Contains("SOLID"))
        {
            return "SSD";
        }

        if (text.Contains("HDD") || text.Contains("FIXED HARD DISK"))
        {
            return "HDD";
        }

        return "Desconocido";
    }
}
