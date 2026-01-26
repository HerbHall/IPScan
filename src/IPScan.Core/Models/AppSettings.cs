namespace IPScan.Core.Models;

/// <summary>
/// Application settings that persist between sessions.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Whether to automatically scan on startup.
    /// </summary>
    public bool ScanOnStartup { get; set; } = true;

    /// <summary>
    /// Subnet mode: "auto" to detect from interface, or custom CIDR.
    /// </summary>
    public string Subnet { get; set; } = "auto";

    /// <summary>
    /// Custom subnet in CIDR notation (used when Subnet != "auto").
    /// </summary>
    public string CustomSubnet { get; set; } = string.Empty;

    /// <summary>
    /// ID of the preferred network interface, or empty for auto-select.
    /// </summary>
    public string PreferredInterfaceId { get; set; } = string.Empty;

    /// <summary>
    /// Timeout in milliseconds for ping operations.
    /// </summary>
    public int ScanTimeoutMs { get; set; } = 1000;

    /// <summary>
    /// Maximum number of concurrent ping operations.
    /// </summary>
    public int MaxConcurrentScans { get; set; } = 100;

    /// <summary>
    /// Whether to automatically remove devices that are missing from scans.
    /// </summary>
    public bool AutoRemoveMissingDevices { get; set; }

    /// <summary>
    /// Number of consecutive missed scans before auto-removing a device.
    /// Only used if AutoRemoveMissingDevices is true.
    /// </summary>
    public int MissedScansBeforeRemoval { get; set; } = 5;

    /// <summary>
    /// Whether to show offline devices in the UI.
    /// </summary>
    public bool ShowOfflineDevices { get; set; } = true;

    /// <summary>
    /// Splash screen timeout in seconds.
    /// </summary>
    public int SplashTimeoutSeconds { get; set; } = 5;
}
