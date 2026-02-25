using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;
using Windows.UI.ViewManagement;
using IPScan.Core.Models;
using Microsoft.Win32;

// Disambiguate WPF types
using Color = System.Windows.Media.Color;
using ThemeMode = IPScan.Core.Models.ThemeMode;
using AccentColorMode = IPScan.Core.Models.AccentColorMode;

namespace IPScan.GUI;

public partial class SplashScreen : Window
{
    private readonly DispatcherTimer _timer;
    private readonly int _timeoutSeconds;
    private int _remainingSeconds;
    private readonly IPScan.Core.Models.ThemeMode _themeMode;
    private readonly IPScan.Core.Models.AccentColorMode _accentColorMode;
    private readonly string _customAccentColor;
    private bool _isDarkMode;

    public SplashScreen(int timeoutSeconds = 5, IPScan.Core.Models.ThemeMode themeMode = IPScan.Core.Models.ThemeMode.CrtGreen,
        IPScan.Core.Models.AccentColorMode accentColorMode = IPScan.Core.Models.AccentColorMode.System, string customAccentColor = "#00FF00")
    {
        InitializeComponent();

        _timeoutSeconds = timeoutSeconds;
        _remainingSeconds = timeoutSeconds;
        _themeMode = themeMode;
        _accentColorMode = accentColorMode;
        _customAccentColor = customAccentColor;

        // Apply theme based on settings
        ApplyTheme();

        // Set version from assembly
        SetVersionInfo();

        // Setup countdown timer
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _timer.Tick += Timer_Tick;

        Loaded += SplashScreen_Loaded;
    }

    private void SplashScreen_Loaded(object sender, RoutedEventArgs e)
    {
        _timer.Start();
    }

    private void SetVersionInfo()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        var infoVersion = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        // Use informational version if available (includes pre-release info from MinVer)
        var displayVersion = !string.IsNullOrEmpty(infoVersion)
            ? infoVersion.Split('+')[0] // Remove build metadata if present
            : version?.ToString() ?? "0.0.0";

