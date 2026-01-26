using System.Text.Json;
using System.Text.Json.Serialization;
using IPScan.Core.Models;
using Microsoft.Extensions.Logging;

namespace IPScan.Core.Services;

/// <summary>
/// JSON file-based implementation of device repository.
/// </summary>
public class JsonDeviceRepository : IDeviceRepository
{
    private readonly string _filePath;
    private readonly ILogger<JsonDeviceRepository> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private DeviceList? _cache;
    private bool _memoryOnlyMode;
    private bool _storageAvailable = true;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public JsonDeviceRepository(ILogger<JsonDeviceRepository> logger, string? filePath = null)
    {
        _logger = logger;
        _filePath = filePath ?? GetDefaultFilePath();
    }

    /// <summary>
    /// Gets whether storage is available (not in memory-only mode).
    /// </summary>
    public bool IsStorageAvailable => _storageAvailable && !_memoryOnlyMode;

    /// <summary>
    /// Gets whether the service is running in memory-only mode.
    /// </summary>
    public bool IsMemoryOnlyMode => _memoryOnlyMode;

    /// <summary>
    /// Enables memory-only mode (no file persistence).
    /// </summary>
    public void EnableMemoryOnlyMode()
    {
        _memoryOnlyMode = true;
        _logger.LogWarning("Enabled memory-only mode - devices will not be persisted");
    }

    private static string GetDefaultFilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "IPScan", "devices.json");
    }

    /// <inheritdoc />
    public async Task<DeviceList> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_cache != null)
                return _cache;

            if (!File.Exists(_filePath))
            {
                _cache = new DeviceList();
                return _cache;
            }

            try
            {
                var json = await File.ReadAllTextAsync(_filePath, cancellationToken);
                _cache = JsonSerializer.Deserialize<DeviceList>(json, JsonOptions) ?? new DeviceList();
                _logger.LogDebug("Loaded {Count} devices from {Path}", _cache.Devices.Count, _filePath);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse devices file, starting fresh");
                _cache = new DeviceList();
            }

            return _cache;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(DeviceList deviceList, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Always update cache
            _cache = deviceList;

            // Skip file I/O if in memory-only mode
            if (_memoryOnlyMode)
            {
                _logger.LogDebug("Device list updated in memory (memory-only mode)");
                return;
            }

            try
            {
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(deviceList, JsonOptions);
                await File.WriteAllTextAsync(_filePath, json, cancellationToken);
                _storageAvailable = true;
                _logger.LogDebug("Saved {Count} devices to {Path}", deviceList.Devices.Count, _filePath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _storageAvailable = false;
                _logger.LogError(ex, "Failed to save devices to {Path} - storage may be unavailable", _filePath);
                throw new InvalidOperationException($"Unable to save devices: {ex.Message}", ex);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Device?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deviceList = await GetAllAsync(cancellationToken);
        return deviceList.Devices.FirstOrDefault(d => d.Id == id);
    }

    /// <inheritdoc />
    public async Task<Device?> GetByIpAddressAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        var deviceList = await GetAllAsync(cancellationToken);
        return deviceList.Devices.FirstOrDefault(d =>
            string.Equals(d.IpAddress, ipAddress, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public async Task<Device> UpsertAsync(Device device, CancellationToken cancellationToken = default)
    {
        var deviceList = await GetAllAsync(cancellationToken);

        var existing = deviceList.Devices.FirstOrDefault(d => d.Id == device.Id);
        if (existing != null)
        {
            // Update existing device
            var index = deviceList.Devices.IndexOf(existing);
            deviceList.Devices[index] = device;
            _logger.LogDebug("Updated device {Id} ({Name})", device.Id, device.DisplayName);
        }
        else
        {
            // Add new device
            deviceList.Devices.Add(device);
            _logger.LogDebug("Added device {Id} ({Name})", device.Id, device.DisplayName);
        }

        await SaveAsync(deviceList, cancellationToken);
        return device;
    }

    /// <inheritdoc />
    public async Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deviceList = await GetAllAsync(cancellationToken);

        var device = deviceList.Devices.FirstOrDefault(d => d.Id == id);
        if (device == null)
            return false;

        deviceList.Devices.Remove(device);
        await SaveAsync(deviceList, cancellationToken);
        _logger.LogDebug("Removed device {Id} ({Name})", device.Id, device.DisplayName);

        return true;
    }

    /// <inheritdoc />
    public async Task<int> RemoveRangeAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var deviceList = await GetAllAsync(cancellationToken);
        var idSet = ids.ToHashSet();

        var toRemove = deviceList.Devices.Where(d => idSet.Contains(d.Id)).ToList();
        foreach (var device in toRemove)
        {
            deviceList.Devices.Remove(device);
            _logger.LogDebug("Removed device {Id} ({Name})", device.Id, device.DisplayName);
        }

        if (toRemove.Count > 0)
        {
            await SaveAsync(deviceList, cancellationToken);
        }

        return toRemove.Count;
    }
}
