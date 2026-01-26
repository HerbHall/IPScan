using IPScan.Core.Models;

namespace IPScan.Core.Services;

/// <summary>
/// Service for detecting and managing network interfaces.
/// </summary>
public interface INetworkInterfaceService
{
    /// <summary>
    /// Gets all available network interfaces.
    /// </summary>
    IReadOnlyList<NetworkInterfaceInfo> GetAllInterfaces();

    /// <summary>
    /// Gets only interfaces that are up and have an IPv4 address.
    /// </summary>
    IReadOnlyList<NetworkInterfaceInfo> GetActiveInterfaces();

    /// <summary>
    /// Gets the default network interface to use for scanning.
    /// Priority: First Ethernet interface, then first wireless, then first available.
    /// </summary>
    NetworkInterfaceInfo? GetDefaultInterface();

    /// <summary>
    /// Gets a specific interface by its ID.
    /// </summary>
    NetworkInterfaceInfo? GetInterface(string interfaceId);

    /// <summary>
    /// Gets the preferred interface based on settings, or the default if not found.
    /// </summary>
    NetworkInterfaceInfo? GetPreferredInterface(string? preferredInterfaceId);
}
