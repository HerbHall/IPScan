using System.Diagnostics;

namespace IPScan.CLI;

class Program
{
    static int Main(string[] args)
    {
        // If no arguments provided, launch GUI
        if (args.Length == 0)
        {
            return LaunchGui();
        }

        var command = args[0].ToLowerInvariant();
        var remainingArgs = args.Skip(1).ToArray();

        return command switch
        {
            "scan" => HandleScan(remainingArgs),
            "list" => HandleList(remainingArgs),
            "show" => HandleShow(remainingArgs),
            "open" => HandleOpen(remainingArgs),
            "settings" => HandleSettings(remainingArgs),
            "test-interfaces" => HandleTestInterfaces(),
            "gui" => LaunchGui(),
            "--help" or "-h" or "help" => ShowHelp(),
            "--version" or "-v" => ShowVersion(),
            _ => ShowHelp(error: $"Unknown command: {command}")
        };
    }

    static int ShowHelp(string? error = null)
    {
        if (error != null)
        {
            Console.WriteLine($"Error: {error}");
            Console.WriteLine();
        }

        Console.WriteLine("IPScan - Network Device Discovery Tool");
        Console.WriteLine();
        Console.WriteLine("Usage: ipscan [command] [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  (no command)     Launch GUI application");
        Console.WriteLine("  scan             Scan network for devices");
        Console.WriteLine("  list             List discovered devices");
        Console.WriteLine("  show <device>    Show device details");
        Console.WriteLine("  open <device>    Open device in browser");
        Console.WriteLine("  settings         View or modify settings");
        Console.WriteLine("  test-interfaces  Test network interface detection (dev/debug)");
        Console.WriteLine("  gui              Launch GUI application");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --help, -h       Show this help message");
        Console.WriteLine("  --version, -v    Show version information");
        Console.WriteLine("  --gui, -g        Launch GUI after command");
        Console.WriteLine();
        Console.WriteLine("Scan options:");
        Console.WriteLine("  --subnet, -s <subnet>   Subnet to scan (e.g., 192.168.1.0/24)");
        Console.WriteLine("  --rescan, -r            Rescan all devices");
        Console.WriteLine();
        Console.WriteLine("List options:");
        Console.WriteLine("  --offline, -o    Include offline devices");
        Console.WriteLine();
        Console.WriteLine("Open options:");
        Console.WriteLine("  --port, -p <port>   Specific port to open");
        Console.WriteLine();
        Console.WriteLine("Settings commands:");
        Console.WriteLine("  settings get [key]      Get setting(s)");
        Console.WriteLine("  settings set <key> <value>  Set a setting");

        return error != null ? 1 : 0;
    }

    static int ShowVersion()
    {
        var version = typeof(Program).Assembly.GetName().Version;
        var infoVersion = typeof(Program).Assembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .FirstOrDefault()?.InformationalVersion;

        var displayVersion = !string.IsNullOrEmpty(infoVersion)
            ? infoVersion.Split('+')[0]
            : version?.ToString() ?? "0.0.0";

        Console.WriteLine($"IPScan version {displayVersion}");
        return 0;
    }

