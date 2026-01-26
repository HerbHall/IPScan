using IPScan.Core.Models;

namespace IPScan.Core.Tests.Models;

public class ScanResultTests
{
    [Fact]
    public void Duration_CalculatesCorrectly()
    {
        var result = new ScanResult
        {
            StartTime = new DateTime(2026, 1, 1, 10, 0, 0),
            EndTime = new DateTime(2026, 1, 1, 10, 0, 30)
        };

        Assert.Equal(TimeSpan.FromSeconds(30), result.Duration);
    }

    [Fact]
    public void DevicesFound_ReturnsCountOfDiscoveredDevices()
    {
        var result = new ScanResult
        {
            DiscoveredDevices =
            [
                new DiscoveredDevice { IpAddress = "192.168.1.1" },
                new DiscoveredDevice { IpAddress = "192.168.1.2" },
                new DiscoveredDevice { IpAddress = "192.168.1.3" }
            ]
        };

        Assert.Equal(3, result.DevicesFound);
    }

    [Fact]
    public void DevicesFound_ReturnsZero_WhenNoDevices()
    {
        var result = new ScanResult();

        Assert.Equal(0, result.DevicesFound);
    }
}

public class ScanProgressEventArgsTests
{
    [Theory]
    [InlineData(0, 100, 0)]
    [InlineData(50, 100, 50)]
    [InlineData(100, 100, 100)]
    [InlineData(25, 200, 12)]  // 12.5% rounds to 12
    [InlineData(0, 0, 0)]  // Edge case: no addresses
    public void ProgressPercent_CalculatesCorrectly(int scanned, int total, int expected)
    {
        var args = new IPScan.Core.Services.ScanProgressEventArgs
        {
            ScannedCount = scanned,
            TotalCount = total
        };

        Assert.Equal(expected, args.ProgressPercent);
    }
}
