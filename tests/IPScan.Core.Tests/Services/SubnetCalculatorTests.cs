using System.Net;
using IPScan.Core.Services;

namespace IPScan.Core.Tests.Services;

public class SubnetCalculatorTests
{
    private readonly SubnetCalculator _calculator = new();

    #region GetNetworkAddress Tests

    [Theory]
    [InlineData("192.168.1.100", "255.255.255.0", "192.168.1.0")]
    [InlineData("192.168.1.100", "255.255.0.0", "192.168.0.0")]
    [InlineData("10.0.0.50", "255.0.0.0", "10.0.0.0")]
    [InlineData("172.16.50.100", "255.255.255.128", "172.16.50.0")]
    [InlineData("192.168.1.129", "255.255.255.128", "192.168.1.128")]
    public void GetNetworkAddress_ReturnsCorrectNetwork(string ip, string mask, string expected)
    {
        var result = _calculator.GetNetworkAddress(IPAddress.Parse(ip), IPAddress.Parse(mask));
        Assert.Equal(expected, result.ToString());
    }

    #endregion

    #region GetBroadcastAddress Tests

    [Theory]
    [InlineData("192.168.1.100", "255.255.255.0", "192.168.1.255")]
    [InlineData("192.168.1.100", "255.255.0.0", "192.168.255.255")]
    [InlineData("10.0.0.50", "255.0.0.0", "10.255.255.255")]
    [InlineData("172.16.50.100", "255.255.255.128", "172.16.50.127")]
    [InlineData("192.168.1.129", "255.255.255.128", "192.168.1.255")]
    public void GetBroadcastAddress_ReturnsCorrectBroadcast(string ip, string mask, string expected)
    {
        var result = _calculator.GetBroadcastAddress(IPAddress.Parse(ip), IPAddress.Parse(mask));
        Assert.Equal(expected, result.ToString());
    }

    #endregion

    #region GetHostCount Tests

    [Theory]
    [InlineData("255.255.255.0", 254)]      // /24
    [InlineData("255.255.255.128", 126)]    // /25
    [InlineData("255.255.255.192", 62)]     // /26
    [InlineData("255.255.255.252", 2)]      // /30
    [InlineData("255.255.0.0", 65534)]      // /16
    [InlineData("255.0.0.0", 16777214)]     // /8
    public void GetHostCount_ReturnsCorrectCount(string mask, int expected)
    {
        var result = _calculator.GetHostCount(IPAddress.Parse(mask));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("255.255.255.254")]  // /31
    [InlineData("255.255.255.255")]  // /32
    public void GetHostCount_ReturnsZeroForSmallSubnets(string mask)
    {
        var result = _calculator.GetHostCount(IPAddress.Parse(mask));
        Assert.Equal(0, result);
    }

    #endregion

    #region GetCidrPrefixLength Tests

    [Theory]
    [InlineData("255.255.255.0", 24)]
    [InlineData("255.255.255.128", 25)]
    [InlineData("255.255.255.192", 26)]
    [InlineData("255.255.255.224", 27)]
    [InlineData("255.255.255.240", 28)]
    [InlineData("255.255.255.248", 29)]
    [InlineData("255.255.255.252", 30)]
    [InlineData("255.255.255.254", 31)]
    [InlineData("255.255.255.255", 32)]
    [InlineData("255.255.0.0", 16)]
    [InlineData("255.0.0.0", 8)]
    [InlineData("0.0.0.0", 0)]
    public void GetCidrPrefixLength_ReturnsCorrectLength(string mask, int expected)
    {
        var result = _calculator.GetCidrPrefixLength(IPAddress.Parse(mask));
        Assert.Equal(expected, result);
    }

    #endregion

    #region GetSubnetMaskFromCidr Tests

