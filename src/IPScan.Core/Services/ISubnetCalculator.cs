using System.Net;

namespace IPScan.Core.Services;

/// <summary>
/// Service for subnet calculations.
/// </summary>
public interface ISubnetCalculator
{
    /// <summary>
    /// Calculates the network address from an IP and subnet mask.
    /// </summary>
    IPAddress GetNetworkAddress(IPAddress ipAddress, IPAddress subnetMask);

    /// <summary>
    /// Calculates the broadcast address from an IP and subnet mask.
    /// </summary>
    IPAddress GetBroadcastAddress(IPAddress ipAddress, IPAddress subnetMask);

    /// <summary>
    /// Gets all usable host addresses in a subnet (excludes network and broadcast).
    /// </summary>
    IEnumerable<IPAddress> GetHostAddresses(IPAddress ipAddress, IPAddress subnetMask);

    /// <summary>
    /// Gets the number of usable host addresses in a subnet.
    /// </summary>
    int GetHostCount(IPAddress subnetMask);

    /// <summary>
    /// Converts a subnet mask to CIDR prefix length (e.g., 255.255.255.0 -> 24).
    /// </summary>
    int GetCidrPrefixLength(IPAddress subnetMask);

    /// <summary>
    /// Converts a CIDR prefix length to a subnet mask (e.g., 24 -> 255.255.255.0).
    /// </summary>
    IPAddress GetSubnetMaskFromCidr(int prefixLength);

    /// <summary>
    /// Parses a CIDR notation string (e.g., "192.168.1.0/24") and returns the IP and prefix length.
    /// </summary>
    (IPAddress Network, int PrefixLength)? ParseCidr(string cidr);

    /// <summary>
    /// Gets the subnet in CIDR notation from an IP and mask.
    /// </summary>
    string GetCidrNotation(IPAddress ipAddress, IPAddress subnetMask);
}
