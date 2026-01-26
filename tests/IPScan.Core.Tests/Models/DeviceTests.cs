using IPScan.Core.Models;

namespace IPScan.Core.Tests.Models;

public class DeviceTests
{
    [Fact]
    public void Device_HasDefaultValues()
    {
        var device = new Device();

        Assert.NotEqual(Guid.Empty, device.Id);
        Assert.Equal(string.Empty, device.Name);
        Assert.Null(device.Hostname);
        Assert.Equal(string.Empty, device.IpAddress);
        Assert.Null(device.MacAddress);
        Assert.False(device.IsOnline);
        Assert.Equal(0, device.ConsecutiveMissedScans);
        Assert.Equal(string.Empty, device.Notes);
    }

    [Fact]
    public void DisplayName_ReturnsName_WhenNameIsSet()
    {
        var device = new Device
        {
            Name = "My Router",
            Hostname = "router.local",
            IpAddress = "192.168.1.1"
        };

        Assert.Equal("My Router", device.DisplayName);
    }

    [Fact]
    public void DisplayName_ReturnsHostname_WhenNameIsEmpty()
    {
        var device = new Device
        {
            Name = "",
            Hostname = "router.local",
            IpAddress = "192.168.1.1"
        };

        Assert.Equal("router.local", device.DisplayName);
    }

    [Fact]
    public void DisplayName_ReturnsIpAddress_WhenNameAndHostnameAreEmpty()
    {
        var device = new Device
        {
            Name = "",
            Hostname = null,
            IpAddress = "192.168.1.1"
        };

        Assert.Equal("192.168.1.1", device.DisplayName);
    }

    [Fact]
    public void DisplayName_ReturnsIpAddress_WhenNameIsWhitespace()
    {
        var device = new Device
        {
            Name = "   ",
            Hostname = "   ",
            IpAddress = "192.168.1.1"
        };

        Assert.Equal("192.168.1.1", device.DisplayName);
    }

    [Fact]
    public void FirstDiscovered_IsSetToCurrentTime()
    {
        var before = DateTime.UtcNow;
        var device = new Device();
        var after = DateTime.UtcNow;

        Assert.InRange(device.FirstDiscovered, before, after);
    }

    [Fact]
    public void LastSeen_IsSetToCurrentTime()
    {
        var before = DateTime.UtcNow;
        var device = new Device();
        var after = DateTime.UtcNow;

        Assert.InRange(device.LastSeen, before, after);
    }
}
