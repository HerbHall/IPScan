using System.Windows;
using IPScan.Core.Models;

namespace IPScan.GUI;

public partial class EditDeviceWindow : Window
{
    public Device Device { get; private set; }

    public EditDeviceWindow(Device device)
    {
        InitializeComponent();
        Device = device;

        LoadDevice();
    }

    private void LoadDevice()
    {
        DeviceIPText.Text = Device.IpAddress;
        DeviceNameTextBox.Text = Device.Name;
        NotesTextBox.Text = Device.Notes ?? string.Empty;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // Validate
        if (string.IsNullOrWhiteSpace(DeviceNameTextBox.Text))
        {
            System.Windows.MessageBox.Show("Device name cannot be empty", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Update device
        Device.Name = DeviceNameTextBox.Text.Trim();
        Device.Notes = NotesTextBox.Text.Trim();

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
