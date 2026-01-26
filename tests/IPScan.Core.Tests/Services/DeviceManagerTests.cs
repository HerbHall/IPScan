using IPScan.Core.Models;
using IPScan.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace IPScan.Core.Tests.Services;

public class DeviceManagerTests
{
    private readonly INetworkScanner _scanner;
    private readonly INetworkInterfaceService _interfaceService;
    private readonly IDeviceRepository _deviceRepository;
    private readonly ISettingsService _settingsService;
    private readonly ISubnetCalculator _subnetCalculator;
    private readonly DeviceManager _manager;
    private readonly DeviceList _deviceList;

    public DeviceManagerTests()
    {
        _scanner = Substitute.For<INetworkScanner>();
        _interfaceService = Substitute.For<INetworkInterfaceService>();
        _deviceRepository = Substitute.For<IDeviceRepository>();
        _settingsService = Substitute.For<ISettingsService>();
        _subnetCalculator = new SubnetCalculator();

        _deviceList = new DeviceList();
        _deviceRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(_deviceList);
        _settingsService.GetSettingsAsync(Arg.Any<CancellationToken>()).Returns(new AppSettings());

        _manager = new DeviceManager(
            _scanner,
            _interfaceService,
            _deviceRepository,
            _settingsService,
            _subnetCalculator,
            NullLogger<DeviceManager>.Instance);
    }

