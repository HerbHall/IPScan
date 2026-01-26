using System.IO;
using System.Text.Json;
using System.Windows;
using IPScan.Core.Models;

// Disambiguate types
using ThemeMode = IPScan.Core.Models.ThemeMode;
using AccentColorMode = IPScan.Core.Models.AccentColorMode;

namespace IPScan.GUI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // Create main window first and set it as the application's main window
        var mainWindow = new MainWindow();
        MainWindow = mainWindow;

        // Load settings for splash screen
        var (splashTimeout, themeMode, accentColorMode, customAccentColor) = LoadSplashSettings();

        // Show splash screen with theme settings (dialog blocks until closed)
        var splash = new SplashScreen(splashTimeout, themeMode, accentColorMode, customAccentColor);
        splash.ShowDialog();

        // Show main window after splash closes
        mainWindow.Show();
    }

    private (int timeout, IPScan.Core.Models.ThemeMode theme, IPScan.Core.Models.AccentColorMode accent, string customColor) LoadSplashSettings()
    {
        const int defaultTimeout = 5;
        var defaultTheme = IPScan.Core.Models.ThemeMode.CrtGreen;
        var defaultAccent = IPScan.Core.Models.AccentColorMode.System;
        const string defaultCustomColor = "#00FF00";

        try
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var settingsPath = Path.Combine(appDataPath, "IPScan", "settings.json");

            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                using var doc = JsonDocument.Parse(json);

                var timeout = defaultTimeout;
                if (doc.RootElement.TryGetProperty("splashTimeoutSeconds", out var timeoutElement))
                {
                    timeout = timeoutElement.GetInt32();
                }

                var themeMode = defaultTheme;
                if (doc.RootElement.TryGetProperty("themeMode", out var themeElement))
                {
                    Enum.TryParse<IPScan.Core.Models.ThemeMode>(themeElement.GetString(), out themeMode);
                }

                var accentMode = defaultAccent;
                if (doc.RootElement.TryGetProperty("accentColorMode", out var accentElement))
                {
                    Enum.TryParse<IPScan.Core.Models.AccentColorMode>(accentElement.GetString(), out accentMode);
                }

                var customColor = defaultCustomColor;
                if (doc.RootElement.TryGetProperty("customAccentColor", out var colorElement))
                {
                    customColor = colorElement.GetString() ?? defaultCustomColor;
                }

                return (timeout, themeMode, accentMode, customColor);
            }
        }
        catch
        {
            // Use defaults on any error
        }

        return (defaultTimeout, defaultTheme, defaultAccent, defaultCustomColor);
    }
}

