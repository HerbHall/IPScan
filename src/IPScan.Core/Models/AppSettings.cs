namespace IPScan.Core.Models;

/// <summary>
/// Application settings that persist between sessions.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Whether to automatically scan on startup.
    /// </summary>
    public bool ScanOnStartup { get; set; } = true;

    /// <summary>
    /// Subnet mode: "auto" to detect from interface, or custom CIDR.
    /// </summary>
    public string Subnet { get; set; } = "auto";

    /// <summary>
    /// Custom subnet in CIDR notation (used when Subnet != "auto").
    /// </summary>
    public string CustomSubnet { get; set; } = string.Empty;

    /// <summary>
    /// ID of the preferred network interface, or empty for auto-select.
    /// </summary>
    public string PreferredInterfaceId { get; set; } = string.Empty;

    /// <summary>
    /// Timeout in milliseconds for ping operations.
    /// </summary>
    public int ScanTimeoutMs { get; set; } = 1000;

    /// <summary>
    /// Maximum number of concurrent ping operations.
    /// </summary>
    public int MaxConcurrentScans { get; set; } = 100;

    /// <summary>
    /// Whether to automatically remove devices that are missing from scans.
    /// </summary>
    public bool AutoRemoveMissingDevices { get; set; }

    /// <summary>
    /// Number of consecutive missed scans before auto-removing a device.
    /// Only used if AutoRemoveMissingDevices is true.
    /// </summary>
    public int MissedScansBeforeRemoval { get; set; } = 5;

    /// <summary>
    /// Whether to show offline devices in the UI.
    /// </summary>
    public bool ShowOfflineDevices { get; set; } = true;

    /// <summary>
    /// Splash screen timeout in seconds.
    /// </summary>
    public int SplashTimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Window startup behavior.
    /// </summary>
    public WindowStartupMode WindowStartup { get; set; } = WindowStartupMode.RememberLast;

    /// <summary>
    /// Preferred monitor device name (e.g., "\\.\DISPLAY1").
    /// Empty string means use primary monitor.
    /// </summary>
    public string PreferredMonitor { get; set; } = string.Empty;

    /// <summary>
    /// Saved window state from last session.
    /// </summary>
    public WindowSettings? LastWindowSettings { get; set; }

    /// <summary>
    /// Theme mode for the application.
    /// </summary>
    public ThemeMode ThemeMode { get; set; } = ThemeMode.CrtGreen;

    /// <summary>
    /// Accent color mode for the application.
    /// </summary>
    public AccentColorMode AccentColorMode { get; set; } = AccentColorMode.System;

    /// <summary>
    /// Custom accent color in hex format (e.g., "#00FF00").
    /// </summary>
    public string CustomAccentColor { get; set; } = "#00FF00";
}

/// <summary>
/// Window startup behavior options.
/// </summary>
public enum WindowStartupMode
{
    /// <summary>
    /// Remember last window position, size, and state.
    /// </summary>
    RememberLast,

    /// <summary>
    /// Always start maximized.
    /// </summary>
    AlwaysMaximized,

    /// <summary>
    /// Always start centered with default size.
    /// </summary>
    DefaultCentered,

    /// <summary>
    /// Start on a specific monitor (uses PreferredMonitor setting).
    /// </summary>
    SpecificMonitor
}

/// <summary>
/// Saved window position and state.
/// </summary>
public class WindowSettings
{
    /// <summary>
    /// Window left position.
    /// </summary>
    public double Left { get; set; }

    /// <summary>
    /// Window top position.
    /// </summary>
    public double Top { get; set; }

    /// <summary>
    /// Window width.
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Window height.
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// Whether window was maximized.
    /// </summary>
    public bool IsMaximized { get; set; }

    /// <summary>
    /// Monitor device name where window was displayed.
    /// </summary>
    public string MonitorDeviceName { get; set; } = string.Empty;
}

/// <summary>
/// Theme mode options for the application.
/// </summary>
public enum ThemeMode
{
    /// <summary>
    /// Follow Windows system theme (light or dark).
    /// </summary>
    WindowsSystem,

    /// <summary>
    /// Light theme.
    /// </summary>
    Light,

    /// <summary>
    /// Dark theme.
    /// </summary>
    Dark,

    /// <summary>
    /// Iconic CRT green terminal theme (default).
    /// </summary>
    CrtGreen
}

/// <summary>
/// Accent color mode options for the application.
/// </summary>
public enum AccentColorMode
{
    /// <summary>
    /// Use iconic CRT green color.
    /// </summary>
    CrtGreen,

    /// <summary>
    /// Follow Windows system accent color (default).
    /// </summary>
    System,

    /// <summary>
    /// Use custom user-defined color.
    /// </summary>
    Custom
}
