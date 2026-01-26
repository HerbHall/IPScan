using IPScan.Core.Services;
using Microsoft.Extensions.Logging;

namespace IPScan.CLI;

/// <summary>
/// Test program to verify network interface detection and VPN filtering.
/// Usage: Add "test-interfaces" command to Program.cs or run this method from Main.
/// </summary>
public static class TestInterfaceDetection
{
    public static void Run()
    {
        Console.WriteLine("=== Network Interface Detection Test ===");
        Console.WriteLine();

        // Create logger (simple console logger)
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        var logger = loggerFactory.CreateLogger<NetworkInterfaceService>();

        // Create service
        var service = new NetworkInterfaceService(logger);

        // Test 1: Get all interfaces
        Console.WriteLine("1. ALL NETWORK INTERFACES:");
        Console.WriteLine(new string('-', 80));
        var allInterfaces = service.GetAllInterfaces();
        if (allInterfaces.Count == 0)
        {
            Console.WriteLine("  No interfaces found!");
        }
        else
        {
            foreach (var iface in allInterfaces)
            {
                PrintInterface(iface, "  ");
                Console.WriteLine();
            }
        }

        // Test 2: Get active interfaces (excludes VPN, loopback, down)
        Console.WriteLine();
        Console.WriteLine("2. ACTIVE INTERFACES (Excludes VPN, Loopback, Down):");
        Console.WriteLine(new string('-', 80));
        var activeInterfaces = service.GetActiveInterfaces();
        if (activeInterfaces.Count == 0)
        {
            Console.WriteLine("  No active interfaces found!");
        }
        else
        {
            foreach (var iface in activeInterfaces)
            {
                PrintInterface(iface, "  ");
                Console.WriteLine();
            }
        }

        // Test 3: Get default interface (the one we'll use for scanning)
        Console.WriteLine();
        Console.WriteLine("3. DEFAULT INTERFACE (Primary for Scanning):");
        Console.WriteLine(new string('-', 80));
        var defaultInterface = service.GetDefaultInterface();
        if (defaultInterface == null)
        {
            Console.WriteLine("  No default interface found!");
        }
        else
        {
            PrintInterface(defaultInterface, "  ");
        }

        // Test 4: Identify VPN interfaces
        Console.WriteLine();
        Console.WriteLine("4. VPN INTERFACES DETECTED:");
        Console.WriteLine(new string('-', 80));
        var vpnInterfaces = allInterfaces.Where(i => i.IsVpn).ToList();
        if (vpnInterfaces.Count == 0)
        {
            Console.WriteLine("  No VPN interfaces detected.");
        }
        else
        {
            foreach (var iface in vpnInterfaces)
            {
                PrintInterface(iface, "  ");
                Console.WriteLine();
            }
        }

        // Summary
        Console.WriteLine();
        Console.WriteLine("=== SUMMARY ===");
        Console.WriteLine($"Total interfaces: {allInterfaces.Count}");
        Console.WriteLine($"Active interfaces (non-VPN): {activeInterfaces.Count}");
        Console.WriteLine($"VPN interfaces: {vpnInterfaces.Count}");
        Console.WriteLine($"Default interface: {defaultInterface?.Name ?? "None"}");

        if (defaultInterface != null)
        {
            Console.WriteLine();
            Console.WriteLine("✓ Network interface detection successful!");
            Console.WriteLine($"  Will scan using: {defaultInterface.Name} ({defaultInterface.IpAddress})");
            if (!string.IsNullOrEmpty(defaultInterface.Gateway))
            {
                Console.WriteLine($"  Default gateway: {defaultInterface.Gateway}");
            }
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine("✗ WARNING: No default interface found!");
            Console.WriteLine("  Network scanning will not be possible.");
        }
    }

    private static void PrintInterface(Core.Models.NetworkInterfaceInfo iface, string indent)
    {
        Console.WriteLine($"{indent}Name: {iface.Name}");
        Console.WriteLine($"{indent}Description: {iface.Description}");
        Console.WriteLine($"{indent}Type: {iface.InterfaceType}{(iface.IsVpn ? " [VPN]" : "")}");
        Console.WriteLine($"{indent}Status: {(iface.IsUp ? "Up" : "Down")}");
        Console.WriteLine($"{indent}IP Address: {iface.IpAddress}");
        Console.WriteLine($"{indent}Subnet Mask: {iface.SubnetMask}");
        Console.WriteLine($"{indent}Gateway: {iface.Gateway ?? "(none)"}");
        Console.WriteLine($"{indent}MAC Address: {iface.MacAddress}");
        Console.WriteLine($"{indent}Speed: {FormatSpeed(iface.Speed)}");
        Console.WriteLine($"{indent}ID: {iface.Id}");
    }

    private static string FormatSpeed(long speed)
    {
        if (speed <= 0)
            return "Unknown";
        if (speed >= 1_000_000_000)
            return $"{speed / 1_000_000_000.0:F1} Gbps";
        if (speed >= 1_000_000)
            return $"{speed / 1_000_000.0:F0} Mbps";
        if (speed >= 1_000)
            return $"{speed / 1_000.0:F0} Kbps";
        return $"{speed} bps";
    }
}
