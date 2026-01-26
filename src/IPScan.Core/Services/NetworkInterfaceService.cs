using System.Net.NetworkInformation;
using System.Net.Sockets;
using IPScan.Core.Models;
using Microsoft.Extensions.Logging;
using NetInterface = System.Net.NetworkInformation.NetworkInterface;

namespace IPScan.Core.Services;

/// <summary>
/// Service for detecting and managing network interfaces.
/// </summary>
public class NetworkInterfaceService : INetworkInterfaceService
{
    private readonly ILogger<NetworkInterfaceService> _logger;

    public NetworkInterfaceService(ILogger<NetworkInterfaceService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<NetworkInterfaceInfo> GetAllInterfaces()
    {
        var interfaces = new List<NetworkInterfaceInfo>();

        try
        {
            foreach (var nic in NetInterface.GetAllNetworkInterfaces())
            {
                var info = ConvertToNetworkInterfaceInfo(nic);
                if (info != null)
                {
                    interfaces.Add(info);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate network interfaces");
        }

        return interfaces;
    }

    /// <inheritdoc />
    public IReadOnlyList<NetworkInterfaceInfo> GetActiveInterfaces()
    {
        return GetAllInterfaces()
            .Where(i => i.IsUp && !string.IsNullOrEmpty(i.IpAddress) && i.InterfaceType != Models.NetworkInterfaceType.Loopback)
            .ToList();
    }

    /// <inheritdoc />
    public NetworkInterfaceInfo? GetDefaultInterface()
    {
        var activeInterfaces = GetActiveInterfaces();

        // Priority 1: First Ethernet interface
        var ethernet = activeInterfaces
            .FirstOrDefault(i => i.InterfaceType == Models.NetworkInterfaceType.Ethernet);
        if (ethernet != null)
        {
            _logger.LogDebug("Selected default interface: {Name} (Ethernet)", ethernet.Name);
            return ethernet;
        }

        // Priority 2: First Wireless interface
        var wireless = activeInterfaces
            .FirstOrDefault(i => i.InterfaceType == Models.NetworkInterfaceType.Wireless);
        if (wireless != null)
        {
            _logger.LogDebug("Selected default interface: {Name} (Wireless)", wireless.Name);
            return wireless;
        }

        // Priority 3: Any active interface
        var any = activeInterfaces.FirstOrDefault();
        if (any != null)
        {
            _logger.LogDebug("Selected default interface: {Name} ({Type})", any.Name, any.InterfaceType);
            return any;
        }

        _logger.LogWarning("No suitable network interface found");
        return null;
    }

    /// <inheritdoc />
    public NetworkInterfaceInfo? GetInterface(string interfaceId)
    {
        if (string.IsNullOrWhiteSpace(interfaceId))
            return null;

        return GetAllInterfaces().FirstOrDefault(i => i.Id == interfaceId);
    }

    /// <inheritdoc />
    public NetworkInterfaceInfo? GetPreferredInterface(string? preferredInterfaceId)
    {
        if (!string.IsNullOrWhiteSpace(preferredInterfaceId))
        {
            var preferred = GetInterface(preferredInterfaceId);
            if (preferred != null && preferred.IsUp)
            {
                _logger.LogDebug("Using preferred interface: {Name}", preferred.Name);
                return preferred;
            }

            _logger.LogWarning("Preferred interface {Id} not found or not active, falling back to default",
                preferredInterfaceId);
        }

        return GetDefaultInterface();
    }

    private NetworkInterfaceInfo? ConvertToNetworkInterfaceInfo(NetInterface nic)
    {
        try
        {
            var properties = nic.GetIPProperties();
            var ipv4Address = properties.UnicastAddresses
                .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);

            var gateway = properties.GatewayAddresses
                .FirstOrDefault(g => g.Address.AddressFamily == AddressFamily.InterNetwork);

            return new NetworkInterfaceInfo
            {
                Id = nic.Id,
                Name = nic.Name,
                Description = nic.Description,
                InterfaceType = ConvertInterfaceType(nic.NetworkInterfaceType),
                IpAddress = ipv4Address?.Address.ToString() ?? string.Empty,
                SubnetMask = ipv4Address?.IPv4Mask?.ToString() ?? string.Empty,
                Gateway = gateway?.Address.ToString(),
                MacAddress = FormatMacAddress(nic.GetPhysicalAddress()),
                IsUp = nic.OperationalStatus == OperationalStatus.Up,
                Speed = nic.Speed
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get properties for interface {Name}", nic.Name);
            return null;
        }
    }

    private static Models.NetworkInterfaceType ConvertInterfaceType(System.Net.NetworkInformation.NetworkInterfaceType type)
    {
        return type switch
        {
            System.Net.NetworkInformation.NetworkInterfaceType.Ethernet => Models.NetworkInterfaceType.Ethernet,
            System.Net.NetworkInformation.NetworkInterfaceType.GigabitEthernet => Models.NetworkInterfaceType.Ethernet,
            System.Net.NetworkInformation.NetworkInterfaceType.FastEthernetT => Models.NetworkInterfaceType.Ethernet,
            System.Net.NetworkInformation.NetworkInterfaceType.FastEthernetFx => Models.NetworkInterfaceType.Ethernet,
            System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211 => Models.NetworkInterfaceType.Wireless,
            System.Net.NetworkInformation.NetworkInterfaceType.Loopback => Models.NetworkInterfaceType.Loopback,
            System.Net.NetworkInformation.NetworkInterfaceType.Tunnel => Models.NetworkInterfaceType.Virtual,
            _ => Models.NetworkInterfaceType.Other
        };
    }

    private static string FormatMacAddress(PhysicalAddress address)
    {
        var bytes = address.GetAddressBytes();
        if (bytes.Length == 0)
            return string.Empty;

        return string.Join(":", bytes.Select(b => b.ToString("X2")));
    }
}
