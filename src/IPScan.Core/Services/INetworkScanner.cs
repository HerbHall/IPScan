using System.Net;
using IPScan.Core.Models;

namespace IPScan.Core.Services;

/// <summary>
/// Service for scanning networks to discover devices.
/// </summary>
public interface INetworkScanner
{
    /// <summary>
    /// Event raised when a device is discovered during scanning.
    /// </summary>
    event EventHandler<DiscoveredDevice>? DeviceDiscovered;

    /// <summary>
    /// Event raised to report scan progress.
    /// </summary>
    event EventHandler<ScanProgressEventArgs>? ProgressChanged;

    /// <summary>
    /// Scans the specified subnet for active devices.
    /// </summary>
    /// <param name="networkInterface">The network interface to use for scanning.</param>
    /// <param name="subnetMask">The subnet mask (uses interface's mask if null).</param>
    /// <param name="timeoutMs">Timeout for each ping in milliseconds.</param>
    /// <param name="maxConcurrent">Maximum number of concurrent ping operations.</param>
    /// <param name="cancellationToken">Cancellation token to stop the scan.</param>
    Task<ScanResult> ScanAsync(
        NetworkInterfaceInfo networkInterface,
        IPAddress? subnetMask = null,
        int timeoutMs = 1000,
        int maxConcurrent = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Scans a specific CIDR range for active devices.
    /// </summary>
    /// <param name="cidr">CIDR notation (e.g., "192.168.1.0/24").</param>
    /// <param name="timeoutMs">Timeout for each ping in milliseconds.</param>
    /// <param name="maxConcurrent">Maximum number of concurrent ping operations.</param>
    /// <param name="cancellationToken">Cancellation token to stop the scan.</param>
    Task<ScanResult> ScanCidrAsync(
        string cidr,
        int timeoutMs = 1000,
        int maxConcurrent = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pings a single IP address to check if it's online.
    /// </summary>
    Task<DiscoveredDevice?> PingAsync(IPAddress address, int timeoutMs = 1000, CancellationToken cancellationToken = default);
}

/// <summary>
/// Event arguments for scan progress updates.
/// </summary>
public class ScanProgressEventArgs : EventArgs
{
    /// <summary>
    /// Current address being scanned.
    /// </summary>
    public IPAddress? CurrentAddress { get; init; }

    /// <summary>
    /// Number of addresses scanned so far.
    /// </summary>
    public int ScannedCount { get; init; }

    /// <summary>
    /// Total number of addresses to scan.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Number of devices found so far.
    /// </summary>
    public int DevicesFound { get; init; }

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    public int ProgressPercent => TotalCount > 0 ? (int)(ScannedCount * 100.0 / TotalCount) : 0;
}