    [Fact]
    public async Task GetAllDevicesAsync_ReturnsAllDevices()
    {
        _deviceList.Devices.Add(new Device { IpAddress = "192.168.1.1" });
        _deviceList.Devices.Add(new Device { IpAddress = "192.168.1.2" });

        var result = await _manager.GetAllDevicesAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetDevicesAsync_FiltersOnlineDevices()
    {
        _deviceList.Devices.Add(new Device { IpAddress = "192.168.1.1", IsOnline = true });
        _deviceList.Devices.Add(new Device { IpAddress = "192.168.1.2", IsOnline = false });

        var result = await _manager.GetDevicesAsync(onlineOnly: true);

        Assert.Single(result);
        Assert.Equal("192.168.1.1", result[0].IpAddress);
    }

    [Fact]
    public async Task GetDevicesAsync_FiltersOfflineDevices()
    {
        _deviceList.Devices.Add(new Device { IpAddress = "192.168.1.1", IsOnline = true });
        _deviceList.Devices.Add(new Device { IpAddress = "192.168.1.2", IsOnline = false });

        var result = await _manager.GetDevicesAsync(onlineOnly: false);

        Assert.Single(result);
        Assert.Equal("192.168.1.2", result[0].IpAddress);
    }

    [Fact]
    public async Task GetDeviceByIdAsync_ReturnsDevice()
    {
        var device = new Device { IpAddress = "192.168.1.1" };
        _deviceRepository.GetByIdAsync(device.Id, Arg.Any<CancellationToken>()).Returns(device);

        var result = await _manager.GetDeviceByIdAsync(device.Id);

        Assert.NotNull(result);
        Assert.Equal(device.Id, result.Id);
    }

    [Fact]
    public async Task AddDeviceAsync_AddsAndRaisesEvent()
    {
        var device = new Device { IpAddress = "192.168.1.1" };
        _deviceRepository.UpsertAsync(device, Arg.Any<CancellationToken>()).Returns(device);

        DeviceEventArgs? eventArgs = null;
        _manager.DeviceDiscovered += (s, e) => eventArgs = e;

        var result = await _manager.AddDeviceAsync(device);

        Assert.NotNull(eventArgs);
        Assert.Equal(device.Id, eventArgs.Device.Id);
        await _deviceRepository.Received(1).UpsertAsync(device, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDeviceAsync_UpdatesAndRaisesEvent()
    {
        var device = new Device { IpAddress = "192.168.1.1", Name = "Updated" };
        _deviceRepository.UpsertAsync(device, Arg.Any<CancellationToken>()).Returns(device);

        DeviceEventArgs? eventArgs = null;
        _manager.DeviceUpdated += (s, e) => eventArgs = e;

        var result = await _manager.UpdateDeviceAsync(device);

        Assert.NotNull(eventArgs);
        Assert.Equal("Updated", eventArgs.Device.Name);
    }

    [Fact]
    public async Task RemoveDeviceAsync_RemovesAndRaisesEvent()
    {
        var device = new Device { IpAddress = "192.168.1.1" };
        _deviceRepository.GetByIdAsync(device.Id, Arg.Any<CancellationToken>()).Returns(device);
        _deviceRepository.RemoveAsync(device.Id, Arg.Any<CancellationToken>()).Returns(true);

        DeviceEventArgs? eventArgs = null;
        _manager.DeviceRemoved += (s, e) => eventArgs = e;

        var result = await _manager.RemoveDeviceAsync(device.Id);

        Assert.True(result);
        Assert.NotNull(eventArgs);
        Assert.Equal(device.Id, eventArgs.Device.Id);
    }

    [Fact]
    public async Task RemoveDeviceAsync_ReturnsFalse_WhenDeviceNotFound()
    {
        _deviceRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Device?)null);

        var result = await _manager.RemoveDeviceAsync(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task ScanAsync_ReturnsError_WhenNoInterfaceFound()
    {
        _interfaceService.GetPreferredInterface(Arg.Any<string?>()).Returns((NetworkInterfaceInfo?)null);

        var result = await _manager.ScanAsync();

        Assert.False(result.Success);
        Assert.Contains("No suitable network interface", result.ErrorMessage);
    }

    [Fact]
    public async Task GetMissingDeviceCandidatesAsync_ReturnsDevicesPastThreshold()
    {
        var settings = new AppSettings { MissedScansBeforeRemoval = 3 };
        _settingsService.GetSettingsAsync(Arg.Any<CancellationToken>()).Returns(settings);

        _deviceList.Devices.Add(new Device { IpAddress = "192.168.1.1", IsOnline = false, ConsecutiveMissedScans = 2 });
        _deviceList.Devices.Add(new Device { IpAddress = "192.168.1.2", IsOnline = false, ConsecutiveMissedScans = 3 });
        _deviceList.Devices.Add(new Device { IpAddress = "192.168.1.3", IsOnline = false, ConsecutiveMissedScans = 5 });
        _deviceList.Devices.Add(new Device { IpAddress = "192.168.1.4", IsOnline = true, ConsecutiveMissedScans = 0 });

        var result = await _manager.GetMissingDeviceCandidatesAsync();

        Assert.Equal(2, result.Count);
        Assert.All(result, d => Assert.True(d.ConsecutiveMissedScans >= 3));
    }

    [Fact]
    public async Task RemoveMissingDevicesAsync_RemovesDevicesPastThreshold()
    {
        var settings = new AppSettings { MissedScansBeforeRemoval = 3 };
        _settingsService.GetSettingsAsync(Arg.Any<CancellationToken>()).Returns(settings);

        var deviceToRemove = new Device { IpAddress = "192.168.1.2", IsOnline = false, ConsecutiveMissedScans = 5 };
        _deviceList.Devices.Add(new Device { IpAddress = "192.168.1.1", IsOnline = false, ConsecutiveMissedScans = 2 });
        _deviceList.Devices.Add(deviceToRemove);

        _deviceRepository.RemoveAsync(deviceToRemove.Id, Arg.Any<CancellationToken>()).Returns(true);

        var removed = await _manager.RemoveMissingDevicesAsync();

        Assert.Single(removed);
        Assert.Equal(deviceToRemove.Id, removed[0].Id);
        await _deviceRepository.Received(1).RemoveAsync(deviceToRemove.Id, Arg.Any<CancellationToken>());
    }
}
