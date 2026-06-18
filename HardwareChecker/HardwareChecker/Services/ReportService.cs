using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using HardwareChecker.Helpers;
using HardwareChecker.Models;

namespace HardwareChecker.Services;

public sealed class ReportService
{
    public string ToJson(HardwareReport report)
    {
        return JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    public string ToText(HardwareReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Informe de diagnostico de hardware");
        builder.AppendLine($"Generado: {report.GeneratedAt:G}");
        builder.AppendLine($"Equipo: {report.MachineName}");
        builder.AppendLine($"Usuario: {report.UserName}");
        builder.AppendLine($"Sistema operativo: {report.OperatingSystem}");
        builder.AppendLine($"Estado general: {report.OverallStatus}");
        builder.AppendLine();

        if (report.System is not null)
        {
            builder.AppendLine("Sistema");
            builder.AppendLine($"- Fabricante: {report.System.Manufacturer}");
            builder.AppendLine($"- Modelo: {report.System.Model}");
            builder.AppendLine($"- BIOS/UEFI: {report.System.BiosVersion}");
            builder.AppendLine($"- Fecha BIOS: {report.System.BiosDate?.ToString("yyyy-MM-dd") ?? "No disponible"}");
            builder.AppendLine($"- TPM: {report.System.TpmStatus}");
            builder.AppendLine($"- Secure Boot: {report.System.SecureBootStatus}");
            builder.AppendLine();
        }

        if (report.Cpu is not null)
        {
            builder.AppendLine("CPU");
            builder.AppendLine($"- Nombre: {report.Cpu.Name}");
            builder.AppendLine($"- Uso: {FormatHelper.Percent(report.Cpu.UsagePercent)}");
            builder.AppendLine($"- Temperatura: {FormatHelper.OptionalTemperature(report.Cpu.TemperatureCelsius)}");
            builder.AppendLine($"- Nucleos/Hilos: {report.Cpu.Cores}/{report.Cpu.LogicalProcessors}");
            builder.AppendLine($"- Frecuencia: {FormatHelper.OptionalMhz(report.Cpu.FrequencyMhz)}");
            builder.AppendLine();
        }

        if (report.Memory is not null)
        {
            builder.AppendLine("RAM");
            builder.AppendLine($"- Total: {FormatHelper.Gb(report.Memory.TotalGb)}");
            builder.AppendLine($"- Usada: {FormatHelper.Gb(report.Memory.UsedGb)} ({FormatHelper.Percent(report.Memory.UsagePercent)})");
            builder.AppendLine($"- Libre: {FormatHelper.Gb(report.Memory.FreeGb)}");
            builder.AppendLine();
        }

        builder.AppendLine("Discos");
        foreach (var disk in report.Disks)
        {
            builder.AppendLine($"- {disk.Name} {disk.Label} {disk.DriveType}: {FormatHelper.Gb(disk.FreeGb)} libres de {FormatHelper.Gb(disk.TotalGb)}. SMART: {disk.SmartStatus}");
        }

        builder.AppendLine();
        builder.AppendLine("GPU");
        foreach (var gpu in report.Gpus)
        {
            builder.AppendLine($"- {gpu.Name}: memoria {gpu.DedicatedMemoryGb?.ToString("0.0 GB") ?? "No disponible"}, estado {gpu.Status}");
        }

        builder.AppendLine();
        builder.AppendLine("Bateria");
        builder.AppendLine(report.Battery?.IsPresent == true
            ? $"- Carga: {report.Battery.ChargePercent}%, energia: {report.Battery.PowerState}, salud: {report.Battery.Health}"
            : "- No detectada");

        builder.AppendLine();
        builder.AppendLine("Red");
        foreach (var adapter in report.NetworkAdapters)
        {
            builder.AppendLine($"- {adapter.Name}: {adapter.LocalIp}, {adapter.SpeedMbps?.ToString("0 Mbps") ?? "velocidad no disponible"}");
        }

        builder.AppendLine();
        builder.AppendLine("Dispositivos con errores");
        if (report.DeviceIssues.Count == 0)
        {
            builder.AppendLine("- No se detectaron errores del Administrador de dispositivos.");
        }
        else
        {
            foreach (var issue in report.DeviceIssues)
            {
                builder.AppendLine($"- {issue.Name}: {issue.Message} Driver {issue.DriverVersion}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("Alertas");
        if (report.Alerts.Count == 0)
        {
            builder.AppendLine("- Sin alertas.");
        }
        else
        {
            foreach (var alert in report.Alerts)
            {
                builder.AppendLine($"- [{alert.Status}] {alert.Category}: {alert.Message}");
                if (!string.IsNullOrWhiteSpace(alert.Recommendation))
                {
                    builder.AppendLine($"  Sugerencia: {alert.Recommendation}");
                }
            }
        }

        return builder.ToString();
    }

    public string ToHtml(HardwareReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"es\"><head><meta charset=\"utf-8\"><title>Informe Hardware Checker</title>");
        builder.AppendLine("<style>");
        builder.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;background:#eef3f8;color:#111827;margin:0;padding:32px}");
        builder.AppendLine(".wrap{max-width:1100px;margin:auto}.panel{background:#fff;border:1px solid #d9e2ec;border-radius:10px;padding:20px;margin:0 0 16px}");
        builder.AppendLine("h1{margin:0 0 6px;font-size:30px}h2{margin:0 0 12px;font-size:18px}.muted{color:#667085}");
        builder.AppendLine(".grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:12px}.metric{background:#f8fafc;border:1px solid #d9e2ec;border-radius:8px;padding:14px}");
        builder.AppendLine(".ok{color:#16a34a}.warning{color:#d97706}.critical{color:#dc2626}.unknown{color:#64748b}");
        builder.AppendLine("table{width:100%;border-collapse:collapse}td,th{border-bottom:1px solid #e5edf5;padding:8px;text-align:left;vertical-align:top}");
        builder.AppendLine("</style></head><body><main class=\"wrap\">");
        builder.AppendLine($"<section class=\"panel\"><h1>Informe de diagnostico</h1><div class=\"muted\">{Escape(report.GeneratedAt.ToString("G"))} | {Escape(report.MachineName)} | {Escape(report.OperatingSystem)}</div></section>");

        builder.AppendLine("<section class=\"grid\">");
        foreach (var summary in report.Summaries)
        {
            builder.AppendLine($"<article class=\"metric\"><strong>{Escape(summary.Name)}</strong><h2>{Escape(summary.PrimaryValue)}</h2><div class=\"muted\">{Escape(summary.SecondaryValue)}</div><div class=\"{CssStatus(summary.Status)}\">{Escape(summary.Status.ToString())}</div></article>");
        }

        builder.AppendLine("</section>");
        builder.AppendLine("<section class=\"panel\"><h2>Alertas y sugerencias</h2>");
        if (report.Alerts.Count == 0)
        {
            builder.AppendLine("<p>Sin alertas.</p>");
        }
        else
        {
            builder.AppendLine("<table><thead><tr><th>Estado</th><th>Categoria</th><th>Mensaje</th><th>Sugerencia</th></tr></thead><tbody>");
            foreach (var alert in report.Alerts)
            {
                builder.AppendLine($"<tr><td class=\"{CssStatus(alert.Status)}\">{Escape(alert.Status.ToString())}</td><td>{Escape(alert.Category)}</td><td>{Escape(alert.Message)}</td><td>{Escape(alert.Recommendation)}</td></tr>");
            }

            builder.AppendLine("</tbody></table>");
        }

        builder.AppendLine("</section>");
        builder.AppendLine("<section class=\"panel\"><h2>Informe completo</h2><pre>");
        builder.AppendLine(Escape(ToText(report)));
        builder.AppendLine("</pre></section></main></body></html>");
        return builder.ToString();
    }

    public async Task ExportAsync(HardwareReport report, string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        var content = extension switch
        {
            ".json" => ToJson(report),
            ".html" or ".htm" => ToHtml(report),
            _ => ToText(report)
        };

        await File.WriteAllTextAsync(path, content, Encoding.UTF8);
    }

    private static string Escape(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private static string CssStatus(DiagnosticStatus status) => status switch
    {
        DiagnosticStatus.Ok => "ok",
        DiagnosticStatus.Warning => "warning",
        DiagnosticStatus.Critical => "critical",
        _ => "unknown"
    };
}
