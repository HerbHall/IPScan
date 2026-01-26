using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using IPScan.Core.Models;
using Microsoft.Extensions.Logging;

namespace IPScan.Core.Services;

/// <summary>
/// Service for scanning networks to discover devices using ping sweep.
/// </summary>
public class NetworkScanner : INetworkScanner
{
    private readonly ISubnetCalculator _subnetCalculator;
    private readonly ILogger<NetworkScanner> _logger;

    public event EventHandler<DiscoveredDevice>? DeviceDiscovered;
    public event EventHandler<ScanProgressEventArgs>? ProgressChanged;

    public NetworkScanner(ISubnetCalculator subnetCalculator, ILogger<NetworkScanner> logger)
    {
        _subnetCalculator = subnetCalculator;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ScanResult> ScanAsync(
        NetworkInterfaceInfo networkInterface,
        IPAddress? subnetMask = null,
        int timeoutMs = 1000,
        int maxConcurrent = 100,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(networkInterface.IpAddress))
        {
            return new ScanResult
            {
                Success = false,
                ErrorMessage = "Network interface has no IP address"
            };
        }

        var ipAddress = IPAddress.Parse(networkInterface.IpAddress);
        var mask = subnetMask ?? IPAddress.Parse(networkInterface.SubnetMask);
        var cidr = _subnetCalculator.GetCidrNotation(ipAddress, mask);

        _logger.LogInformation("Starting scan on interface {Name} ({IP}), subnet {Cidr}",
            networkInterface.Name, networkInterface.IpAddress, cidr);

        return await ScanSubnetAsync(ipAddress, mask, networkInterface.Id, timeoutMs, maxConcurrent, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ScanResult> ScanCidrAsync(
        string cidr,
        int timeoutMs = 1000,
        int maxConcurrent = 100,
        CancellationToken cancellationToken = default)
    {
        var parsed = _subnetCalculator.ParseCidr(cidr);
        if (parsed == null)
        {
            return new ScanResult
            {
                Success = false,
                ErrorMessage = $"Invalid CIDR notation: {cidr}"
            };
        }

        var (network, prefixLength) = parsed.Value;
        var mask = _subnetCalculator.GetSubnetMaskFromCidr(prefixLength);

        _logger.LogInformation("Starting scan on CIDR {Cidr}", cidr);

        return await ScanSubnetAsync(network, mask, string.Empty, timeoutMs, maxConcurrent, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DiscoveredDevice?> PingAsync(IPAddress address, int timeoutMs = 1000, CancellationToken cancellationToken = default)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(address, timeoutMs);

            if (reply.Status == IPStatus.Success)
            {
                var hostname = await ResolveHostnameAsync(address, cancellationToken);

                return new DiscoveredDevice
                {
                    IpAddress = address.ToString(),
                    Hostname = hostname,
                    ResponseTimeMs = reply.RoundtripTime
                };
            }
        }
        catch (PingException ex)
        {
            _logger.LogDebug(ex, "Ping failed for {Address}", address);
        }
        catch (OperationCanceledException)
        {
            // Scan was cancelled
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error pinging {Address}", address);
        }

        return null;
    }

    private async Task<ScanResult> ScanSubnetAsync(
        IPAddress ipAddress,
        IPAddress subnetMask,
        string interfaceId,
        int timeoutMs,
        int maxConcurrent,
        CancellationToken cancellationToken)
    {
        var result = new ScanResult
        {
            Subnet = _subnetCalculator.GetCidrNotation(ipAddress, subnetMask),
            InterfaceId = interfaceId,
            StartTime = DateTime.UtcNow
        };

        try
        {
            var hostAddresses = _subnetCalculator.GetHostAddresses(ipAddress, subnetMask).ToList();
            result.TotalAddressesScanned = hostAddresses.Count;

            _logger.LogDebug("Scanning {Count} addresses with {MaxConcurrent} concurrent pings",
                hostAddresses.Count, maxConcurrent);

            var scannedCount = 0;
            var devicesFound = 0;
            var semaphore = new SemaphoreSlim(maxConcurrent);

            var tasks = hostAddresses.Select(async address =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var device = await PingAsync(address, timeoutMs, cancellationToken);

                    var currentScanned = Interlocked.Increment(ref scannedCount);

                    if (device != null)
                    {
                        var currentDevices = Interlocked.Increment(ref devicesFound);
                        lock (result.DiscoveredDevices)
                        {
                            result.DiscoveredDevices.Add(device);
                        }
                        DeviceDiscovered?.Invoke(this, device);
                    }

                    // Report progress periodically (every 10 addresses or on device found)
                    if (device != null || currentScanned % 10 == 0 || currentScanned == hostAddresses.Count)
                    {
                        ProgressChanged?.Invoke(this, new ScanProgressEventArgs
                        {
                            CurrentAddress = address,
                            ScannedCount = currentScanned,
                            TotalCount = hostAddresses.Count,
                            DevicesFound = devicesFound
                        });
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            result.Success = true;
            _logger.LogInformation("Scan completed: {Found}/{Total} devices found",
                result.DevicesFound, result.TotalAddressesScanned);
        }
        catch (OperationCanceledException)
        {
            result.Success = false;
            result.ErrorMessage = "Scan was cancelled";
            _logger.LogInformation("Scan was cancelled");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Scan failed");
        }

        result.EndTime = DateTime.UtcNow;
        return result;
    }

    private async Task<string?> ResolveHostnameAsync(IPAddress address, CancellationToken cancellationToken)
    {
        try
        {
            var hostEntry = await Dns.GetHostEntryAsync(address.ToString()).WaitAsync(cancellationToken);
            // Only return hostname if it's different from the IP (indicates real resolution)
            if (hostEntry.HostName != address.ToString())
            {
                return hostEntry.HostName;
            }
        }
        catch
        {
            // DNS resolution failed - this is common for devices without PTR records
        }

        return null;
    }
}
