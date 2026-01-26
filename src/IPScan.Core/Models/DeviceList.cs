namespace IPScan.Core.Models;

/// <summary>
/// Container for persisted device data.
/// </summary>
public class DeviceList
{
    /// <summary>
    /// List of all known devices.
    /// </summary>
    public List<Device> Devices { get; set; } = [];

    /// <summary>
    /// Timestamp of the last scan.
    /// </summary>
    public DateTime? LastScanTime { get; set; }

    /// <summary>
    /// Total number of scans performed.
    /// </summary>
    public int TotalScans { get; set; }
}
