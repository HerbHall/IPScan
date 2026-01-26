using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using IPScan.Core.Models;
using IPScan.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Win32;
using Windows.UI.ViewManagement;
using WinForms = System.Windows.Forms;

// Disambiguate WPF types from WinForms types
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;
using Orientation = System.Windows.Controls.Orientation;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace IPScan.GUI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly UISettings _uiSettings;
    private bool _isDarkMode;

    // Core services
    private readonly IDeviceManager _deviceManager;
    private readonly ISettingsService _settingsService;
    private CancellationTokenSource? _scanCancellationTokenSource;
    private bool _isScanning;

    // Device collection for UI binding
    private readonly List<Device> _devices = new();
    private readonly object _devicesLock = new();

    public MainWindow()
    {
        InitializeComponent();

        // Initialize Windows UI settings for theme detection
        _uiSettings = new UISettings();
        _uiSettings.ColorValuesChanged += UiSettings_ColorValuesChanged;

        // Apply initial theme
        DetectAndApplyTheme();

        // Initialize Core services
        var loggerFactory = NullLoggerFactory.Instance;

        var subnetCalculator = new SubnetCalculator();
        var networkInterfaceService = new NetworkInterfaceService(loggerFactory.CreateLogger<NetworkInterfaceService>());
        var networkScanner = new NetworkScanner(subnetCalculator, loggerFactory.CreateLogger<NetworkScanner>());
        var deviceRepository = new JsonDeviceRepository(loggerFactory.CreateLogger<JsonDeviceRepository>());
        _settingsService = new JsonSettingsService(loggerFactory.CreateLogger<JsonSettingsService>());

        _deviceManager = new DeviceManager(
            networkScanner,
            networkInterfaceService,
            deviceRepository,
            _settingsService,
            subnetCalculator,
            loggerFactory.CreateLogger<DeviceManager>());

        // Subscribe to DeviceManager events
        _deviceManager.ScanStarted += DeviceManager_ScanStarted;
        _deviceManager.ScanProgress += DeviceManager_ScanProgress;
        _deviceManager.ScanCompleted += DeviceManager_ScanCompleted;
        _deviceManager.DeviceDiscovered += DeviceManager_DeviceDiscovered;
        _deviceManager.DeviceUpdated += DeviceManager_DeviceUpdated;

        // Apply window settings before showing
        ApplyWindowSettings();

        // Save window state on close
        Closing += MainWindow_Closing;
    }

    #region Window State Management

    private void ApplyWindowSettings()
    {
        // Load settings synchronously for initial window positioning
        var settings = _settingsService.GetSettingsAsync().GetAwaiter().GetResult();

        switch (settings.WindowStartup)
        {
            case WindowStartupMode.AlwaysMaximized:
                WindowState = WindowState.Maximized;
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
                break;

            case WindowStartupMode.DefaultCentered:
                ApplySmartDefaultSize();
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
                break;

            case WindowStartupMode.SpecificMonitor:
                ApplySmartDefaultSize();
                PositionOnMonitor(settings.PreferredMonitor, settings.LastWindowSettings);
                break;

            case WindowStartupMode.RememberLast:
            default:
                if (settings.LastWindowSettings != null)
                {
                    RestoreWindowSettings(settings.LastWindowSettings);
                }
                else
                {
                    ApplySmartDefaultSize();
                    WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
                break;
        }
    }

    private void ApplySmartDefaultSize()
    {
        // Get primary screen dimensions
        var primaryScreen = WinForms.Screen.PrimaryScreen;
        if (primaryScreen == null) return;

        var workingArea = primaryScreen.WorkingArea;

        // Set window to 80% of screen size, but respect min/max constraints
        var targetWidth = Math.Max(MinWidth, Math.Min(1400, workingArea.Width * 0.8));
        var targetHeight = Math.Max(MinHeight, Math.Min(900, workingArea.Height * 0.8));

        Width = targetWidth;
        Height = targetHeight;
    }

    private void RestoreWindowSettings(WindowSettings windowSettings)
    {
        // First, check if the saved monitor is still available
        var targetScreen = FindMonitorByDeviceName(windowSettings.MonitorDeviceName);

        if (targetScreen == null)
        {
            // Fallback to primary monitor
            targetScreen = WinForms.Screen.PrimaryScreen;
        }

        if (targetScreen == null) return;

        // Restore size
        Width = Math.Max(MinWidth, windowSettings.Width);
        Height = Math.Max(MinHeight, windowSettings.Height);

        // Check if the saved position is still valid (window is visible on a monitor)
        var windowRect = new System.Drawing.Rectangle(
            (int)windowSettings.Left,
            (int)windowSettings.Top,
            (int)Width,
            (int)Height);

        if (IsWindowVisibleOnAnyScreen(windowRect))
        {
            // Position is valid, use it
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = windowSettings.Left;
            Top = windowSettings.Top;
        }
        else
        {
            // Position is off-screen, center on target monitor
            CenterOnScreen(targetScreen);
        }

        // Restore maximized state after positioning
        if (windowSettings.IsMaximized)
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void PositionOnMonitor(string monitorDeviceName, WindowSettings? lastSettings)
    {
        var targetScreen = FindMonitorByDeviceName(monitorDeviceName);

        if (targetScreen == null)
        {
            // Fallback to primary monitor
            targetScreen = WinForms.Screen.PrimaryScreen;
        }

        if (targetScreen == null) return;

        // If we have last settings for this monitor, use them
        if (lastSettings != null && lastSettings.MonitorDeviceName == monitorDeviceName)
        {
            RestoreWindowSettings(lastSettings);
        }
        else
        {
            // Center on the specified monitor
            CenterOnScreen(targetScreen);
        }
    }

    private void CenterOnScreen(WinForms.Screen screen)
    {
        WindowStartupLocation = WindowStartupLocation.Manual;
        var workingArea = screen.WorkingArea;

        Left = workingArea.Left + (workingArea.Width - Width) / 2;
        Top = workingArea.Top + (workingArea.Height - Height) / 2;
    }

    private static WinForms.Screen? FindMonitorByDeviceName(string deviceName)
    {
        if (string.IsNullOrEmpty(deviceName))
            return null;

        return WinForms.Screen.AllScreens.FirstOrDefault(s => s.DeviceName == deviceName);
    }

    private static bool IsWindowVisibleOnAnyScreen(System.Drawing.Rectangle windowRect)
    {
        // Check if at least 100x100 pixels of the window are visible on any screen
        foreach (var screen in WinForms.Screen.AllScreens)
        {
            var intersection = System.Drawing.Rectangle.Intersect(screen.WorkingArea, windowRect);
            if (intersection.Width >= 100 && intersection.Height >= 100)
            {
                return true;
            }
        }
        return false;
    }

    private string GetCurrentMonitorDeviceName()
    {
        // Get the monitor where the window is currently displayed
        var windowInteropHelper = new WindowInteropHelper(this);
        var hMonitor = NativeMethods.MonitorFromWindow(windowInteropHelper.Handle, NativeMethods.MONITOR_DEFAULTTONEAREST);

        foreach (var screen in WinForms.Screen.AllScreens)
        {
            // Compare by checking if the screen contains the window center
            var windowCenter = new System.Drawing.Point(
                (int)(Left + Width / 2),
                (int)(Top + Height / 2));

            if (screen.Bounds.Contains(windowCenter))
            {
                return screen.DeviceName;
            }
        }

        return WinForms.Screen.PrimaryScreen?.DeviceName ?? string.Empty;
    }

    private async void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Save window state
        var settings = await _settingsService.GetSettingsAsync();

        // Capture state before window closes
        var windowSettings = new WindowSettings
        {
            IsMaximized = WindowState == WindowState.Maximized,
            MonitorDeviceName = GetCurrentMonitorDeviceName()
        };

        // If maximized, save the restore bounds instead of current bounds
        if (WindowState == WindowState.Maximized)
        {
            windowSettings.Left = RestoreBounds.Left;
            windowSettings.Top = RestoreBounds.Top;
            windowSettings.Width = RestoreBounds.Width;
            windowSettings.Height = RestoreBounds.Height;
        }
        else
        {
            windowSettings.Left = Left;
            windowSettings.Top = Top;
            windowSettings.Width = Width;
            windowSettings.Height = Height;
        }

        settings.LastWindowSettings = windowSettings;

        await _settingsService.SaveSettingsAsync(settings);
    }

    private static class NativeMethods
    {
        public const int MONITOR_DEFAULTTONEAREST = 2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);
    }

    #endregion

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Load existing devices from storage
        await LoadDevicesAsync();

        // Check if auto-scan is enabled
        var settings = await _settingsService.GetSettingsAsync();
        if (settings.ScanOnStartup)
        {
            await StartScanAsync();
        }
        else
        {
            StatusText.Text = "Ready - Click Scan to discover devices";
        }
    }

    private async Task LoadDevicesAsync()
    {
        try
        {
            var devices = await _deviceManager.GetAllDevicesAsync();
            lock (_devicesLock)
            {
                _devices.Clear();
                foreach (var device in devices)
                {
                    _devices.Add(device);
                }
            }
            RefreshDeviceTreeView();
            UpdateStatusCounts();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error loading devices: {ex.Message}";
        }
    }

    #region Theme Support

    private void DetectAndApplyTheme()
    {
        // Check Windows dark mode setting
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            _isDarkMode = value is int intValue && intValue == 0;
        }
        catch
        {
            _isDarkMode = false;
        }

        // Get Windows accent color
        var accentColor = _uiSettings.GetColorValue(UIColorType.Accent);
        var wpfAccentColor = Color.FromArgb(accentColor.A, accentColor.R, accentColor.G, accentColor.B);

        ApplyTheme(wpfAccentColor);
    }

    private void UiSettings_ColorValuesChanged(UISettings sender, object args)
    {
        Dispatcher.Invoke(() =>
        {
            DetectAndApplyTheme();
        });
    }

    private void ApplyTheme(Color accentColor)
    {
        var accentBrush = new SolidColorBrush(accentColor);

        // Calculate hover and pressed colors
        var hoverColor = Color.FromArgb(255,
            (byte)Math.Max(0, accentColor.R - 20),
            (byte)Math.Max(0, accentColor.G - 20),
            (byte)Math.Max(0, accentColor.B - 20));
        var pressedColor = Color.FromArgb(255,
            (byte)Math.Max(0, accentColor.R - 40),
            (byte)Math.Max(0, accentColor.G - 40),
            (byte)Math.Max(0, accentColor.B - 40));

        if (_isDarkMode)
        {
            // Dark theme colors
            Resources["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            Resources["PanelBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 45));
            Resources["PanelBorderBrush"] = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            Resources["MenuBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 45));
            Resources["MenuBorderBrush"] = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            Resources["ToolbarBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(40, 40, 40));
            Resources["TitleBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            Resources["TextBrush"] = new SolidColorBrush(Color.FromRgb(230, 230, 230));
            Resources["SecondaryTextBrush"] = new SolidColorBrush(Color.FromRgb(180, 180, 180));
            Resources["DisabledTextBrush"] = new SolidColorBrush(Color.FromRgb(120, 120, 120));
            Resources["SeparatorBrush"] = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            Resources["TreeViewSelectedBrush"] = new SolidColorBrush(Color.FromRgb(60, 60, 60));
        }
        else
        {
            // Light theme colors (defaults from XAML)
            Resources["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(243, 243, 243));
            Resources["PanelBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            Resources["PanelBorderBrush"] = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            Resources["MenuBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            Resources["MenuBorderBrush"] = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            Resources["ToolbarBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(250, 250, 250));
            Resources["TitleBrush"] = new SolidColorBrush(Color.FromRgb(26, 26, 26));
            Resources["TextBrush"] = new SolidColorBrush(Color.FromRgb(51, 51, 51));
            Resources["SecondaryTextBrush"] = new SolidColorBrush(Color.FromRgb(102, 102, 102));
            Resources["DisabledTextBrush"] = new SolidColorBrush(Color.FromRgb(153, 153, 153));
            Resources["SeparatorBrush"] = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            Resources["TreeViewSelectedBrush"] = new SolidColorBrush(Color.FromRgb(204, 228, 247));
        }

        // Apply accent colors
        Resources["AccentBrush"] = accentBrush;
        Resources["AccentHoverBrush"] = new SolidColorBrush(hoverColor);
        Resources["AccentPressedBrush"] = new SolidColorBrush(pressedColor);
        Resources["LinkBrush"] = accentBrush;
        Resources["StatusBarBackgroundBrush"] = accentBrush;
    }

    private void ApplyThemeToWindow(Window window)
    {
        // Copy current theme resources to child window
        if (_isDarkMode)
        {
            // Dark theme
            window.Resources["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            window.Resources["PanelBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 45));
            window.Resources["PanelBorderBrush"] = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            window.Resources["TitleBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            window.Resources["TextBrush"] = new SolidColorBrush(Color.FromRgb(230, 230, 230));
            window.Resources["SecondaryTextBrush"] = new SolidColorBrush(Color.FromRgb(180, 180, 180));
        }
        else
        {
            // Light theme
            window.Resources["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(243, 243, 243));
            window.Resources["PanelBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            window.Resources["PanelBorderBrush"] = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            window.Resources["TitleBrush"] = new SolidColorBrush(Color.FromRgb(26, 26, 26));
            window.Resources["TextBrush"] = new SolidColorBrush(Color.FromRgb(51, 51, 51));
            window.Resources["SecondaryTextBrush"] = new SolidColorBrush(Color.FromRgb(102, 102, 102));
        }

        // Apply accent color
        window.Resources["AccentBrush"] = Resources["AccentBrush"];
    }

    #endregion

    #region Menu Event Handlers

    private async void ScanNetwork_Click(object sender, RoutedEventArgs e)
    {
        await StartScanAsync();
    }

    private async Task StartScanAsync()
    {
        if (_isScanning)
        {
            // Cancel current scan
            _scanCancellationTokenSource?.Cancel();
            return;
        }

        _isScanning = true;
        _scanCancellationTokenSource = new CancellationTokenSource();

        try
        {
            var result = await _deviceManager.ScanAsync(cancellationToken: _scanCancellationTokenSource.Token);

            if (!result.Success)
            {
                MessageBox.Show($"Scan failed: {result.ErrorMessage}",
                    "Scan Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Scan cancelled";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Scan error: {ex.Message}",
                "Scan Error", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Scan failed";
        }
        finally
        {
            _isScanning = false;
            _scanCancellationTokenSource?.Dispose();
            _scanCancellationTokenSource = null;
        }
    }

    private async void RescanAll_Click(object sender, RoutedEventArgs e)
    {
        await StartScanAsync();
    }

    private async void ExportDevices_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = ".json",
            FileName = $"ipscan_devices_{DateTime.Now:yyyyMMdd_HHmmss}.json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var devices = await _deviceManager.GetAllDevicesAsync();
                var json = System.Text.Json.JsonSerializer.Serialize(devices, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await System.IO.File.WriteAllTextAsync(dialog.FileName, json);

                MessageBox.Show($"Exported {devices.Count} device(s) to {System.IO.Path.GetFileName(dialog.FileName)}",
                    "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                StatusText.Text = $"Exported {devices.Count} devices";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting devices: {ex.Message}",
                    "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void ImportDevices_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = ".json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var json = await System.IO.File.ReadAllTextAsync(dialog.FileName);
                var devices = System.Text.Json.JsonSerializer.Deserialize<List<Device>>(json);

                if (devices == null || devices.Count == 0)
                {
                    MessageBox.Show("No devices found in the selected file.",
                        "Import", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Ask for confirmation
                var result = MessageBox.Show(
                    $"Found {devices.Count} device(s) in the file.\n\n" +
                    "This will add them to your existing devices (duplicates by IP will be skipped).\n\n" +
                    "Continue with import?",
                    "Confirm Import",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    int imported = 0;
                    int skipped = 0;

                    foreach (var device in devices)
                    {
                        // Check if device already exists by IP
                        var existing = _devices.FirstOrDefault(d => d.IpAddress == device.IpAddress);
                        if (existing == null)
                        {
                            await _deviceManager.AddDeviceAsync(device);
                            imported++;
                        }
                        else
                        {
                            skipped++;
                        }
                    }

                    await LoadDevicesAsync();

                    MessageBox.Show($"Import complete:\n\n" +
                        $"Imported: {imported}\n" +
                        $"Skipped (duplicates): {skipped}",
                        "Import Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                    StatusText.Text = $"Imported {imported} devices";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing devices: {ex.Message}",
                    "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void Settings_Click(object sender, RoutedEventArgs e)
    {
        var settings = await _settingsService.GetSettingsAsync();
        var dialog = new SettingsWindow(_settingsService, settings)
        {
            Owner = this
        };

        // Apply current theme to dialog
        ApplyThemeToWindow(dialog);

        if (dialog.ShowDialog() == true)
        {
            // Settings were saved, reload them
            var updatedSettings = await _settingsService.GetSettingsAsync();
            ShowOfflineMenuItem.IsChecked = updatedSettings.ShowOfflineDevices;
            RefreshDeviceTreeView();
            StatusText.Text = "Settings saved successfully";
        }
    }

    private void ToggleOffline_Click(object sender, RoutedEventArgs e)
    {
        var showOffline = ShowOfflineMenuItem.IsChecked;
        StatusText.Text = showOffline ? "Showing all devices" : "Hiding offline devices";
        RefreshDeviceTreeView();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Refreshing device list...";
        await LoadDevicesAsync();
        StatusText.Text = "Ready";
    }

    private void Help_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Open help documentation
        MessageBox.Show("IPScan Help\n\n" +
            "Scan: Discover new devices on your network\n" +
            "Rescan All: Update information for known devices\n" +
            "Device List: Click a device to view details\n" +
            "Open Ports: Click to open device web interfaces\n\n" +
            "For more information, visit the GitHub repository.",
            "Help", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void GitHub_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/HerbHall/IPScan",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open browser: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var version = typeof(MainWindow).Assembly.GetName().Version;
        var infoVersion = typeof(MainWindow).Assembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .FirstOrDefault()?.InformationalVersion;

        var displayVersion = !string.IsNullOrEmpty(infoVersion)
            ? infoVersion.Split('+')[0]
            : version?.ToString() ?? "0.0.0";

        MessageBox.Show($"IPScan\nVersion {displayVersion}\n\n" +
            "Network Device Discovery Tool\n\n" +
            "Author: Herb Hall\n" +
            "Email: herbhall21@gmail.com\n" +
            "License: MIT\n\n" +
            "https://github.com/HerbHall/IPScan",
            "About IPScan", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    #endregion

    #region DeviceManager Event Handlers

    private void DeviceManager_ScanStarted(object? sender, ScanStartedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            StatusText.Text = $"Scanning {e.Subnet} ({e.TotalAddresses} addresses)...";
            SubnetText.Text = $"Subnet: {e.Subnet}";
        });
    }

    private void DeviceManager_ScanProgress(object? sender, ScanProgressEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            var percent = (int)((double)e.ScannedCount / e.TotalCount * 100);
            StatusText.Text = $"Scanning... {percent}% ({e.DevicesFound} devices found)";
        });
    }

    private void DeviceManager_ScanCompleted(object? sender, ScanCompletedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            var duration = e.Result.Duration.TotalSeconds;
            StatusText.Text = $"Scan complete: {e.Result.DevicesFound} devices found in {duration:F1}s " +
                             $"({e.NewDevicesFound} new, {e.DevicesUpdated} updated)";
            RefreshDeviceTreeView();
            UpdateStatusCounts();
        });
    }

    private void DeviceManager_DeviceDiscovered(object? sender, DeviceEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            lock (_devicesLock)
            {
                _devices.Add(e.Device);
            }
            RefreshDeviceTreeView();
            UpdateStatusCounts();
        });
    }

    private void DeviceManager_DeviceUpdated(object? sender, DeviceEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            lock (_devicesLock)
            {
                var existing = _devices.FirstOrDefault(d => d.Id == e.Device.Id);
                if (existing != null)
                {
                    var index = _devices.IndexOf(existing);
                    _devices[index] = e.Device;
                }
            }
            RefreshDeviceTreeView();
            UpdateStatusCounts();
        });
    }

    #endregion

    #region Toolbar & Search

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = SearchBox.Text;
        if (string.IsNullOrWhiteSpace(searchText))
        {
            RefreshDeviceTreeView();
            StatusText.Text = "Ready";
            return;
        }

        // Filter devices based on search text
        FilterDeviceTreeView(searchText);
        StatusText.Text = $"Filtering: {searchText}";
    }

    private void FilterDeviceTreeView(string searchText)
    {
        DeviceTreeView.Items.Clear();

        var showOffline = ShowOfflineMenuItem.IsChecked;
        var searchLower = searchText.ToLowerInvariant();
        List<Device> deviceSnapshot;
        lock (_devicesLock)
        {
            deviceSnapshot = _devices.ToList(); // Take snapshot under lock
        }

        var filteredDevices = deviceSnapshot
            .Where(d => (showOffline || d.IsOnline) &&
                       (d.DisplayName.ToLowerInvariant().Contains(searchLower) ||
                        d.IpAddress.Contains(searchLower) ||
                        (d.Hostname?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                        (d.MacAddress?.ToLowerInvariant().Contains(searchLower) ?? false)))
            .OrderBy(d => d.DisplayName)
            .ToList();

        // Group by online status
        var onlineDevices = filteredDevices.Where(d => d.IsOnline).ToList();
        var offlineDevices = filteredDevices.Where(d => !d.IsOnline).ToList();

        if (onlineDevices.Any())
        {
            var onlineCategory = new TreeViewItem
            {
                Header = $"Online ({onlineDevices.Count})",
                IsExpanded = true,
                FontWeight = FontWeights.SemiBold
            };

            foreach (var device in onlineDevices)
            {
                onlineCategory.Items.Add(CreateDeviceTreeItem(device));
            }

            DeviceTreeView.Items.Add(onlineCategory);
        }

        if (offlineDevices.Any() && showOffline)
        {
            var offlineCategory = new TreeViewItem
            {
                Header = $"Offline ({offlineDevices.Count})",
                IsExpanded = true,
                FontWeight = FontWeights.SemiBold
            };

            foreach (var device in offlineDevices)
            {
                offlineCategory.Items.Add(CreateDeviceTreeItem(device));
            }

            DeviceTreeView.Items.Add(offlineCategory);
        }

        DeviceCountText.Text = $"{filteredDevices.Count} device{(filteredDevices.Count != 1 ? "s" : "")} (filtered)";
    }

    #endregion

    #region Device Tree View

    private void RefreshDeviceTreeView()
    {
        DeviceTreeView.Items.Clear();

        var showOffline = ShowOfflineMenuItem.IsChecked;
        List<Device> deviceSnapshot;
        lock (_devicesLock)
        {
            deviceSnapshot = _devices.ToList(); // Take snapshot under lock
        }
        var filteredDevices = showOffline ? deviceSnapshot : deviceSnapshot.Where(d => d.IsOnline).ToList();

        // Group devices: Online first, then Offline
        var onlineDevices = filteredDevices.Where(d => d.IsOnline).OrderBy(d => d.DisplayName).ToList();
        var offlineDevices = filteredDevices.Where(d => !d.IsOnline).OrderBy(d => d.DisplayName).ToList();

        // Create Online category
        if (onlineDevices.Any())
        {
            var onlineCategory = new TreeViewItem
            {
                Header = $"Online ({onlineDevices.Count})",
                IsExpanded = true,
                FontWeight = FontWeights.SemiBold
            };

            foreach (var device in onlineDevices)
            {
                onlineCategory.Items.Add(CreateDeviceTreeItem(device));
            }

            DeviceTreeView.Items.Add(onlineCategory);
        }

        // Create Offline category
        if (offlineDevices.Any() && showOffline)
        {
            var offlineCategory = new TreeViewItem
            {
                Header = $"Offline ({offlineDevices.Count})",
                IsExpanded = true,
                FontWeight = FontWeights.SemiBold
            };

            foreach (var device in offlineDevices)
            {
                offlineCategory.Items.Add(CreateDeviceTreeItem(device));
            }

            DeviceTreeView.Items.Add(offlineCategory);
        }

        // Update device count
        DeviceCountText.Text = $"{filteredDevices.Count} device{(filteredDevices.Count != 1 ? "s" : "")}";
    }

    private TreeViewItem CreateDeviceTreeItem(Device device)
    {
        var statusColor = device.IsOnline
            ? (SolidColorBrush)Resources["OnlineStatusBrush"]
            : (SolidColorBrush)Resources["OfflineStatusBrush"];

        var textColor = device.IsOnline
            ? (SolidColorBrush)Resources["TextBrush"]
            : (SolidColorBrush)Resources["DisabledTextBrush"];

        var secondaryColor = device.IsOnline
            ? (SolidColorBrush)Resources["SecondaryTextBrush"]
            : (SolidColorBrush)Resources["DisabledTextBrush"];

        var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };

        var statusIndicator = new System.Windows.Shapes.Ellipse
        {
            Width = 8,
            Height = 8,
            Fill = statusColor,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        var nameText = new TextBlock
        {
            Text = device.DisplayName,
            Foreground = textColor,
            VerticalAlignment = VerticalAlignment.Center
        };

        var ipText = new TextBlock
        {
            Text = $" ({device.IpAddress})",
            Foreground = secondaryColor,
            VerticalAlignment = VerticalAlignment.Center
        };

        stackPanel.Children.Add(statusIndicator);
        stackPanel.Children.Add(nameText);
        stackPanel.Children.Add(ipText);

        return new TreeViewItem
        {
            Header = stackPanel,
            Tag = device,
            FontWeight = FontWeights.Normal
        };
    }

    private void UpdateStatusCounts()
    {
        List<Device> deviceSnapshot;
        lock (_devicesLock)
        {
            deviceSnapshot = _devices.ToList(); // Take snapshot under lock
        }
        var onlineCount = deviceSnapshot.Count(d => d.IsOnline);
        var offlineCount = deviceSnapshot.Count(d => !d.IsOnline);

        OnlineCountText.Text = $"{onlineCount} online";
        OfflineCountText.Text = $"{offlineCount} offline";
    }

    #endregion

    #region Device List Events

    private void DeviceTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        var selectedItem = DeviceTreeView.SelectedItem as TreeViewItem;

        if (selectedItem == null || selectedItem.Items.Count > 0)
        {
            // Category node selected or nothing selected
            NoSelectionPanel.Visibility = Visibility.Visible;
            SelectedDevicePanel.Visibility = Visibility.Collapsed;
            return;
        }

        // Device node selected - show details panel
        var device = selectedItem.Tag as Device;
        if (device == null)
        {
            NoSelectionPanel.Visibility = Visibility.Visible;
            SelectedDevicePanel.Visibility = Visibility.Collapsed;
            return;
        }

        NoSelectionPanel.Visibility = Visibility.Collapsed;
        SelectedDevicePanel.Visibility = Visibility.Visible;

        // Populate device details
        DeviceNameText.Text = device.DisplayName;
        DeviceIPText.Text = device.IpAddress;
        DeviceMACText.Text = device.MacAddress ?? "Unknown";
        DeviceTypeText.Text = "Device"; // TODO: Add device type categorization
        DeviceFirstSeenText.Text = device.FirstDiscovered.ToLocalTime().ToString("MMM dd, yyyy");
        DeviceLastSeenText.Text = device.LastSeen.ToLocalTime().ToString("MMM dd, yyyy h:mm tt");
        DeviceNotesText.Text = device.Notes ?? "";

        // Update status indicator
        if (device.IsOnline)
        {
            DeviceStatusIndicator.Fill = (SolidColorBrush)Resources["OnlineStatusBrush"];
            DeviceStatusText.Text = "Online";
        }
        else
        {
            DeviceStatusIndicator.Fill = (SolidColorBrush)Resources["OfflineStatusBrush"];
            DeviceStatusText.Text = "Offline";
        }
    }

    #endregion

    #region Device Details Events

    private async void EditDevice_Click(object sender, RoutedEventArgs e)
    {
        var selectedItem = DeviceTreeView.SelectedItem as TreeViewItem;
        if (selectedItem?.Tag is not Device device)
        {
            MessageBox.Show("Please select a device to edit.", "Edit Device",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new EditDeviceWindow(device)
        {
            Owner = this
        };

        // Apply current theme to dialog
        ApplyThemeToWindow(dialog);

        if (dialog.ShowDialog() == true)
        {
            // Save the updated device
            await _deviceManager.UpdateDeviceAsync(device);
            await LoadDevicesAsync();
            StatusText.Text = $"Updated device: {device.DisplayName}";
        }
    }

    private void OpenPort_Click(object sender, RoutedEventArgs e)
    {
        var selectedItem = DeviceTreeView.SelectedItem as TreeViewItem;
        if (selectedItem?.Tag is not Device device)
        {
            MessageBox.Show("Please select a device first.", "Open Device",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            // Default to HTTPS, fallback to HTTP
            var url = $"https://{device.IpAddress}";

            var result = MessageBox.Show(
                $"Open device in browser?\n\n{url}\n\nIf HTTPS doesn't work, try HTTP instead.",
                "Open Device",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
                StatusText.Text = $"Opening {url}";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open browser: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddCredentials_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Open credentials dialog
        MessageBox.Show("Credentials management not yet implemented.\n\n" +
            "This will securely store login credentials using Windows Credential Manager.",
            "Add Credentials", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    #endregion
}
