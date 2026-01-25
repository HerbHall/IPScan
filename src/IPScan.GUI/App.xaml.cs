using System.IO;
using System.Text.Json;
using System.Windows;

namespace IPScan.GUI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // Load settings to get splash timeout
        var splashTimeout = LoadSplashTimeout();

        // Show splash screen
        var splash = new SplashScreen(splashTimeout);
        splash.ShowDialog();

        // Show main window
        var mainWindow = new MainWindow();
        mainWindow.Show();
    }

    private int LoadSplashTimeout()
    {
        const int defaultTimeout = 5;

        try
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var settingsPath = Path.Combine(appDataPath, "IPScan", "settings.json");

            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("splashTimeoutSeconds", out var timeoutElement))
                {
                    return timeoutElement.GetInt32();
                }
            }
        }
        catch
        {
            // Use default on any error
        }

        return defaultTimeout;
    }
}

