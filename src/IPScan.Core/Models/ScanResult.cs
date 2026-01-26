namespace IPScan.Core.Models;

/// <summary>
/// Represents the result of a network scan operation.
/// </summary>
public class ScanResult
{
    /// <summary>
    /// Whether the scan completed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if the scan failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The subnet that was scanned (CIDR notation).
    /// </summary>
    public string Subnet { get; set; } = string.Empty;

    /// <summary>
    /// The network interface used for scanning.
    /// </summary>
    public string InterfaceId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the scan started.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Timestamp when the scan completed.
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Duration of the scan.
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>
    /// Total number of IP addresses scanned.
    /// </summary>
    public int TotalAddressesScanned { get; set; }

    /// <summary>
    /// Devices that were discovered during this scan.
    /// </summary>
    public List<DiscoveredDevice> DiscoveredDevices { get; set; } = [];

    /// <summary>
    /// Number of devices that responded.
    /// </summary>
    public int DevicesFound => DiscoveredDevices.Count;
}

/// <summary>
/// Represents a device discovered during a scan (before being added to persistent storage).
/// </summary>
public class DiscoveredDevice
{
    /// <summary>
    /// IPv4 address of the device.
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Hostname if resolved via DNS.
    /// </summary>
    public string? Hostname { get; set; }

    /// <summary>
    /// MAC address if discovered.
    /// </summary>
    public string? MacAddress { get; set; }

    /// <summary>
    /// Response time in milliseconds.
    /// </summary>
    public long ResponseTimeMs { get; set; }
}
