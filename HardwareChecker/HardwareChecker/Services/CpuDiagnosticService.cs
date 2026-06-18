using System.Diagnostics;
using HardwareChecker.Helpers;
using HardwareChecker.Models;

namespace HardwareChecker.Services;

public sealed class CpuDiagnosticService
{
    public Task<CpuResult> AnalyzeAsync() => Task.Run(async () =>
    {
        var alerts = new List<Alert>();
        var notes = new List<string>();
        var cpu = WmiHelper.Query("SELECT Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed FROM Win32_Processor").FirstOrDefault();

        var usage = await ReadCpuUsageAsync();
        var name = WmiHelper.GetValue<string>(cpu, "Name") ?? "CPU no detectada";
        var cores = WmiHelper.GetValue<uint>(cpu, "NumberOfCores");
        var logical = WmiHelper.GetValue<uint>(cpu, "NumberOfLogicalProcessors");
        var maxClock = WmiHelper.GetValue<uint>(cpu, "MaxClockSpeed");
        var temperature = ReadTemperature();

        if (temperature is null)
        {
            notes.Add("La temperatura de CPU no esta disponible mediante los sensores WMI estandar.");
        }

        var status = DiagnosticStatus.Ok;
        if (usage >= 95)
        {
            status = DiagnosticStatus.Critical;
            alerts.Add(new Alert(status, "CPU", $"Uso de CPU critico: {usage:0}%."));
        }
        else if (usage >= 85)
        {
            status = DiagnosticStatus.Warning;
            alerts.Add(new Alert(status, "CPU", $"Uso de CPU elevado: {usage:0}%."));
        }

        return new CpuResult
        {
            Name = name.Trim(),
            UsagePercent = usage,
            TemperatureCelsius = temperature,
            Cores = (int)cores,
            LogicalProcessors = (int)logical,
            FrequencyMhz = maxClock > 0 ? maxClock : null,
            Status = status,
            Alerts = alerts,
            Notes = string.Join(" ", notes)
        };
    });

    private static async Task<double> ReadCpuUsageAsync()
    {
        try
        {
            using var counter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            counter.NextValue();
            await Task.Delay(750);
            return Math.Clamp(counter.NextValue(), 0, 100);
        }
        catch
        {
            var processor = WmiHelper.Query("SELECT LoadPercentage FROM Win32_Processor").FirstOrDefault();
            return WmiHelper.GetValue<uint>(processor, "LoadPercentage");
        }
    }

    private static double? ReadTemperature()
    {
        var sensors = WmiHelper.Query("SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature", @"root\WMI");
        var values = sensors
            .Select(sensor => WmiHelper.GetValue<uint>(sensor, "CurrentTemperature"))
            .Where(value => value > 0)
            .Select(value => value / 10d - 273.15d)
            .Where(value => value is > -40 and < 130)
            .ToList();

        return values.Count > 0 ? values.Average() : null;
    }
}
