using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using HardwareChecker.Helpers;
using HardwareChecker.Models;
using HardwareChecker.Services;

namespace HardwareChecker.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly SystemDiagnosticService _systemService = new();
    private readonly CpuDiagnosticService _cpuService = new();
    private readonly MemoryDiagnosticService _memoryService = new();
    private readonly DiskDiagnosticService _diskService = new();
    private readonly GpuDiagnosticService _gpuService = new();
    private readonly BatteryDiagnosticService _batteryService = new();
    private readonly NetworkDiagnosticService _networkService = new();
    private readonly DeviceDiagnosticService _deviceService = new();
    private readonly ReportService _reportService = new();
    private readonly DispatcherTimer _liveTimer = new();

    private HardwareReport? _currentReport;
    private CategorySummary? _selectedSummary;
    private Alert? _selectedAlert;
    private string _selectedAlertFilter = "Todas";
    private DiagnosticStatus _overallStatus = DiagnosticStatus.Unknown;
    private bool _isScanning;
    private bool _isLiveMonitoring;
    private bool _isLiveRefreshRunning;
    private double _progress;
    private int _healthScore;
    private string _statusMessage = "Listo para iniciar el diagnostico.";
    private string _detailsText = "Ejecuta un diagnostico para ver los detalles por categoria.";
    private string _lastScanText = "Sin diagnosticos ejecutados";
    private string _alertSummaryText = "Sin datos";
    private string _healthTrendText = "Sin comparacion";
    private string _liveMonitorText = "Activar monitor";

    public MainViewModel()
    {
        StartDiagnosticCommand = new AsyncRelayCommand(StartDiagnosticAsync, () => !IsScanning);
        ExportReportCommand = new AsyncRelayCommand(ExportReportAsync, () => CurrentReport is not null && !IsScanning);
        CopyReportCommand = new RelayCommand(CopyReportToClipboard, () => CurrentReport is not null);
        ResolveSelectedAlertCommand = new RelayCommand(OpenHelpForSelectedAlert, () => SelectedAlert is not null);
        ToggleLiveMonitorCommand = new RelayCommand(ToggleLiveMonitor, () => CurrentReport is not null && !IsScanning);
        AlertFilters = ["Todas", "Criticas", "Advertencias", "CPU", "RAM", "Disco", "GPU", "Bateria", "Red", "Sistema", "Dispositivos"];

        _liveTimer.Interval = TimeSpan.FromSeconds(5);
        _liveTimer.Tick += async (_, _) => await RefreshLiveMetricsAsync();

        LoadEmptySummaries();
    }

    public ObservableCollection<CategorySummary> Summaries { get; } = [];
    public ObservableCollection<Alert> Alerts { get; } = [];
    public ObservableCollection<Alert> FilteredAlerts { get; } = [];
    public ObservableCollection<DetailItem> SelectedDetails { get; } = [];
    public ObservableCollection<DiagnosticHistoryItem> DiagnosticHistory { get; } = [];
    public IReadOnlyList<string> AlertFilters { get; }

    public AsyncRelayCommand StartDiagnosticCommand { get; }
    public AsyncRelayCommand ExportReportCommand { get; }
    public RelayCommand CopyReportCommand { get; }
    public RelayCommand ResolveSelectedAlertCommand { get; }
    public RelayCommand ToggleLiveMonitorCommand { get; }

    public HardwareReport? CurrentReport
    {
        get => _currentReport;
        private set
        {
            if (SetProperty(ref _currentReport, value))
            {
                ExportReportCommand.RaiseCanExecuteChanged();
                CopyReportCommand.RaiseCanExecuteChanged();
                ToggleLiveMonitorCommand.RaiseCanExecuteChanged();
                RefreshSelectedDetails();
            }
        }
    }

    public CategorySummary? SelectedSummary
    {
        get => _selectedSummary;
        set
        {
            if (SetProperty(ref _selectedSummary, value))
            {
                RefreshSelectedDetails();
            }
        }
    }

    public Alert? SelectedAlert
    {
        get => _selectedAlert;
        set
        {
            if (SetProperty(ref _selectedAlert, value))
            {
                ResolveSelectedAlertCommand.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(SelectedAlertActionText));
            }
        }
    }

    public string SelectedAlertFilter
    {
        get => _selectedAlertFilter;
        set
        {
            if (SetProperty(ref _selectedAlertFilter, value))
            {
                ApplyAlertFilter();
            }
        }
    }

    public string SelectedAlertActionText
    {
        get
        {
            if (SelectedAlert is null)
            {
                return "Selecciona una alerta";
            }

            return SelectedAlert.Category switch
            {
                "Dispositivos" => "Abrir Administrador de dispositivos",
                "Red" => "Abrir configuracion de red",
                _ when SelectedAlert.Message.Contains("driver", StringComparison.OrdinalIgnoreCase) => "Abrir Windows Update",
                _ => "Abrir Windows Update"
            };
        }
    }

    public DiagnosticStatus OverallStatus
    {
        get => _overallStatus;
        private set
        {
            if (SetProperty(ref _overallStatus, value))
            {
                OnPropertyChanged(nameof(OverallStatusText));
            }
        }
    }

    public string OverallStatusText => OverallStatus switch
    {
        DiagnosticStatus.Ok => "Correcto",
        DiagnosticStatus.Warning => "Atencion requerida",
        DiagnosticStatus.Critical => "Problemas criticos",
        _ => "Pendiente"
    };

    public bool IsScanning
    {
        get => _isScanning;
        private set
        {
            if (SetProperty(ref _isScanning, value))
            {
                StartDiagnosticCommand.RaiseCanExecuteChanged();
                ExportReportCommand.RaiseCanExecuteChanged();
                ToggleLiveMonitorCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsLiveMonitoring
    {
        get => _isLiveMonitoring;
        private set
        {
            if (SetProperty(ref _isLiveMonitoring, value))
            {
                LiveMonitorText = value ? "Detener monitor" : "Activar monitor";
            }
        }
    }

    public double Progress
    {
        get => _progress;
        private set => SetProperty(ref _progress, value);
    }

    public int HealthScore
    {
        get => _healthScore;
        private set => SetProperty(ref _healthScore, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string DetailsText
    {
        get => _detailsText;
        private set => SetProperty(ref _detailsText, value);
    }

    public string LastScanText
    {
        get => _lastScanText;
        private set => SetProperty(ref _lastScanText, value);
    }

    public string AlertSummaryText
    {
        get => _alertSummaryText;
        private set => SetProperty(ref _alertSummaryText, value);
    }

    public string HealthTrendText
    {
        get => _healthTrendText;
        private set => SetProperty(ref _healthTrendText, value);
    }

    public string LiveMonitorText
    {
        get => _liveMonitorText;
        private set => SetProperty(ref _liveMonitorText, value);
    }

    private async Task StartDiagnosticAsync()
    {
        IsScanning = true;
        Progress = 0;
        CurrentReport = null;
        OverallStatus = DiagnosticStatus.Unknown;
        HealthScore = 0;
        AlertSummaryText = "Analizando...";
        HealthTrendText = "Calculando comparacion...";
        Summaries.Clear();
        Alerts.Clear();
        FilteredAlerts.Clear();
        SelectedAlert = null;
        SelectedDetails.Clear();
        DetailsText = "Analizando hardware...";
        var startedAt = DateTime.Now;

        var report = new HardwareReport();
        var steps = new List<(string Name, Func<Task> Run)>
        {
            ("Sistema", async () => report.System = await _systemService.AnalyzeAsync()),
            ("CPU", async () => report.Cpu = await _cpuService.AnalyzeAsync()),
            ("RAM", async () => report.Memory = await _memoryService.AnalyzeAsync()),
            ("Discos", async () => report.Disks = await _diskService.AnalyzeAsync()),
            ("GPU", async () => report.Gpus = await _gpuService.AnalyzeAsync()),
            ("Bateria", async () => report.Battery = await _batteryService.AnalyzeAsync()),
            ("Red", async () => report.NetworkAdapters = await _networkService.AnalyzeAsync()),
            ("Dispositivos", async () => report.DeviceIssues = await _deviceService.AnalyzeAsync())
        };

        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            StatusMessage = $"Analizando {step.Name}...";
            try
            {
                await step.Run();
            }
            catch (Exception ex)
            {
                report.Alerts.Add(new Alert(DiagnosticStatus.Warning, "Diagnostico", $"No se pudo completar el analisis de {step.Name}: {ex.Message}"));
            }

            Progress = (i + 1) / (double)steps.Count * 100;
        }

        BuildReportSummaryAndRefresh(report);
        CurrentReport = report;
        DetailsText = _reportService.ToText(report);
        OverallStatus = report.OverallStatus;
        HealthScore = CalculateHealthScore(report);
        AddHistoryItem(report, HealthScore);
        LastScanText = $"Ultimo diagnostico: {report.GeneratedAt:g} ({DateTime.Now - startedAt:mm\\:ss})";
        AlertSummaryText = BuildAlertSummary(report);
        StatusMessage = report.OverallStatus switch
        {
            DiagnosticStatus.Ok => "Diagnostico completado sin problemas relevantes.",
            DiagnosticStatus.Warning => "Diagnostico completado con advertencias.",
            DiagnosticStatus.Critical => "Diagnostico completado con problemas criticos.",
            _ => "Diagnostico completado con informacion parcial."
        };

        IsScanning = false;
    }

    private async Task ExportReportAsync()
    {
        if (CurrentReport is null)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Exportar informe",
            Filter = "Informe de texto (*.txt)|*.txt|Informe JSON (*.json)|*.json|Informe HTML (*.html)|*.html",
            FileName = $"hardware-report-{DateTime.Now:yyyyMMdd-HHmm}.txt",
            AddExtension = true,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() == true)
        {
            await _reportService.ExportAsync(CurrentReport, dialog.FileName);
            StatusMessage = $"Informe exportado: {dialog.FileName}";
        }
    }

    private void CopyReportToClipboard()
    {
        if (CurrentReport is null)
        {
            return;
        }

        Clipboard.SetText(_reportService.ToText(CurrentReport));
        StatusMessage = "Informe copiado al portapapeles.";
    }

    private static void BuildReportSummary(HardwareReport report)
    {
        report.Summaries =
        [
            new CategorySummary("Sistema", report.System?.Status ?? DiagnosticStatus.Unknown, report.System?.Model ?? "Sin datos", report.System?.SecureBootStatus ?? "Secure Boot sin datos", "SYS", "BIOS, TPM, Secure Boot y modelo"),
            new CategorySummary("CPU", report.Cpu?.Status ?? DiagnosticStatus.Unknown, $"{report.Cpu?.UsagePercent:0}% uso", $"{report.Cpu?.Cores ?? 0} nucleos / {report.Cpu?.LogicalProcessors ?? 0} hilos", "CPU", "Carga, frecuencia y sensores termicos"),
            new CategorySummary("RAM", report.Memory?.Status ?? DiagnosticStatus.Unknown, $"{report.Memory?.UsagePercent:0}% uso", $"{report.Memory?.FreeGb:0.0} GB libres", "RAM", "Memoria disponible y presion del sistema"),
            new CategorySummary("Discos", StatusEvaluator.Worst(report.Disks.Select(d => d.Status).DefaultIfEmpty(DiagnosticStatus.Unknown)), $"{report.Disks.Count} unidades", $"{report.Disks.Sum(d => d.FreeGb):0.0} GB libres", "DSK", "Capacidad, tipo de unidad y SMART"),
            new CategorySummary("GPU", StatusEvaluator.Worst(report.Gpus.Select(g => g.Status).DefaultIfEmpty(DiagnosticStatus.Unknown)), $"{report.Gpus.Count} GPU", report.Gpus.FirstOrDefault()?.Name ?? "Sin datos", "GPU", "Controlador grafico y memoria dedicada"),
            new CategorySummary("Bateria", report.Battery?.Status ?? DiagnosticStatus.Unknown, report.Battery?.IsPresent == true ? $"{report.Battery.ChargePercent}%" : "No detectada", report.Battery?.PowerState ?? "Sin datos", "BAT", "Carga, conexion y salud si esta disponible"),
            new CategorySummary("Red", StatusEvaluator.Worst(report.NetworkAdapters.Select(n => n.DiagnosticStatus).DefaultIfEmpty(DiagnosticStatus.Unknown)), $"{report.NetworkAdapters.Count} activos", report.NetworkAdapters.FirstOrDefault()?.LocalIp ?? "Sin IP", "NET", "Interfaces activas, IP y velocidad"),
            new CategorySummary("Dispositivos", report.DeviceIssues.Count == 0 ? DiagnosticStatus.Ok : DiagnosticStatus.Warning, $"{report.DeviceIssues.Count} problemas", "Administrador de dispositivos", "DRV", "Errores y drivers posiblemente antiguos")
        ];

        var diagnosticAlerts = report.Alerts.Where(alert => alert.Category == "Diagnostico").Select(AddRecommendation).ToList();
        report.Alerts.Clear();
        report.Alerts.AddRange(diagnosticAlerts);
        report.Alerts.AddRange((report.System?.Alerts ?? []).Select(AddRecommendation));
        report.Alerts.AddRange((report.Cpu?.Alerts ?? []).Select(AddRecommendation));
        report.Alerts.AddRange((report.Memory?.Alerts ?? []).Select(AddRecommendation));
        report.Alerts.AddRange(report.Disks.SelectMany(d => d.Alerts).Select(AddRecommendation));
        report.Alerts.AddRange(report.Gpus.SelectMany(g => g.Alerts).Select(AddRecommendation));
        report.Alerts.AddRange((report.Battery?.Alerts ?? []).Select(AddRecommendation));
        report.Alerts.AddRange(report.NetworkAdapters.SelectMany(n => n.Alerts).Select(AddRecommendation));
        report.Alerts.AddRange(report.DeviceIssues.Select(issue => new Alert(
            issue.Status,
            "Dispositivos",
            $"{issue.Name}: {issue.Message}",
            BuildDeviceRecommendation(issue))));

        report.OverallStatus = StatusEvaluator.Worst(report.Summaries.Select(summary => summary.Status));
    }

    private void BuildReportSummaryAndRefresh(HardwareReport report)
    {
        var previousSelectedName = SelectedSummary?.Name;
        BuildReportSummary(report);
        Summaries.Clear();
        Alerts.Clear();

        foreach (var summary in report.Summaries)
        {
            Summaries.Add(summary);
        }

        foreach (var alert in report.Alerts)
        {
            Alerts.Add(alert);
        }

        ApplyAlertFilter();
        SelectedSummary = Summaries.FirstOrDefault(summary => summary.Name == previousSelectedName) ?? Summaries.FirstOrDefault();
    }

    private void LoadEmptySummaries()
    {
        var initialSummaries = new[]
        {
            new CategorySummary("Sistema", DiagnosticStatus.Unknown, "Sin analizar", "BIOS, TPM y Secure Boot", "SYS", "BIOS, TPM, Secure Boot y modelo"),
            new CategorySummary("CPU", DiagnosticStatus.Unknown, "Sin analizar", "Nucleos e hilos", "CPU", "Carga, frecuencia y sensores termicos"),
            new CategorySummary("RAM", DiagnosticStatus.Unknown, "Sin analizar", "Uso de memoria", "RAM", "Memoria disponible y presion del sistema"),
            new CategorySummary("Discos", DiagnosticStatus.Unknown, "Sin analizar", "Espacio y SMART", "DSK", "Capacidad, tipo de unidad y SMART"),
            new CategorySummary("GPU", DiagnosticStatus.Unknown, "Sin analizar", "Memoria y estado", "GPU", "Controlador grafico y memoria dedicada"),
            new CategorySummary("Bateria", DiagnosticStatus.Unknown, "Sin analizar", "Portatiles", "BAT", "Carga, conexion y salud si esta disponible"),
            new CategorySummary("Red", DiagnosticStatus.Unknown, "Sin analizar", "Adaptadores activos", "NET", "Interfaces activas, IP y velocidad"),
            new CategorySummary("Dispositivos", DiagnosticStatus.Unknown, "Sin analizar", "Errores de drivers", "DRV", "Errores y drivers posiblemente antiguos")
        };

        foreach (var summary in initialSummaries)
        {
            Summaries.Add(summary);
        }

        SelectedSummary = Summaries.FirstOrDefault();
        RefreshSelectedDetails();
    }

    private void RefreshSelectedDetails()
    {
        SelectedDetails.Clear();
        if (SelectedSummary is null)
        {
            return;
        }

        if (CurrentReport is null)
        {
            SelectedDetails.Add(new DetailItem("Estado", "Pendiente", DiagnosticStatus.Unknown, "Ejecuta el diagnostico para obtener datos reales."));
            SelectedDetails.Add(new DetailItem("Categoria", SelectedSummary.Description, DiagnosticStatus.Unknown));
            return;
        }

        foreach (var item in BuildDetailsFor(SelectedSummary.Name, CurrentReport))
        {
            SelectedDetails.Add(item);
        }
    }

    private static IEnumerable<DetailItem> BuildDetailsFor(string category, HardwareReport report)
    {
        return category switch
        {
            "CPU" => BuildCpuDetails(report),
            "Sistema" => BuildSystemDetails(report),
            "RAM" => BuildMemoryDetails(report),
            "Discos" => BuildDiskDetails(report),
            "GPU" => BuildGpuDetails(report),
            "Bateria" => BuildBatteryDetails(report),
            "Red" => BuildNetworkDetails(report),
            "Dispositivos" => BuildDeviceDetails(report),
            _ => []
        };
    }

    private static IEnumerable<DetailItem> BuildSystemDetails(HardwareReport report)
    {
        var system = report.System;
        if (system is null)
        {
            return [new DetailItem("Sistema", "Sin datos", DiagnosticStatus.Unknown)];
        }

        return
        [
            new DetailItem("Fabricante", system.Manufacturer, system.Status),
            new DetailItem("Modelo", system.Model, system.Status),
            new DetailItem("BIOS/UEFI", system.BiosVersion, system.BiosDate.HasValue ? system.Status : DiagnosticStatus.Unknown, system.BiosDate.HasValue ? $"Fecha: {system.BiosDate:yyyy-MM-dd}" : "Fecha no disponible"),
            new DetailItem("TPM", system.TpmStatus, system.TpmStatus == "Activado" ? DiagnosticStatus.Ok : DiagnosticStatus.Warning),
            new DetailItem("Secure Boot", system.SecureBootStatus, system.SecureBootStatus == "Activado" ? DiagnosticStatus.Ok : DiagnosticStatus.Warning)
        ];
    }

    private static IEnumerable<DetailItem> BuildCpuDetails(HardwareReport report)
    {
        var cpu = report.Cpu;
        if (cpu is null)
        {
            return [new DetailItem("CPU", "Sin datos", DiagnosticStatus.Unknown)];
        }

        return
        [
            new DetailItem("Modelo", cpu.Name, cpu.Status),
            new DetailItem("Uso actual", $"{cpu.UsagePercent:0}%", cpu.Status),
            new DetailItem("Temperatura", FormatHelper.OptionalTemperature(cpu.TemperatureCelsius), cpu.TemperatureCelsius.HasValue ? DiagnosticStatus.Ok : DiagnosticStatus.Unknown),
            new DetailItem("Nucleos fisicos", cpu.Cores.ToString(), DiagnosticStatus.Ok),
            new DetailItem("Hilos logicos", cpu.LogicalProcessors.ToString(), DiagnosticStatus.Ok),
            new DetailItem("Frecuencia maxima", FormatHelper.OptionalMhz(cpu.FrequencyMhz), cpu.FrequencyMhz.HasValue ? DiagnosticStatus.Ok : DiagnosticStatus.Unknown),
            new DetailItem("Notas", string.IsNullOrWhiteSpace(cpu.Notes) ? "Sin observaciones" : cpu.Notes, DiagnosticStatus.Unknown)
        ];
    }

    private static IEnumerable<DetailItem> BuildMemoryDetails(HardwareReport report)
    {
        var memory = report.Memory;
        if (memory is null)
        {
            return [new DetailItem("RAM", "Sin datos", DiagnosticStatus.Unknown)];
        }

        return
        [
            new DetailItem("Memoria total", FormatHelper.Gb(memory.TotalGb), memory.Status),
            new DetailItem("Memoria usada", FormatHelper.Gb(memory.UsedGb), memory.Status),
            new DetailItem("Memoria libre", FormatHelper.Gb(memory.FreeGb), memory.Status),
            new DetailItem("Porcentaje de uso", $"{memory.UsagePercent:0}%", memory.Status)
        ];
    }

    private static IEnumerable<DetailItem> BuildDiskDetails(HardwareReport report)
    {
        if (report.Disks.Count == 0)
        {
            return [new DetailItem("Discos", "Sin unidades listas", DiagnosticStatus.Warning)];
        }

        return report.Disks.Select(disk => new DetailItem(
            $"{disk.Name} {disk.Label}".Trim(),
            $"{FormatHelper.Gb(disk.FreeGb)} libres de {FormatHelper.Gb(disk.TotalGb)} | {disk.DriveType} | SMART: {disk.SmartStatus}",
            disk.Status,
            $"{disk.UsagePercent:0}% usado, {disk.FileSystem}"));
    }

    private static IEnumerable<DetailItem> BuildGpuDetails(HardwareReport report)
    {
        return report.Gpus.Count == 0
            ? [new DetailItem("GPU", "Sin datos", DiagnosticStatus.Unknown)]
            : report.Gpus.Select(gpu => new DetailItem(
                gpu.Name,
                $"Memoria: {gpu.DedicatedMemoryGb?.ToString("0.0 GB") ?? "No disponible"}",
                gpu.Status,
                string.IsNullOrWhiteSpace(gpu.Notes) ? "Sin observaciones" : gpu.Notes));
    }

    private static IEnumerable<DetailItem> BuildBatteryDetails(HardwareReport report)
    {
        var battery = report.Battery;
        if (battery is null)
        {
            return [new DetailItem("Bateria", "Sin datos", DiagnosticStatus.Unknown)];
        }

        return
        [
            new DetailItem("Detectada", battery.IsPresent ? "Si" : "No", battery.Status),
            new DetailItem("Carga", battery.ChargePercent.HasValue ? $"{battery.ChargePercent}%" : "No disponible", battery.Status),
            new DetailItem("Estado", battery.PowerState, battery.Status),
            new DetailItem("Salud", battery.Health, battery.Health == "No disponible" ? DiagnosticStatus.Unknown : battery.Status)
        ];
    }

    private static IEnumerable<DetailItem> BuildNetworkDetails(HardwareReport report)
    {
        return report.NetworkAdapters.Select(adapter => new DetailItem(
            adapter.Name,
            $"{adapter.LocalIp} | {adapter.SpeedMbps?.ToString("0 Mbps") ?? "velocidad no disponible"}",
            adapter.DiagnosticStatus,
            adapter.Description));
    }

    private static IEnumerable<DetailItem> BuildDeviceDetails(HardwareReport report)
    {
        return report.DeviceIssues.Count == 0
            ? [new DetailItem("Administrador de dispositivos", "Sin errores detectados", DiagnosticStatus.Ok)]
            : report.DeviceIssues.Select(issue => new DetailItem(
                issue.Name,
                issue.Message,
                issue.Status,
                $"Driver {issue.DriverVersion}{(issue.DriverDate.HasValue ? $" | {issue.DriverDate:yyyy-MM-dd}" : string.Empty)}"));
    }

    private static int CalculateHealthScore(HardwareReport report)
    {
        var penalty = report.Summaries.Sum(summary => summary.Status switch
        {
            DiagnosticStatus.Critical => 24,
            DiagnosticStatus.Warning => 11,
            DiagnosticStatus.Unknown => 4,
            _ => 0
        });

        penalty += Math.Min(report.Alerts.Count, 10) * 2;
        return Math.Clamp(100 - penalty, 0, 100);
    }

    private static string BuildAlertSummary(HardwareReport report)
    {
        var critical = report.Alerts.Count(alert => alert.Status == DiagnosticStatus.Critical);
        var warnings = report.Alerts.Count(alert => alert.Status == DiagnosticStatus.Warning);

        return critical == 0 && warnings == 0
            ? "Sin alertas activas"
            : $"{critical} criticas, {warnings} advertencias";
    }

    private void ApplyAlertFilter()
    {
        FilteredAlerts.Clear();
        var filtered = SelectedAlertFilter switch
        {
            "Criticas" => Alerts.Where(alert => alert.Status == DiagnosticStatus.Critical),
            "Advertencias" => Alerts.Where(alert => alert.Status == DiagnosticStatus.Warning),
            "Todas" => Alerts,
            _ => Alerts.Where(alert => alert.Category.Equals(SelectedAlertFilter, StringComparison.OrdinalIgnoreCase))
        };

        foreach (var alert in filtered)
        {
            FilteredAlerts.Add(alert);
        }

        SelectedAlert = SelectedAlert is not null && FilteredAlerts.Contains(SelectedAlert)
            ? SelectedAlert
            : FilteredAlerts.FirstOrDefault();
    }

    private void AddHistoryItem(HardwareReport report, int score)
    {
        var previous = DiagnosticHistory.FirstOrDefault();
        var scoreDelta = previous is null ? 0 : score - previous.HealthScore;
        var delta = previous is null ? "Primera ejecucion" : scoreDelta switch
        {
            > 0 => $"+{scoreDelta} puntos",
            < 0 => $"{scoreDelta} puntos",
            _ => "Sin cambios"
        };

        HealthTrendText = previous is null
            ? "Primera medicion registrada"
            : $"Comparado con el diagnostico anterior: {delta}";

        DiagnosticHistory.Insert(0, new DiagnosticHistoryItem(
            report.GeneratedAt,
            score,
            report.OverallStatus,
            report.Alerts.Count,
            delta));

        while (DiagnosticHistory.Count > 10)
        {
            DiagnosticHistory.RemoveAt(DiagnosticHistory.Count - 1);
        }
    }

    private void ToggleLiveMonitor()
    {
        if (IsLiveMonitoring)
        {
            _liveTimer.Stop();
            IsLiveMonitoring = false;
            return;
        }

        _liveTimer.Start();
        IsLiveMonitoring = true;
    }

    private async Task RefreshLiveMetricsAsync()
    {
        if (CurrentReport is null || IsScanning || _isLiveRefreshRunning)
        {
            return;
        }

        try
        {
            _isLiveRefreshRunning = true;
            CurrentReport.Cpu = await _cpuService.AnalyzeAsync();
            CurrentReport.Memory = await _memoryService.AnalyzeAsync();
            BuildReportSummaryAndRefresh(CurrentReport);
            DetailsText = _reportService.ToText(CurrentReport);
            OverallStatus = CurrentReport.OverallStatus;
            HealthScore = CalculateHealthScore(CurrentReport);
            AlertSummaryText = BuildAlertSummary(CurrentReport);
            StatusMessage = $"Monitor en vivo actualizado: {DateTime.Now:T}";
        }
        finally
        {
            _isLiveRefreshRunning = false;
        }
    }

    private void OpenHelpForSelectedAlert()
    {
        if (SelectedAlert is null)
        {
            return;
        }

        var target = SelectedAlert.Category switch
        {
            "Dispositivos" => "devmgmt.msc",
            "Red" => "ms-settings:network",
            "Sistema" => "ms-settings:windowsupdate",
            "Disco" => "ms-settings:storagesense",
            _ => "ms-settings:windowsupdate"
        };

        Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
    }

    private static Alert AddRecommendation(Alert alert)
    {
        if (!string.IsNullOrWhiteSpace(alert.Recommendation))
        {
            return alert;
        }

        var recommendation = alert.Category switch
        {
            "CPU" => "Cierra procesos de alto consumo, revisa ventilacion y limpia polvo si el uso alto es constante.",
            "RAM" => "Cierra aplicaciones pesadas, desactiva programas de inicio o considera ampliar memoria si ocurre con frecuencia.",
            "Disco" => "Libera espacio, vacia temporales y revisa la unidad con herramientas del fabricante si SMART indica fallo.",
            "GPU" => "Actualiza el driver grafico desde Windows Update o desde NVIDIA, AMD o Intel segun corresponda.",
            "Bateria" => "Conecta el cargador y revisa el informe de bateria de Windows si la salud aparece degradada.",
            "Red" => "Comprueba cable/Wi-Fi, renueva IP o revisa el adaptador desde Configuracion de red.",
            "Sistema" => "Revisa Windows Update, la pagina del fabricante y la configuracion UEFI/BIOS.",
            _ => "Revisa la configuracion de Windows y vuelve a ejecutar el diagnostico tras aplicar cambios."
        };

        return alert with { Recommendation = recommendation };
    }

    private static string BuildDeviceRecommendation(DeviceIssueResult issue)
    {
        if (issue.Message.Contains("antiguo", StringComparison.OrdinalIgnoreCase))
        {
            return "Abre Windows Update y revisa actualizaciones opcionales de controladores; si no aparece nada, descarga el driver desde la web del fabricante.";
        }

        return issue.ErrorCode switch
        {
            10 => "Abre el Administrador de dispositivos, desinstala o actualiza el driver y reinicia el equipo.",
            22 => "Abre el Administrador de dispositivos y habilita el dispositivo si lo necesitas.",
            28 => "Instala el controlador desde Windows Update o desde la pagina oficial del fabricante.",
            31 or 43 => "Actualiza o reinstala el controlador. Si persiste, prueba otro puerto o revisa el hardware fisico.",
            _ => "Abre el Administrador de dispositivos para ver propiedades, eventos y opciones de actualizacion del driver."
        };
    }
}
