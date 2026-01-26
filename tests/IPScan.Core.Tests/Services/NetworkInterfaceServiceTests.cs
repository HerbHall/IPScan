using IPScan.Core.Models;
using IPScan.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace IPScan.Core.Tests.Services;

public class NetworkInterfaceServiceTests
{
    private readonly NetworkInterfaceService _service;

    public NetworkInterfaceServiceTests()
    {
        _service = new NetworkInterfaceService(NullLogger<NetworkInterfaceService>.Instance);
    }

    [Fact]
    public void GetAllInterfaces_ReturnsInterfaces()
    {
        var interfaces = _service.GetAllInterfaces();

        // Should have at least loopback
        Assert.NotEmpty(interfaces);
    }

    [Fact]
    public void GetAllInterfaces_IncludesLoopback()
    {
        var interfaces = _service.GetAllInterfaces();

        var loopback = interfaces.FirstOrDefault(i => i.InterfaceType == NetworkInterfaceType.Loopback);
        Assert.NotNull(loopback);
        Assert.True(loopback.IpAddress == "127.0.0.1" || loopback.IpAddress.StartsWith("127."));
    }

    [Fact]
    public void GetActiveInterfaces_ExcludesLoopback()
    {
        var interfaces = _service.GetActiveInterfaces();

        Assert.DoesNotContain(interfaces, i => i.InterfaceType == NetworkInterfaceType.Loopback);
    }

    [Fact]
    public void GetActiveInterfaces_OnlyReturnsUpInterfaces()
    {
        var interfaces = _service.GetActiveInterfaces();

        Assert.All(interfaces, i => Assert.True(i.IsUp));
    }

    [Fact]
    public void GetActiveInterfaces_OnlyReturnsInterfacesWithIp()
    {
        var interfaces = _service.GetActiveInterfaces();

        Assert.All(interfaces, i => Assert.False(string.IsNullOrEmpty(i.IpAddress)));
    }

    [Fact]
    public void GetInterface_ReturnsNull_WhenIdIsEmpty()
    {
        var result = _service.GetInterface("");

        Assert.Null(result);
    }

    [Fact]
    public void GetInterface_ReturnsNull_WhenIdNotFound()
    {
        var result = _service.GetInterface("nonexistent-interface-id");

        Assert.Null(result);
    }

    [Fact]
    public void GetInterface_ReturnsInterface_WhenIdExists()
    {
        var interfaces = _service.GetAllInterfaces();
        if (interfaces.Count == 0) return; // Skip if no interfaces

        var firstInterface = interfaces[0];
        var result = _service.GetInterface(firstInterface.Id);

        Assert.NotNull(result);
        Assert.Equal(firstInterface.Id, result.Id);
    }

    [Fact]
    public void GetPreferredInterface_ReturnsDefault_WhenPreferredIdIsNull()
    {
        var result = _service.GetPreferredInterface(null);

        // Should return some interface (default behavior)
        // May be null if no active interfaces exist
        var defaultInterface = _service.GetDefaultInterface();
        Assert.Equal(defaultInterface?.Id, result?.Id);
    }

    [Fact]
    public void GetPreferredInterface_ReturnsDefault_WhenPreferredNotFound()
    {
        var result = _service.GetPreferredInterface("nonexistent-id");

        var defaultInterface = _service.GetDefaultInterface();
        Assert.Equal(defaultInterface?.Id, result?.Id);
    }

    [Fact]
    public void GetDefaultInterface_PrioritizesEthernet()
    {
        var activeInterfaces = _service.GetActiveInterfaces();
        var hasEthernet = activeInterfaces.Any(i => i.InterfaceType == NetworkInterfaceType.Ethernet);

        if (!hasEthernet) return; // Skip if no Ethernet interfaces

        var defaultInterface = _service.GetDefaultInterface();

        Assert.NotNull(defaultInterface);
        Assert.Equal(NetworkInterfaceType.Ethernet, defaultInterface.InterfaceType);
    }

    [Fact]
    public void GetDefaultInterface_FallsBackToWireless_WhenNoEthernet()
    {
        var activeInterfaces = _service.GetActiveInterfaces();
        var hasEthernet = activeInterfaces.Any(i => i.InterfaceType == NetworkInterfaceType.Ethernet);
        var hasWireless = activeInterfaces.Any(i => i.InterfaceType == NetworkInterfaceType.Wireless);

        if (hasEthernet || !hasWireless) return; // Skip if has Ethernet or no Wireless

        var defaultInterface = _service.GetDefaultInterface();

        Assert.NotNull(defaultInterface);
        Assert.Equal(NetworkInterfaceType.Wireless, defaultInterface.InterfaceType);
    }
}
