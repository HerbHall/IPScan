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
            .Where(i => i.IsUp
                && !string.IsNullOrEmpty(i.IpAddress)
                && i.InterfaceType != Models.NetworkInterfaceType.Loopback
                && !i.IsVpn)  // Exclude VPN interfaces by default per DECISIONS.md
            .ToList();
    }

    /// <inheritdoc />
    public NetworkInterfaceInfo? GetDefaultInterface()
    {
        var activeInterfaces = GetActiveInterfaces();

        // CRITICAL: Prioritize interfaces with a gateway (default route)
        // This identifies the primary routing interface for the network
        var interfacesWithGateway = activeInterfaces
            .Where(i => !string.IsNullOrEmpty(i.Gateway))
            .ToList();

        // Priority 1: Ethernet interface with gateway (wired connection is most stable)
        var ethernet = interfacesWithGateway
            .FirstOrDefault(i => i.InterfaceType == Models.NetworkInterfaceType.Ethernet);
        if (ethernet != null)
        {
            _logger.LogInformation("Selected default interface: {Name} (Ethernet with gateway {Gateway})",
                ethernet.Name, ethernet.Gateway);
            return ethernet;
        }

        // Priority 2: Wireless interface with gateway
        var wireless = interfacesWithGateway
            .FirstOrDefault(i => i.InterfaceType == Models.NetworkInterfaceType.Wireless);
        if (wireless != null)
        {
            _logger.LogInformation("Selected default interface: {Name} (Wireless with gateway {Gateway})",
                wireless.Name, wireless.Gateway);
            return wireless;
        }

        // Priority 3: Any interface with gateway
        var anyWithGateway = interfacesWithGateway.FirstOrDefault();
        if (anyWithGateway != null)
        {
            _logger.LogInformation("Selected default interface: {Name} ({Type} with gateway {Gateway})",
                anyWithGateway.Name, anyWithGateway.InterfaceType, anyWithGateway.Gateway);
            return anyWithGateway;
        }

        // Fallback: Try Ethernet without gateway (unusual but possible)
        var ethernetNoGateway = activeInterfaces
            .FirstOrDefault(i => i.InterfaceType == Models.NetworkInterfaceType.Ethernet);
        if (ethernetNoGateway != null)
        {
            _logger.LogWarning("Selected default interface: {Name} (Ethernet but no gateway configured)",
                ethernetNoGateway.Name);
            return ethernetNoGateway;
        }

        // Last resort: Any active interface
        var any = activeInterfaces.FirstOrDefault();
        if (any != null)
        {
            _logger.LogWarning("Selected default interface: {Name} ({Type} - no gateway found)",
                any.Name, any.InterfaceType);
            return any;
        }

        _logger.LogError("No suitable network interface found");
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
            // Defensive null check - UnicastAddresses and GatewayAddresses may be null
            var ipv4Address = properties.UnicastAddresses?
                .FirstOrDefault(a => a?.Address?.AddressFamily == AddressFamily.InterNetwork);

            var gateway = properties.GatewayAddresses?
                .FirstOrDefault(g => g?.Address?.AddressFamily == AddressFamily.InterNetwork);

            var isVpn = IsVpnInterface(nic);
            var interfaceType = isVpn ? Models.NetworkInterfaceType.Vpn : ConvertInterfaceType(nic.NetworkInterfaceType);

            return new NetworkInterfaceInfo
            {
                Id = nic.Id,
                Name = nic.Name,
                Description = nic.Description,
                InterfaceType = interfaceType,
                IpAddress = ipv4Address?.Address?.ToString() ?? string.Empty,
                SubnetMask = ipv4Address?.IPv4Mask?.ToString() ?? string.Empty,
                Gateway = gateway?.Address?.ToString(),
                MacAddress = FormatMacAddress(nic.GetPhysicalAddress()),
                IsUp = nic.OperationalStatus == OperationalStatus.Up,
                Speed = nic.Speed,
                IsVpn = isVpn
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

    /// <summary>
    /// Detects if an interface is a VPN based on name and description patterns.
    /// Checks for common VPN software: Tailscale, Wireguard, OpenVPN, PPTP, L2TP, etc.
    /// </summary>
    private static bool IsVpnInterface(NetInterface nic)
    {
        var name = nic.Name.ToLowerInvariant();
        var description = nic.Description.ToLowerInvariant();

        // VPN interface patterns (name or description)
        string[] vpnPatterns =
        {
            "tailscale",       // Tailscale VPN
            "wireguard",       // Wireguard VPN
            "wg",              // Wireguard short name
            "openvpn",         // OpenVPN
            "tap-windows",     // OpenVPN TAP adapter
            "tap0901",         // OpenVPN TAP adapter version
            "tun",             // Generic tunnel interface
            "vpn",             // Generic VPN
            "pptp",            // Point-to-Point Tunneling Protocol
            "l2tp",            // Layer 2 Tunneling Protocol
            "ipsec",           // IPSec VPN
            "cisco anyconnect",// Cisco AnyConnect
            "sonicwall",       // SonicWall VPN
            "fortinet",        // FortiClient VPN
            "palo alto",       // Palo Alto GlobalProtect
            "globalprotect",   // Palo Alto GlobalProtect
            "checkpoint",      // Check Point VPN
            "nordvpn",         // NordVPN
            "expressvpn",      // ExpressVPN
            "protonvpn",       // ProtonVPN
            "private internet access", // PIA VPN
            "hamachi",         // LogMeIn Hamachi
            "zerotier",        // ZeroTier
            "virtual private", // Generic "Virtual Private Network"
        };

        foreach (var pattern in vpnPatterns)
        {
            if (name.Contains(pattern) || description.Contains(pattern))
            {
                return true;
            }
        }

        // Additional check: if it's a tunnel interface type, likely a VPN
        if (nic.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Tunnel)
        {
            return true;
        }

        return false;
    }
}
