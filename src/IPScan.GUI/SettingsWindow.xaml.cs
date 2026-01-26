using System.Windows;
using IPScan.Core.Models;
using IPScan.Core.Services;

namespace IPScan.GUI;

public partial class SettingsWindow : Window
{
    private readonly ISettingsService _settingsService;
    private AppSettings _settings;

    public SettingsWindow(ISettingsService settingsService, AppSettings settings)
    {
        InitializeComponent();
        _settingsService = settingsService;
        _settings = settings;

        LoadSettings();
    }

    private void LoadSettings()
    {
        // Scanning
        ScanOnStartupCheckBox.IsChecked = _settings.ScanOnStartup;
        ScanTimeoutTextBox.Text = _settings.ScanTimeoutMs.ToString();
        MaxConcurrentTextBox.Text = _settings.MaxConcurrentScans.ToString();

        // Device Management
        ShowOfflineCheckBox.IsChecked = _settings.ShowOfflineDevices;
        AutoRemoveCheckBox.IsChecked = _settings.AutoRemoveMissingDevices;
        MissedScansTextBox.Text = _settings.MissedScansBeforeRemoval.ToString();

        // Window Behavior
        foreach (System.Windows.Controls.ComboBoxItem item in WindowStartupComboBox.Items)
        {
            if (item.Tag.ToString() == _settings.WindowStartup.ToString())
            {
                WindowStartupComboBox.SelectedItem = item;
                break;
            }
        }
        SplashTimeoutTextBox.Text = _settings.SplashTimeoutSeconds.ToString();

        // Network Configuration
        if (_settings.Subnet == "auto")
        {
            AutoSubnetRadio.IsChecked = true;
        }
        else
        {
            CustomSubnetRadio.IsChecked = true;
            CustomSubnetTextBox.Text = _settings.CustomSubnet;
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Validate and save settings
            if (!int.TryParse(ScanTimeoutTextBox.Text, out var scanTimeout) || scanTimeout < 100)
            {
                System.Windows.MessageBox.Show("Scan timeout must be at least 100ms", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(MaxConcurrentTextBox.Text, out var maxConcurrent) || maxConcurrent < 1 || maxConcurrent > 1000)
            {
                System.Windows.MessageBox.Show("Max concurrent scans must be between 1 and 1000", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(MissedScansTextBox.Text, out var missedScans) || missedScans < 1)
            {
                System.Windows.MessageBox.Show("Missed scans before removal must be at least 1", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(SplashTimeoutTextBox.Text, out var splashTimeout) || splashTimeout < 0)
            {
                System.Windows.MessageBox.Show("Splash timeout must be 0 or greater", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate custom subnet if selected
            if (CustomSubnetRadio.IsChecked == true && !string.IsNullOrWhiteSpace(CustomSubnetTextBox.Text))
            {
                // Basic CIDR validation
                if (!System.Text.RegularExpressions.Regex.IsMatch(CustomSubnetTextBox.Text, @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}/\d{1,2}$"))
                {
                    System.Windows.MessageBox.Show("Custom subnet must be in CIDR notation (e.g., 192.168.1.0/24)", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // Update settings
            _settings.ScanOnStartup = ScanOnStartupCheckBox.IsChecked ?? false;
            _settings.ScanTimeoutMs = scanTimeout;
            _settings.MaxConcurrentScans = maxConcurrent;
            _settings.ShowOfflineDevices = ShowOfflineCheckBox.IsChecked ?? true;
            _settings.AutoRemoveMissingDevices = AutoRemoveCheckBox.IsChecked ?? false;
            _settings.MissedScansBeforeRemoval = missedScans;
            _settings.SplashTimeoutSeconds = splashTimeout;

            // Window startup mode
            var selectedItem = WindowStartupComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem;
            if (selectedItem != null && Enum.TryParse<WindowStartupMode>(selectedItem.Tag.ToString(), out var windowMode))
            {
                _settings.WindowStartup = windowMode;
            }

            // Network configuration
            if (AutoSubnetRadio.IsChecked == true)
            {
                _settings.Subnet = "auto";
                _settings.CustomSubnet = string.Empty;
            }
            else
            {
                _settings.Subnet = "custom";
                _settings.CustomSubnet = CustomSubnetTextBox.Text;
            }

            // Save to file
            await _settingsService.SaveSettingsAsync(_settings);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error saving settings: {ex.Message}", "Save Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