        VersionText.Text = $"Version {displayVersion}";
    }

    private bool IsWindowsDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int intValue && intValue == 0;
        }
        catch
        {
            return false;
        }
    }

    private void ApplyTheme()
    {
        // Determine theme based on ThemeMode setting
        switch (_themeMode)
        {
            case IPScan.Core.Models.ThemeMode.WindowsSystem:
                _isDarkMode = IsWindowsDarkMode();
                break;

            case IPScan.Core.Models.ThemeMode.Light:
                _isDarkMode = false;
                break;

            case IPScan.Core.Models.ThemeMode.Dark:
                _isDarkMode = true;
                break;

            case IPScan.Core.Models.ThemeMode.CrtGreen:
                ApplyCrtGreenTheme();
                return; // CRT theme handles everything

            default:
                _isDarkMode = false;
                break;
        }

        // Get accent color
        var accentColor = GetAccentColor();

        // Apply theme colors
        ApplyStandardTheme(accentColor);
    }

    private void ApplyCrtGreenTheme()
    {
        // CRT Green Terminal Theme - Authentic 1970s-80s terminal aesthetic
        Color accentColor;
        if (_accentColorMode == IPScan.Core.Models.AccentColorMode.Custom)
        {
            accentColor = ParseHexColor(_customAccentColor);
        }
        else
        {
            accentColor = Color.FromRgb(0, 255, 65); // P1 phosphor green
        }

        var accentBrush = new SolidColorBrush(accentColor);
        var dimmedGreen = new SolidColorBrush(Color.FromRgb(0, 170, 0));

        Resources["SplashBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0, 0, 0)); // Pure black
        Resources["SplashBorderBrush"] = new SolidColorBrush(Color.FromRgb(0, 51, 0)); // Dark green
        Resources["AccentBrush"] = accentBrush;
        Resources["ConnectionLineBrush"] = accentBrush;
        Resources["DeviceNodeBrush"] = new SolidColorBrush(Color.FromRgb(10, 10, 10)); // Very dark
        Resources["DeviceBorderBrush"] = new SolidColorBrush(Color.FromRgb(0, 51, 0)); // Dark green
        Resources["DeviceIconBrush"] = accentBrush; // Bright green
        Resources["TitleBrush"] = accentBrush; // Bright green
        Resources["SubtitleBrush"] = accentBrush; // Bright green
        Resources["InfoTextBrush"] = dimmedGreen; // Dimmed green
        Resources["LinkBrush"] = accentBrush;
        Resources["FooterBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0, 0, 0)); // Black
        Resources["FooterTextBrush"] = dimmedGreen; // Dimmed green
        Resources["ProgressBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0, 51, 0)); // Dark green
    }

    private void ApplyStandardTheme(SolidColorBrush accentColor)
    {
        if (_isDarkMode)
        {
            Resources["SplashBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(32, 32, 32));
            Resources["SplashBorderBrush"] = new SolidColorBrush(Color.FromRgb(64, 64, 64));
            Resources["AccentBrush"] = accentColor;
            Resources["ConnectionLineBrush"] = accentColor;
            Resources["DeviceNodeBrush"] = new SolidColorBrush(Color.FromRgb(48, 48, 48));
            Resources["DeviceBorderBrush"] = new SolidColorBrush(Color.FromRgb(80, 80, 80));
            Resources["DeviceIconBrush"] = new SolidColorBrush(Color.FromRgb(220, 220, 220));
            Resources["TitleBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            Resources["SubtitleBrush"] = new SolidColorBrush(Color.FromRgb(180, 180, 180));
            Resources["InfoTextBrush"] = new SolidColorBrush(Color.FromRgb(150, 150, 150));
            Resources["LinkBrush"] = accentColor;
            Resources["FooterBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(40, 40, 40));
            Resources["FooterTextBrush"] = new SolidColorBrush(Color.FromRgb(150, 150, 150));
            Resources["ProgressBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(64, 64, 64));
        }
        else
        {
            // Light theme - update accent color (rest use default XAML values)
            Resources["AccentBrush"] = accentColor;
            Resources["ConnectionLineBrush"] = accentColor;
            Resources["LinkBrush"] = accentColor;
        }
    }

    private SolidColorBrush GetAccentColor()
    {
        switch (_accentColorMode)
        {
            case IPScan.Core.Models.AccentColorMode.CrtGreen:
                return new SolidColorBrush(Color.FromRgb(0, 255, 0)); // Bright CRT green

            case IPScan.Core.Models.AccentColorMode.Custom:
                return new SolidColorBrush(ParseHexColor(_customAccentColor));

            case IPScan.Core.Models.AccentColorMode.System:
            default:
                return GetWindowsAccentColor();
        }
    }

    private SolidColorBrush GetWindowsAccentColor()
    {
        try
        {
            var uiSettings = new UISettings();
            var accent = uiSettings.GetColorValue(UIColorType.Accent);
            return new SolidColorBrush(Color.FromArgb(accent.A, accent.R, accent.G, accent.B));
        }
        catch
        {
            return new SolidColorBrush(Color.FromRgb(0, 120, 212)); // Default Windows blue
        }
    }

    private Color ParseHexColor(string hexColor)
    {
        try
        {
            if (string.IsNullOrEmpty(hexColor) || !hexColor.StartsWith("#") || hexColor.Length != 7)
                return Color.FromRgb(0, 255, 0); // Default green

            var r = Convert.ToByte(hexColor.Substring(1, 2), 16);
            var g = Convert.ToByte(hexColor.Substring(3, 2), 16);
            var b = Convert.ToByte(hexColor.Substring(5, 2), 16);

            return Color.FromRgb(r, g, b);
        }
        catch
        {
            return Color.FromRgb(0, 255, 0); // Default green
        }
    }

    private double _elapsedMs = 0;

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _elapsedMs += 100;

        var totalMs = _timeoutSeconds * 1000.0;
        var progress = Math.Max(0, 100 - (_elapsedMs / totalMs * 100));
        TimeoutProgress.Value = progress;

        var remaining = Math.Ceiling((_timeoutSeconds * 1000 - _elapsedMs) / 1000);
        CountdownText.Text = $"Auto-continuing in {Math.Max(0, remaining)}s";

        if (_elapsedMs >= totalMs)
        {
            CloseSplash();
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        CloseSplash();
    }

    private void CloseSplash()
    {
        _timer.Stop();
        DialogResult = true;
        Close();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = e.Uri.AbsoluteUri,
            UseShellExecute = true
        });
        e.Handled = true;
    }
}
