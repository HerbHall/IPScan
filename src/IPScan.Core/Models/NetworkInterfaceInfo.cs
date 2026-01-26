namespace IPScan.Core.Models;

/// <summary>
/// Represents information about a network interface.
/// </summary>
public class NetworkInterfaceInfo
{
    /// <summary>
    /// Unique identifier for the network interface.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the network interface.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the network interface.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Type of the network interface (Ethernet, Wireless, etc.).
    /// </summary>
    public NetworkInterfaceType InterfaceType { get; set; }

    /// <summary>
    /// IPv4 address assigned to this interface.
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Subnet mask for the interface.
    /// </summary>
    public string SubnetMask { get; set; } = string.Empty;

    /// <summary>
    /// Default gateway for the interface.
    /// </summary>
    public string? Gateway { get; set; }

    /// <summary>
    /// MAC address of the interface.
    /// </summary>
    public string MacAddress { get; set; } = string.Empty;

    /// <summary>
    /// Whether the interface is currently up and operational.
    /// </summary>
    public bool IsUp { get; set; }

    /// <summary>
    /// Speed of the interface in bits per second.
    /// </summary>
    public long Speed { get; set; }
}

/// <summary>
/// Type of network interface.
/// </summary>
public enum NetworkInterfaceType
{
    Unknown,
    Ethernet,
    Wireless,
    Loopback,
    Virtual,
    Other
}
