namespace IPScan.Core.Models;

/// <summary>
/// Represents a discovered network device.
/// </summary>
public class Device
{
    /// <summary>
    /// Unique identifier for the device.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Display name for the device (hostname or user-assigned).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// DNS or mDNS hostname if discovered.
    /// </summary>
    public string? Hostname { get; set; }

    /// <summary>
    /// IPv4 address of the device.
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// MAC address of the device (if discovered).
    /// </summary>
    public string? MacAddress { get; set; }

    /// <summary>
    /// Whether the device responded in the last scan.
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Timestamp when the device was first discovered.
    /// </summary>
    public DateTime FirstDiscovered { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the device was last seen online.
    /// </summary>
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Number of consecutive scans where the device was not found.
    /// Reset to 0 when device is seen again.
    /// </summary>
    public int ConsecutiveMissedScans { get; set; }

    /// <summary>
    /// User-provided notes about the device.
    /// </summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Creates a display-friendly name from the hostname or IP address.
    /// </summary>
    public string DisplayName => !string.IsNullOrWhiteSpace(Name)
        ? Name
        : !string.IsNullOrWhiteSpace(Hostname)
            ? Hostname
            : IpAddress;
}
