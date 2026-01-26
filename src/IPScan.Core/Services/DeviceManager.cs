using System.Net;
using IPScan.Core.Models;
using Microsoft.Extensions.Logging;

namespace IPScan.Core.Services;

/// <summary>
/// High-level service for managing devices and scanning operations.
/// </summary>
public class DeviceManager : IDeviceManager
{
    private readonly INetworkScanner _scanner;
    private readonly INetworkInterfaceService _interfaceService;
    private readonly IDeviceRepository _deviceRepository;
    private readonly ISettingsService _settingsService;
    private readonly ISubnetCalculator _subnetCalculator;
    private readonly ILogger<DeviceManager> _logger;

    public event EventHandler<ScanStartedEventArgs>? ScanStarted;
    public event EventHandler<ScanProgressEventArgs>? ScanProgress;
    public event EventHandler<ScanCompletedEventArgs>? ScanCompleted;
    public event EventHandler<DeviceEventArgs>? DeviceDiscovered;
    public event EventHandler<DeviceEventArgs>? DeviceUpdated;
    public event EventHandler<DeviceEventArgs>? DeviceRemoved;

    public DeviceManager(
        INetworkScanner scanner,
        INetworkInterfaceService interfaceService,
        IDeviceRepository deviceRepository,
        ISettingsService settingsService,
        ISubnetCalculator subnetCalculator,
        ILogger<DeviceManager> logger)
    {
        _scanner = scanner;
        _interfaceService = interfaceService;
        _deviceRepository = deviceRepository;
        _settingsService = settingsService;
        _subnetCalculator = subnetCalculator;
        _logger = logger;

        // Forward scanner progress events
        _scanner.ProgressChanged += (s, e) => ScanProgress?.Invoke(this, e);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Device>> GetAllDevicesAsync(CancellationToken cancellationToken = default)
    {
        var deviceList = await _deviceRepository.GetAllAsync(cancellationToken);
        return deviceList.Devices.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Device>> GetDevicesAsync(bool? onlineOnly = null, CancellationToken cancellationToken = default)
    {
        var devices = await GetAllDevicesAsync(cancellationToken);

        if (onlineOnly.HasValue)
        {
            return devices.Where(d => d.IsOnline == onlineOnly.Value).ToList().AsReadOnly();
        }

        return devices;
    }

    /// <inheritdoc />
    public async Task<Device?> GetDeviceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _deviceRepository.GetByIdAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ScanResult> ScanAsync(string? interfaceId = null, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.GetSettingsAsync(cancellationToken);

        // Get the network interface to use
        var networkInterface = !string.IsNullOrWhiteSpace(interfaceId)
            ? _interfaceService.GetInterface(interfaceId)
            : _interfaceService.GetPreferredInterface(settings.PreferredInterfaceId);

        if (networkInterface == null)
        {
            return new ScanResult
            {
                Success = false,
                ErrorMessage = "No suitable network interface found",
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow
            };
        }

        // Determine subnet to scan
        IPAddress? subnetMask = null;
        if (settings.Subnet != "auto" && !string.IsNullOrWhiteSpace(settings.CustomSubnet))
        {
            var parsed = _subnetCalculator.ParseCidr(settings.CustomSubnet);
            if (parsed != null)
            {
                subnetMask = _subnetCalculator.GetSubnetMaskFromCidr(parsed.Value.PrefixLength);
            }
        }

        // Calculate total addresses for event
        var effectiveMask = subnetMask ?? (string.IsNullOrEmpty(networkInterface.SubnetMask)
            ? IPAddress.Parse("255.255.255.0")
            : IPAddress.Parse(networkInterface.SubnetMask));
        var totalAddresses = _subnetCalculator.GetHostCount(effectiveMask);

        // Notify scan started
        ScanStarted?.Invoke(this, new ScanStartedEventArgs
        {
            Subnet = _subnetCalculator.GetCidrNotation(IPAddress.Parse(networkInterface.IpAddress), effectiveMask),
            InterfaceName = networkInterface.Name,
            TotalAddresses = totalAddresses
        });

        // Perform the scan
        var result = await _scanner.ScanAsync(
            networkInterface,
            subnetMask,
            settings.ScanTimeoutMs,
            settings.MaxConcurrentScans,
            cancellationToken);

        if (!result.Success)
        {
            ScanCompleted?.Invoke(this, new ScanCompletedEventArgs
            {
                Result = result,
                NewDevicesFound = 0,
                DevicesUpdated = 0,
                DevicesAutoRemoved = 0
            });
            return result;
        }

        // Process discovered devices
        var (newCount, updatedCount) = await ProcessDiscoveredDevicesAsync(result.DiscoveredDevices, cancellationToken);

        // Mark devices not found in scan as offline
        await MarkOfflineDevicesAsync(result.DiscoveredDevices, cancellationToken);

        // Handle auto-removal if enabled
        var autoRemovedCount = 0;
        if (settings.AutoRemoveMissingDevices)
        {
            var removed = await RemoveMissingDevicesAsync(cancellationToken);
            autoRemovedCount = removed.Count;
        }

        // Update scan metadata
        var deviceList = await _deviceRepository.GetAllAsync(cancellationToken);
        deviceList.LastScanTime = DateTime.UtcNow;
        deviceList.TotalScans++;
        await _deviceRepository.SaveAsync(deviceList, cancellationToken);

        // Notify scan completed
        ScanCompleted?.Invoke(this, new ScanCompletedEventArgs
        {
            Result = result,
            NewDevicesFound = newCount,
            DevicesUpdated = updatedCount,
            DevicesAutoRemoved = autoRemovedCount
        });

        return result;
    }

    private async Task<(int newCount, int updatedCount)> ProcessDiscoveredDevicesAsync(
        List<DiscoveredDevice> discoveredDevices,
        CancellationToken cancellationToken)
    {
        var newCount = 0;
        var updatedCount = 0;

        foreach (var discovered in discoveredDevices)
        {
            var existing = await _deviceRepository.GetByIpAddressAsync(discovered.IpAddress, cancellationToken);

            if (existing != null)
            {
                // Update existing device
                existing.IsOnline = true;
                existing.LastSeen = DateTime.UtcNow;
                existing.ConsecutiveMissedScans = 0;

                // Update hostname if we got one and didn't have one before
                if (!string.IsNullOrWhiteSpace(discovered.Hostname) && string.IsNullOrWhiteSpace(existing.Hostname))
                {
                    existing.Hostname = discovered.Hostname;
                }

                await _deviceRepository.UpsertAsync(existing, cancellationToken);
                DeviceUpdated?.Invoke(this, new DeviceEventArgs { Device = existing });
                updatedCount++;
            }
            else
            {
                // Add new device
                var newDevice = new Device
                {
                    IpAddress = discovered.IpAddress,
                    Hostname = discovered.Hostname,
                    Name = discovered.Hostname ?? string.Empty,
                    MacAddress = discovered.MacAddress,
                    IsOnline = true,
                    FirstDiscovered = DateTime.UtcNow,
                    LastSeen = DateTime.UtcNow,
                    ConsecutiveMissedScans = 0
                };

                await _deviceRepository.UpsertAsync(newDevice, cancellationToken);
                DeviceDiscovered?.Invoke(this, new DeviceEventArgs { Device = newDevice });
                newCount++;
            }
        }

        return (newCount, updatedCount);
    }

    private async Task MarkOfflineDevicesAsync(List<DiscoveredDevice> discoveredDevices, CancellationToken cancellationToken)
    {
        var discoveredIps = discoveredDevices.Select(d => d.IpAddress).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allDevices = await GetAllDevicesAsync(cancellationToken);

        // Take a copy to avoid collection modified exception when UpsertAsync modifies the cached list
        foreach (var device in allDevices.ToList())
        {
            if (!discoveredIps.Contains(device.IpAddress))
            {
                device.IsOnline = false;
                device.ConsecutiveMissedScans++;
                await _deviceRepository.UpsertAsync(device, cancellationToken);
                DeviceUpdated?.Invoke(this, new DeviceEventArgs { Device = device });
            }
        }
    }

    /// <inheritdoc />
    public async Task<Device> AddDeviceAsync(Device device, CancellationToken cancellationToken = default)
    {
        var result = await _deviceRepository.UpsertAsync(device, cancellationToken);
        DeviceDiscovered?.Invoke(this, new DeviceEventArgs { Device = result });
        return result;
    }

    /// <inheritdoc />
    public async Task<Device> UpdateDeviceAsync(Device device, CancellationToken cancellationToken = default)
    {
        var result = await _deviceRepository.UpsertAsync(device, cancellationToken);
        DeviceUpdated?.Invoke(this, new DeviceEventArgs { Device = result });
        return result;
    }

    /// <inheritdoc />
    public async Task<bool> RemoveDeviceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var device = await _deviceRepository.GetByIdAsync(id, cancellationToken);
        if (device == null)
            return false;

        var result = await _deviceRepository.RemoveAsync(id, cancellationToken);
        if (result)
        {
            DeviceRemoved?.Invoke(this, new DeviceEventArgs { Device = device });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Device>> RemoveMissingDevicesAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.GetSettingsAsync(cancellationToken);
        var candidates = await GetMissingDeviceCandidatesAsync(cancellationToken);

        var removed = new List<Device>();
        foreach (var device in candidates)
        {
            if (device.ConsecutiveMissedScans >= settings.MissedScansBeforeRemoval)
            {
                await _deviceRepository.RemoveAsync(device.Id, cancellationToken);
                DeviceRemoved?.Invoke(this, new DeviceEventArgs { Device = device });
                removed.Add(device);
                _logger.LogInformation("Auto-removed device {Name} ({IP}) after {Count} missed scans",
                    device.DisplayName, device.IpAddress, device.ConsecutiveMissedScans);
            }
        }

        return removed.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Device>> GetMissingDeviceCandidatesAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.GetSettingsAsync(cancellationToken);
        var allDevices = await GetAllDevicesAsync(cancellationToken);

        return allDevices
            .Where(d => !d.IsOnline && d.ConsecutiveMissedScans >= settings.MissedScansBeforeRemoval)
            .ToList()
            .AsReadOnly();
    }
}
