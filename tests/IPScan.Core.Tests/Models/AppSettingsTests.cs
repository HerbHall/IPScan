using IPScan.Core.Models;

namespace IPScan.Core.Tests.Models;

public class AppSettingsTests
{
    [Fact]
    public void AppSettings_HasCorrectDefaults()
    {
        var settings = new AppSettings();

        Assert.True(settings.ScanOnStartup);
        Assert.Equal("auto", settings.Subnet);
        Assert.Equal(string.Empty, settings.CustomSubnet);
        Assert.Equal(string.Empty, settings.PreferredInterfaceId);
        Assert.Equal(1000, settings.ScanTimeoutMs);
        Assert.Equal(100, settings.MaxConcurrentScans);
        Assert.False(settings.AutoRemoveMissingDevices);
        Assert.Equal(5, settings.MissedScansBeforeRemoval);
        Assert.True(settings.ShowOfflineDevices);
        Assert.Equal(5, settings.SplashTimeoutSeconds);
    }
}
