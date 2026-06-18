namespace HardwareChecker.Models;

public sealed class HardwareReport
{
    public DateTime GeneratedAt { get; init; } = DateTime.Now;
    public string MachineName { get; init; } = Environment.MachineName;
    public string UserName { get; init; } = Environment.UserName;
    public string OperatingSystem { get; init; } = Environment.OSVersion.VersionString;
    public DiagnosticStatus OverallStatus { get; set; } = DiagnosticStatus.Unknown;
    public SystemResult? System { get; set; }
    public CpuResult? Cpu { get; set; }
    public MemoryResult? Memory { get; set; }
    public List<DiskResult> Disks { get; set; } = [];
    public List<GpuResult> Gpus { get; set; } = [];
    public BatteryResult? Battery { get; set; }
    public List<NetworkAdapterResult> NetworkAdapters { get; set; } = [];
    public List<DeviceIssueResult> DeviceIssues { get; set; } = [];
    public List<CategorySummary> Summaries { get; set; } = [];
    public List<Alert> Alerts { get; set; } = [];
}