    static int LaunchGui()
    {
        try
        {
            var guiPath = Path.Combine(AppContext.BaseDirectory, "IPScan.GUI.exe");

            if (!File.Exists(guiPath))
            {
                // Try relative path for development
                var devPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "IPScan.GUI", "bin", "Debug", "net10.0-windows10.0.19041.0", "IPScan.GUI.exe"));
                if (File.Exists(devPath))
                {
                    guiPath = devPath;
                }
                else
                {
                    Console.WriteLine("GUI application not found. Run 'dotnet build' first.");
                    return 1;
                }
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = guiPath,
                UseShellExecute = true
            });

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to launch GUI: {ex.Message}");
            return 1;
        }
    }

    static int HandleScan(string[] args)
    {
        string? subnet = null;
        bool rescan = false;
        bool launchGui = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--subnet" or "-s":
                    if (i + 1 < args.Length)
                        subnet = args[++i];
                    break;
                case "--rescan" or "-r":
                    rescan = true;
                    break;
                case "--gui" or "-g":
                    launchGui = true;
                    break;
            }
        }

        Console.WriteLine($"Scanning network{(subnet != null ? $" ({subnet})" : "")}...");
        if (rescan)
        {
            Console.WriteLine("Rescan mode: updating all existing devices");
        }

        // TODO: Implement actual scanning using IPScan.Core
        Console.WriteLine("[Scanning not yet implemented - Core services pending]");

        if (launchGui) LaunchGui();
        return 0;
    }

    static int HandleList(string[] args)
    {
        bool includeOffline = args.Contains("--offline") || args.Contains("-o");
        bool launchGui = args.Contains("--gui") || args.Contains("-g");

        Console.WriteLine("Discovered Devices:");
        Console.WriteLine("-------------------");

        if (includeOffline)
        {
            Console.WriteLine("(Including offline devices)");
        }

        // TODO: Implement device listing from IPScan.Core
        Console.WriteLine("[Device listing not yet implemented - Core services pending]");

        if (launchGui) LaunchGui();
        return 0;
    }

    static int HandleShow(string[] args)
    {
        var device = args.FirstOrDefault(a => !a.StartsWith("-"));
        bool launchGui = args.Contains("--gui") || args.Contains("-g");

        if (string.IsNullOrEmpty(device))
        {
            Console.WriteLine("Error: Device name or IP required");
            return 1;
        }

        Console.WriteLine($"Device Details: {device}");
        Console.WriteLine("------------------------");

        // TODO: Implement device details from IPScan.Core
        Console.WriteLine("[Device details not yet implemented - Core services pending]");

        if (launchGui) LaunchGui();
        return 0;
    }

    static int HandleOpen(string[] args)
    {
        var device = args.FirstOrDefault(a => !a.StartsWith("-"));
        bool launchGui = args.Contains("--gui") || args.Contains("-g");
        int? port = null;

        for (int i = 0; i < args.Length; i++)
        {
            if ((args[i] == "--port" || args[i] == "-p") && i + 1 < args.Length)
            {
                if (int.TryParse(args[i + 1], out var p))
                    port = p;
            }
        }

        if (string.IsNullOrEmpty(device))
        {
            Console.WriteLine("Error: Device name or IP required");
            return 1;
        }

        Console.WriteLine($"Opening {device}{(port.HasValue ? $":{port}" : "")} in browser...");

        // TODO: Implement actual device opening from IPScan.Core
        Console.WriteLine("[Device opening not yet implemented - Core services pending]");

        if (launchGui) LaunchGui();
        return 0;
    }

    static int HandleSettings(string[] args)
    {
        if (args.Length == 0)
        {
            return HandleSettingsGet(null);
        }

        var subcommand = args[0].ToLowerInvariant();

        return subcommand switch
        {
            "get" => HandleSettingsGet(args.ElementAtOrDefault(1)),
            "set" => HandleSettingsSet(args.ElementAtOrDefault(1), args.ElementAtOrDefault(2)),
            _ => HandleSettingsGet(subcommand) // Treat as key name
        };
    }

    static int HandleSettingsGet(string? key)
    {
        if (string.IsNullOrEmpty(key))
        {
            Console.WriteLine("Current Settings:");
            Console.WriteLine("-----------------");
            Console.WriteLine("scanOnStartup: true");
            Console.WriteLine("subnet: auto");
            Console.WriteLine("scanTimeoutMs: 1000");
            Console.WriteLine("splashTimeoutSeconds: 5");
            Console.WriteLine("theme.mode: system");
            Console.WriteLine("theme.accentColor: system");
            // TODO: Read actual settings from IPScan.Core
        }
        else
        {
            Console.WriteLine($"{key}: [value]");
            // TODO: Read actual setting from IPScan.Core
        }
        return 0;
    }

    static int HandleSettingsSet(string? key, string? value)
    {
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
        {
            Console.WriteLine("Error: Both key and value are required");
            Console.WriteLine("Usage: ipscan settings set <key> <value>");
            return 1;
        }

        Console.WriteLine($"Setting {key} = {value}");
        // TODO: Write setting using IPScan.Core
        Console.WriteLine("[Settings update not yet implemented - Core services pending]");
        return 0;
    }

    static int HandleTestInterfaces()
    {
        try
        {
            TestInterfaceDetection.Run();
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error testing interfaces: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
    }
}