    [Theory]
    [InlineData(24, "255.255.255.0")]
    [InlineData(25, "255.255.255.128")]
    [InlineData(26, "255.255.255.192")]
    [InlineData(16, "255.255.0.0")]
    [InlineData(8, "255.0.0.0")]
    [InlineData(0, "0.0.0.0")]
    [InlineData(32, "255.255.255.255")]
    public void GetSubnetMaskFromCidr_ReturnsCorrectMask(int prefix, string expected)
    {
        var result = _calculator.GetSubnetMaskFromCidr(prefix);
        Assert.Equal(expected, result.ToString());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(33)]
    public void GetSubnetMaskFromCidr_ThrowsForInvalidPrefix(int prefix)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _calculator.GetSubnetMaskFromCidr(prefix));
    }

    #endregion

    #region ParseCidr Tests

    [Theory]
    [InlineData("192.168.1.0/24", "192.168.1.0", 24)]
    [InlineData("10.0.0.0/8", "10.0.0.0", 8)]
    [InlineData("172.16.0.0/16", "172.16.0.0", 16)]
    [InlineData("192.168.1.100/24", "192.168.1.0", 24)] // Normalizes to network address
    public void ParseCidr_ParsesValidCidr(string cidr, string expectedNetwork, int expectedPrefix)
    {
        var result = _calculator.ParseCidr(cidr);

        Assert.NotNull(result);
        Assert.Equal(expectedNetwork, result.Value.Network.ToString());
        Assert.Equal(expectedPrefix, result.Value.PrefixLength);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("192.168.1.0")]  // Missing prefix
    [InlineData("192.168.1.0/")]  // Empty prefix
    [InlineData("192.168.1.0/abc")]  // Non-numeric prefix
    [InlineData("192.168.1.0/33")]  // Invalid prefix
    [InlineData("192.168.1.0/-1")]  // Negative prefix
    [InlineData("invalid/24")]  // Invalid IP
    [InlineData(null)]
    public void ParseCidr_ReturnsNullForInvalidCidr(string? cidr)
    {
        var result = _calculator.ParseCidr(cidr!);
        Assert.Null(result);
    }

    #endregion

    #region GetCidrNotation Tests

    [Theory]
    [InlineData("192.168.1.100", "255.255.255.0", "192.168.1.0/24")]
    [InlineData("10.0.0.50", "255.0.0.0", "10.0.0.0/8")]
    [InlineData("172.16.50.100", "255.255.255.128", "172.16.50.0/25")]
    public void GetCidrNotation_ReturnsCorrectNotation(string ip, string mask, string expected)
    {
        var result = _calculator.GetCidrNotation(IPAddress.Parse(ip), IPAddress.Parse(mask));
        Assert.Equal(expected, result);
    }

    #endregion

    #region GetHostAddresses Tests

    [Fact]
    public void GetHostAddresses_ReturnsCorrectAddresses_ForSlash30()
    {
        var ip = IPAddress.Parse("192.168.1.0");
        var mask = IPAddress.Parse("255.255.255.252");

        var addresses = _calculator.GetHostAddresses(ip, mask).ToList();

        Assert.Equal(2, addresses.Count);
        Assert.Equal("192.168.1.1", addresses[0].ToString());
        Assert.Equal("192.168.1.2", addresses[1].ToString());
    }

    [Fact]
    public void GetHostAddresses_ExcludesNetworkAndBroadcast()
    {
        var ip = IPAddress.Parse("192.168.1.0");
        var mask = IPAddress.Parse("255.255.255.248"); // /29 = 6 usable hosts

        var addresses = _calculator.GetHostAddresses(ip, mask).ToList();

        Assert.Equal(6, addresses.Count);
        Assert.DoesNotContain(addresses, a => a.ToString() == "192.168.1.0"); // Network
        Assert.DoesNotContain(addresses, a => a.ToString() == "192.168.1.7"); // Broadcast
        Assert.Equal("192.168.1.1", addresses.First().ToString());
        Assert.Equal("192.168.1.6", addresses.Last().ToString());
    }

    [Fact]
    public void GetHostAddresses_ReturnsCorrectCount_ForSlash24()
    {
        var ip = IPAddress.Parse("192.168.1.0");
        var mask = IPAddress.Parse("255.255.255.0");

        var addresses = _calculator.GetHostAddresses(ip, mask).ToList();

        Assert.Equal(254, addresses.Count);
        Assert.Equal("192.168.1.1", addresses.First().ToString());
        Assert.Equal("192.168.1.254", addresses.Last().ToString());
    }

    #endregion
}
