using IPScan.Core.Models;

namespace IPScan.Core.Services;

/// <summary>
/// Repository for persisting device data.
/// </summary>
public interface IDeviceRepository
{
    /// <summary>
    /// Gets all devices from storage.
    /// </summary>
    Task<DeviceList> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the device list to storage.
    /// </summary>
    Task SaveAsync(DeviceList deviceList, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a device by ID.
    /// </summary>
    Task<Device?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a device by IP address.
    /// </summary>
    Task<Device?> GetByIpAddressAsync(string ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds or updates a device.
    /// </summary>
    Task<Device> UpsertAsync(Device device, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a device by ID.
    /// </summary>
    Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes multiple devices by their IDs.
    /// </summary>
    Task<int> RemoveRangeAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}
