using IPScan.Core.Models;
using IPScan.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace IPScan.Core.Tests.Services;

public class JsonSettingsServiceTests : IDisposable
{
    private readonly string _testFilePath;
    private readonly JsonSettingsService _service;

    public JsonSettingsServiceTests()
    {
        _testFilePath = Path.Combine(Path.GetTempPath(), $"ipscan_settings_test_{Guid.NewGuid()}.json");
        _service = new JsonSettingsService(NullLogger<JsonSettingsService>.Instance, _testFilePath);
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }

    [Fact]
    public async Task GetSettingsAsync_ReturnsDefaults_WhenFileDoesNotExist()
    {
        var result = await _service.GetSettingsAsync();

        Assert.NotNull(result);
        Assert.True(result.ScanOnStartup);
        Assert.Equal("auto", result.Subnet);
    }

    [Fact]
    public async Task SaveSettingsAsync_CreatesFile()
    {
        var settings = new AppSettings
        {
            ScanOnStartup = false,
            ScanTimeoutMs = 2000
        };

        await _service.SaveSettingsAsync(settings);

        Assert.True(File.Exists(_testFilePath));
    }

    [Fact]
    public async Task SaveSettingsAsync_PersistsSettings()
    {
        var settings = new AppSettings
        {
            ScanOnStartup = false,
            Subnet = "custom",
            CustomSubnet = "10.0.0.0/8",
            PreferredInterfaceId = "test-interface",
            ScanTimeoutMs = 2000,
            MaxConcurrentScans = 50,
            AutoRemoveMissingDevices = true,
            MissedScansBeforeRemoval = 10,
            ShowOfflineDevices = false,
            SplashTimeoutSeconds = 3
        };

        await _service.SaveSettingsAsync(settings);

        // Create new service to force reload
        var newService = new JsonSettingsService(NullLogger<JsonSettingsService>.Instance, _testFilePath);
        var result = await newService.GetSettingsAsync();

        Assert.False(result.ScanOnStartup);
        Assert.Equal("custom", result.Subnet);
        Assert.Equal("10.0.0.0/8", result.CustomSubnet);
        Assert.Equal("test-interface", result.PreferredInterfaceId);
        Assert.Equal(2000, result.ScanTimeoutMs);
        Assert.Equal(50, result.MaxConcurrentScans);
        Assert.True(result.AutoRemoveMissingDevices);
        Assert.Equal(10, result.MissedScansBeforeRemoval);
        Assert.False(result.ShowOfflineDevices);
        Assert.Equal(3, result.SplashTimeoutSeconds);
    }

    [Fact]
    public async Task ResetToDefaultsAsync_RestoresDefaults()
    {
        // First, save custom settings
        var settings = new AppSettings
        {
            ScanOnStartup = false,
            ScanTimeoutMs = 9999
        };
        await _service.SaveSettingsAsync(settings);

        // Reset to defaults
        await _service.ResetToDefaultsAsync();

        var result = await _service.GetSettingsAsync();
        Assert.True(result.ScanOnStartup);
        Assert.Equal(1000, result.ScanTimeoutMs);
    }

    [Fact]
    public async Task GetSettingsAsync_CachesSettings()
    {
        var settings = new AppSettings { ScanTimeoutMs = 5000 };
        await _service.SaveSettingsAsync(settings);

        // Modify file directly
        await File.WriteAllTextAsync(_testFilePath, "{}");

        // Should return cached value, not the modified file
        var result = await _service.GetSettingsAsync();
        Assert.Equal(5000, result.ScanTimeoutMs);
    }
}
