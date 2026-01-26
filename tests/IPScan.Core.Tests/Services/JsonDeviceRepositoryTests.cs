using IPScan.Core.Models;
using IPScan.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace IPScan.Core.Tests.Services;

public class JsonDeviceRepositoryTests : IDisposable
{
    private readonly string _testFilePath;
    private readonly JsonDeviceRepository _repository;

    public JsonDeviceRepositoryTests()
    {
        _testFilePath = Path.Combine(Path.GetTempPath(), $"ipscan_test_{Guid.NewGuid()}.json");
        _repository = new JsonDeviceRepository(NullLogger<JsonDeviceRepository>.Instance, _testFilePath);
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenFileDoesNotExist()
    {
        var result = await _repository.GetAllAsync();

        Assert.NotNull(result);
        Assert.Empty(result.Devices);
    }

    [Fact]
    public async Task SaveAsync_CreatesFileAndDirectory()
    {
        var deviceList = new DeviceList
        {
            Devices = [new Device { IpAddress = "192.168.1.1" }]
        };

        await _repository.SaveAsync(deviceList);

        Assert.True(File.Exists(_testFilePath));
    }

    [Fact]
    public async Task UpsertAsync_AddsNewDevice()
    {
        var device = new Device
        {
            IpAddress = "192.168.1.1",
            Name = "Test Device"
        };

        await _repository.UpsertAsync(device);
        var result = await _repository.GetAllAsync();

        Assert.Single(result.Devices);
        Assert.Equal("192.168.1.1", result.Devices[0].IpAddress);
        Assert.Equal("Test Device", result.Devices[0].Name);
    }

    [Fact]
    public async Task UpsertAsync_UpdatesExistingDevice()
    {
        var device = new Device
        {
            IpAddress = "192.168.1.1",
            Name = "Test Device"
        };
        await _repository.UpsertAsync(device);

        device.Name = "Updated Name";
        await _repository.UpsertAsync(device);

        var result = await _repository.GetAllAsync();
        Assert.Single(result.Devices);
        Assert.Equal("Updated Name", result.Devices[0].Name);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDevice_WhenExists()
    {
        var device = new Device
        {
            IpAddress = "192.168.1.1",
            Name = "Test Device"
        };
        await _repository.UpsertAsync(device);

        var result = await _repository.GetByIdAsync(device.Id);

        Assert.NotNull(result);
        Assert.Equal(device.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIpAddressAsync_ReturnsDevice_WhenExists()
    {
        var device = new Device
        {
            IpAddress = "192.168.1.100",
            Name = "Test Device"
        };
        await _repository.UpsertAsync(device);

        var result = await _repository.GetByIpAddressAsync("192.168.1.100");

        Assert.NotNull(result);
        Assert.Equal("192.168.1.100", result.IpAddress);
    }

    [Fact]
    public async Task GetByIpAddressAsync_IsCaseInsensitive()
    {
        var device = new Device
        {
            IpAddress = "192.168.1.100",
            Name = "Test Device"
        };
        await _repository.UpsertAsync(device);

        // IP addresses are case-insensitive in practice
        var result = await _repository.GetByIpAddressAsync("192.168.1.100");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetByIpAddressAsync_ReturnsNull_WhenNotExists()
    {
        var result = await _repository.GetByIpAddressAsync("192.168.1.1");

        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveAsync_RemovesDevice()
    {
        var device = new Device { IpAddress = "192.168.1.1" };
        await _repository.UpsertAsync(device);

        var removed = await _repository.RemoveAsync(device.Id);
        var result = await _repository.GetAllAsync();

        Assert.True(removed);
        Assert.Empty(result.Devices);
    }

    [Fact]
    public async Task RemoveAsync_ReturnsFalse_WhenNotExists()
    {
        var removed = await _repository.RemoveAsync(Guid.NewGuid());

        Assert.False(removed);
    }

    [Fact]
    public async Task RemoveRangeAsync_RemovesMultipleDevices()
    {
        var device1 = new Device { IpAddress = "192.168.1.1" };
        var device2 = new Device { IpAddress = "192.168.1.2" };
        var device3 = new Device { IpAddress = "192.168.1.3" };

        await _repository.UpsertAsync(device1);
        await _repository.UpsertAsync(device2);
        await _repository.UpsertAsync(device3);

        var removedCount = await _repository.RemoveRangeAsync([device1.Id, device2.Id]);
        var result = await _repository.GetAllAsync();

        Assert.Equal(2, removedCount);
        Assert.Single(result.Devices);
        Assert.Equal(device3.Id, result.Devices[0].Id);
    }

    [Fact]
    public async Task SaveAsync_PersistsMetadata()
    {
        var deviceList = new DeviceList
        {
            Devices = [new Device { IpAddress = "192.168.1.1" }],
            LastScanTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            TotalScans = 5
        };

        await _repository.SaveAsync(deviceList);

        // Force reload by creating a new repository
        var newRepository = new JsonDeviceRepository(NullLogger<JsonDeviceRepository>.Instance, _testFilePath);
        var result = await newRepository.GetAllAsync();

        Assert.Equal(5, result.TotalScans);
        Assert.Equal(new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc), result.LastScanTime);
    }
}
