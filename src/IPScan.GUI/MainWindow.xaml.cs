using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using Windows.UI.ViewManagement;

namespace IPScan.GUI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly UISettings _uiSettings;
    private bool _isDarkMode;

    public MainWindow()
    {
        InitializeComponent();

        // Initialize Windows UI settings for theme detection
        _uiSettings = new UISettings();
        _uiSettings.ColorValuesChanged += UiSettings_ColorValuesChanged;

        // Apply initial theme
        DetectAndApplyTheme();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Update device count placeholder
        DeviceCountText.Text = "4 devices";

        // TODO: Load devices from IPScan.Core
        // TODO: Start auto-scan if enabled in settings
        StatusText.Text = "Ready - Click Scan to discover devices";
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

    #endregion

    #region Menu Event Handlers

    private void ScanNetwork_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Scanning network for new devices...";
        // TODO: Implement network scanning via IPScan.Core
        MessageBox.Show("Network scanning not yet implemented.\n\nThis will scan the local subnet for new devices.",
            "Scan Network", MessageBoxButton.OK, MessageBoxImage.Information);
        StatusText.Text = "Ready";
    }

    private void RescanAll_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Rescanning all known devices...";
        // TODO: Implement rescan via IPScan.Core
        MessageBox.Show("Rescan not yet implemented.\n\nThis will update information for all known devices.",
            "Rescan All", MessageBoxButton.OK, MessageBoxImage.Information);
        StatusText.Text = "Ready";
    }

    private void ExportDevices_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = ".json",
            FileName = "devices_export"
        };

        if (dialog.ShowDialog() == true)
        {
            // TODO: Export devices via IPScan.Core
            MessageBox.Show($"Export to {dialog.FileName} not yet implemented.",
                "Export Devices", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void ImportDevices_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = ".json"
        };

        if (dialog.ShowDialog() == true)
        {
            // TODO: Import devices via IPScan.Core
            MessageBox.Show($"Import from {dialog.FileName} not yet implemented.",
                "Import Devices", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Open settings dialog
        MessageBox.Show("Settings dialog not yet implemented.\n\nThis will allow you to configure:\n" +
            "- Scan on startup\n- Default subnet\n- Port list\n- Theme preferences\n- Splash screen timeout",
            "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ToggleOffline_Click(object sender, RoutedEventArgs e)
    {
        var showOffline = ShowOfflineMenuItem.IsChecked;
        StatusText.Text = showOffline ? "Showing all devices" : "Hiding offline devices";
        // TODO: Filter device list based on online status
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Refreshing device list...";
        // TODO: Refresh device list from saved data
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

    #region Toolbar & Search

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = SearchBox.Text;
        if (string.IsNullOrWhiteSpace(searchText))
        {
            // TODO: Show all devices
            return;
        }

        // TODO: Filter device list based on search text
        StatusText.Text = $"Filtering devices: {searchText}";
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
        NoSelectionPanel.Visibility = Visibility.Collapsed;
        SelectedDevicePanel.Visibility = Visibility.Visible;

        // TODO: Load actual device data from IPScan.Core
        // For now, display placeholder data based on selection
    }

    #endregion

    #region Device Details Events

    private void EditDevice_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Open device edit dialog
        MessageBox.Show("Device editing not yet implemented.\n\nThis will allow you to:\n" +
            "- Rename the device\n- Change device type\n- Add notes",
            "Edit Device", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OpenPort_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Open port in browser or appropriate application
        MessageBox.Show("Port opening not yet implemented.\n\nThis will open the device's web interface in your browser.",
            "Open Port", MessageBoxButton.OK, MessageBoxImage.Information);
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
