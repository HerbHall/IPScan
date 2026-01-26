using IPScan.Core.Models;

namespace IPScan.Core.Services;

/// <summary>
/// High-level service for managing devices and scanning operations.
/// </summary>
public interface IDeviceManager
{
    /// <summary>
    /// Event raised when scan starts.
    /// </summary>
    event EventHandler<ScanStartedEventArgs>? ScanStarted;

    /// <summary>
    /// Event raised when scan progress changes.
    /// </summary>
    event EventHandler<ScanProgressEventArgs>? ScanProgress;

    /// <summary>
    /// Event raised when scan completes.
    /// </summary>
    event EventHandler<ScanCompletedEventArgs>? ScanCompleted;

    /// <summary>
    /// Event raised when a device is discovered during scanning.
    /// </summary>
    event EventHandler<DeviceEventArgs>? DeviceDiscovered;

    /// <summary>
    /// Event raised when a device is updated.
    /// </summary>
    event EventHandler<DeviceEventArgs>? DeviceUpdated;

    /// <summary>
    /// Event raised when a device is removed.
    /// </summary>
    event EventHandler<DeviceEventArgs>? DeviceRemoved;

    /// <summary>
    /// Gets all known devices.
    /// </summary>
    Task<IReadOnlyList<Device>> GetAllDevicesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets devices filtered by online status.
    /// </summary>
    Task<IReadOnlyList<Device>> GetDevicesAsync(bool? onlineOnly = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a device by ID.
    /// </summary>
    Task<Device?> GetDeviceByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a network scan using the preferred or specified interface.
    /// </summary>
    Task<ScanResult> ScanAsync(string? interfaceId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually adds a device.
    /// </summary>
    Task<Device> AddDeviceAsync(Device device, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing device.
    /// </summary>
    Task<Device> UpdateDeviceAsync(Device device, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a device by ID.
    /// </summary>
    Task<bool> RemoveDeviceAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes devices that were not seen in recent scans (based on settings).
    /// </summary>
    Task<IReadOnlyList<Device>> RemoveMissingDevicesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets devices that are candidates for removal based on missed scan count.
    /// </summary>
    Task<IReadOnlyList<Device>> GetMissingDeviceCandidatesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Event args for scan started event.
/// </summary>
public class ScanStartedEventArgs : EventArgs
{
    public required string Subnet { get; init; }
    public required string InterfaceName { get; init; }
    public int TotalAddresses { get; init; }
}

/// <summary>
/// Event args for scan completed event.
/// </summary>
public class ScanCompletedEventArgs : EventArgs
{
    public required ScanResult Result { get; init; }
    public int NewDevicesFound { get; init; }
    public int DevicesUpdated { get; init; }
    public int DevicesAutoRemoved { get; init; }
}

/// <summary>
/// Event args for device events.
/// </summary>
public class DeviceEventArgs : EventArgs
{
    public required Device Device { get; init; }
}
