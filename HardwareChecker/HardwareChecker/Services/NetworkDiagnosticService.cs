using System.Net.NetworkInformation;
using System.Net.Sockets;
using HardwareChecker.Models;

namespace HardwareChecker.Services;

public sealed class NetworkDiagnosticService
{
    public Task<List<NetworkAdapterResult>> AnalyzeAsync() => Task.Run(() =>
    {
        var adapters = NetworkInterface.GetAllNetworkInterfaces()
            .Where(adapter => adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .Where(adapter => adapter.OperationalStatus == OperationalStatus.Up)
            .Select(adapter =>
            {
                var ip = adapter.GetIPProperties().UnicastAddresses
                    .FirstOrDefault(address => address.Address.AddressFamily == AddressFamily.InterNetwork)
                    ?.Address.ToString() ?? "No disponible";
                var speed = adapter.Speed > 0 ? adapter.Speed / 1_000_000d : (double?)null;
                var alerts = new List<Alert>();
                var status = DiagnosticStatus.Ok;

                if (ip == "No disponible")
                {
                    status = DiagnosticStatus.Warning;
                    alerts.Add(new Alert(status, "Red", $"{adapter.Name} esta activo pero no tiene IPv4 local."));
                }

                return new NetworkAdapterResult
                {
                    Name = adapter.Name,
                    Description = adapter.Description,
                    Status = adapter.OperationalStatus.ToString(),
                    LocalIp = ip,
                    SpeedMbps = speed,
                    DiagnosticStatus = status,
                    Alerts = alerts
                };
            })
            .ToList();

        if (adapters.Count == 0)
        {
            adapters.Add(new NetworkAdapterResult
            {
                Name = "Sin adaptadores activos",
                Status = "Desconectado",
                DiagnosticStatus = DiagnosticStatus.Warning,
                Alerts = [new Alert(DiagnosticStatus.Warning, "Red", "No se detectaron adaptadores de red activos.")]
            });
        }

        return adapters;
    });
}
