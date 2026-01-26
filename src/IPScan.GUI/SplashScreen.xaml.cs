using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;
using Windows.UI.ViewManagement;

// Disambiguate WPF types from WinForms types
using Color = System.Windows.Media.Color;

namespace IPScan.GUI;

public partial class SplashScreen : Window
{
    private readonly DispatcherTimer _timer;
    private readonly int _timeoutSeconds;
    private int _remainingSeconds;
    private readonly bool _isDarkMode;

    public SplashScreen(int timeoutSeconds = 5)
    {
        InitializeComponent();

        _timeoutSeconds = timeoutSeconds;
        _remainingSeconds = timeoutSeconds;

        // Detect Windows theme
        _isDarkMode = IsWindowsDarkMode();
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
            var uiSettings = new UISettings();
            var foreground = uiSettings.GetColorValue(UIColorType.Foreground);
            // If foreground is light, we're in dark mode
            return foreground.R > 128 && foreground.G > 128 && foreground.B > 128;
        }
        catch
        {
            return false;
        }
    }

    private void ApplyTheme()
    {
        if (_isDarkMode)
        {
            Resources["SplashBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(32, 32, 32));
            Resources["SplashBorderBrush"] = new SolidColorBrush(Color.FromRgb(64, 64, 64));
            Resources["AccentBrush"] = GetWindowsAccentColor();
            Resources["ConnectionLineBrush"] = GetWindowsAccentColor();
            Resources["DeviceNodeBrush"] = new SolidColorBrush(Color.FromRgb(48, 48, 48));
            Resources["DeviceBorderBrush"] = new SolidColorBrush(Color.FromRgb(80, 80, 80));
            Resources["DeviceIconBrush"] = new SolidColorBrush(Color.FromRgb(220, 220, 220));
            Resources["TitleBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            Resources["SubtitleBrush"] = new SolidColorBrush(Color.FromRgb(180, 180, 180));
            Resources["InfoTextBrush"] = new SolidColorBrush(Color.FromRgb(150, 150, 150));
            Resources["LinkBrush"] = GetWindowsAccentColor();
            Resources["FooterBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(40, 40, 40));
            Resources["FooterTextBrush"] = new SolidColorBrush(Color.FromRgb(150, 150, 150));
            Resources["ProgressBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(64, 64, 64));
        }
        else
        {
            // Light theme - update accent color
            Resources["AccentBrush"] = GetWindowsAccentColor();
            Resources["ConnectionLineBrush"] = GetWindowsAccentColor();
            Resources["LinkBrush"] = GetWindowsAccentColor();
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
