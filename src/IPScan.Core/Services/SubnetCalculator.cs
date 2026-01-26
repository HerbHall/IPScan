using System.Net;
using System.Net.Sockets;

namespace IPScan.Core.Services;

/// <summary>
/// Service for subnet calculations.
/// </summary>
public class SubnetCalculator : ISubnetCalculator
{
    /// <inheritdoc />
    public IPAddress GetNetworkAddress(IPAddress ipAddress, IPAddress subnetMask)
    {
        ValidateIPv4(ipAddress, nameof(ipAddress));
        ValidateIPv4(subnetMask, nameof(subnetMask));

        var ipBytes = ipAddress.GetAddressBytes();
        var maskBytes = subnetMask.GetAddressBytes();
        var networkBytes = new byte[4];

        for (var i = 0; i < 4; i++)
        {
            networkBytes[i] = (byte)(ipBytes[i] & maskBytes[i]);
        }

        return new IPAddress(networkBytes);
    }

    /// <inheritdoc />
    public IPAddress GetBroadcastAddress(IPAddress ipAddress, IPAddress subnetMask)
    {
        ValidateIPv4(ipAddress, nameof(ipAddress));
        ValidateIPv4(subnetMask, nameof(subnetMask));

        var ipBytes = ipAddress.GetAddressBytes();
        var maskBytes = subnetMask.GetAddressBytes();
        var broadcastBytes = new byte[4];

        for (var i = 0; i < 4; i++)
        {
            broadcastBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
        }

        return new IPAddress(broadcastBytes);
    }

    /// <inheritdoc />
    public IEnumerable<IPAddress> GetHostAddresses(IPAddress ipAddress, IPAddress subnetMask)
    {
        var network = GetNetworkAddress(ipAddress, subnetMask);
        var broadcast = GetBroadcastAddress(ipAddress, subnetMask);

        var networkValue = IpToUint(network);
        var broadcastValue = IpToUint(broadcast);

        // Skip network address (first) and broadcast address (last)
        for (var i = networkValue + 1; i < broadcastValue; i++)
        {
            yield return UintToIp(i);
        }
    }

    /// <inheritdoc />
    public int GetHostCount(IPAddress subnetMask)
    {
        ValidateIPv4(subnetMask, nameof(subnetMask));

        var prefixLength = GetCidrPrefixLength(subnetMask);
        var hostBits = 32 - prefixLength;

        if (hostBits <= 1)
            return 0; // /31 or /32 has no usable hosts in traditional subnetting

        return (1 << hostBits) - 2; // 2^hostBits - 2 (network and broadcast)
    }

    /// <inheritdoc />
    public int GetCidrPrefixLength(IPAddress subnetMask)
    {
        ValidateIPv4(subnetMask, nameof(subnetMask));

        var maskBytes = subnetMask.GetAddressBytes();
        var prefixLength = 0;

        foreach (var b in maskBytes)
        {
            // Count leading 1s
            for (var i = 7; i >= 0; i--)
            {
                if ((b & (1 << i)) != 0)
                    prefixLength++;
                else
                    return prefixLength;
            }
        }

        return prefixLength;
    }

    /// <inheritdoc />
    public IPAddress GetSubnetMaskFromCidr(int prefixLength)
    {
        if (prefixLength < 0 || prefixLength > 32)
            throw new ArgumentOutOfRangeException(nameof(prefixLength), "Prefix length must be between 0 and 32");

        if (prefixLength == 0)
            return new IPAddress(new byte[] { 0, 0, 0, 0 });

        var mask = uint.MaxValue << (32 - prefixLength);
        var bytes = new byte[4];
        bytes[0] = (byte)(mask >> 24);
        bytes[1] = (byte)(mask >> 16);
        bytes[2] = (byte)(mask >> 8);
        bytes[3] = (byte)mask;

        return new IPAddress(bytes);
    }

    /// <inheritdoc />
    public (IPAddress Network, int PrefixLength)? ParseCidr(string cidr)
    {
        if (string.IsNullOrWhiteSpace(cidr))
            return null;

        var parts = cidr.Split('/');
        if (parts.Length != 2)
            return null;

        if (!IPAddress.TryParse(parts[0], out var ip))
            return null;

        if (ip.AddressFamily != AddressFamily.InterNetwork)
            return null;

        if (!int.TryParse(parts[1], out var prefixLength))
            return null;

        if (prefixLength < 0 || prefixLength > 32)
            return null;

        // Normalize to network address
        var mask = GetSubnetMaskFromCidr(prefixLength);
        var network = GetNetworkAddress(ip, mask);

        return (network, prefixLength);
    }

    /// <inheritdoc />
    public string GetCidrNotation(IPAddress ipAddress, IPAddress subnetMask)
    {
        var network = GetNetworkAddress(ipAddress, subnetMask);
        var prefixLength = GetCidrPrefixLength(subnetMask);
        return $"{network}/{prefixLength}";
    }

    private static void ValidateIPv4(IPAddress address, string paramName)
    {
        if (address.AddressFamily != AddressFamily.InterNetwork)
            throw new ArgumentException("Only IPv4 addresses are supported", paramName);
    }

    private static uint IpToUint(IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();
        return ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
    }

    private static IPAddress UintToIp(uint value)
    {
        var bytes = new byte[4];
        bytes[0] = (byte)(value >> 24);
        bytes[1] = (byte)(value >> 16);
        bytes[2] = (byte)(value >> 8);
        bytes[3] = (byte)value;
        return new IPAddress(bytes);
    }
}
