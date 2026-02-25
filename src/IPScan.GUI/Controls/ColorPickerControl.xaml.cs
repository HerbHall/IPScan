using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

// Disambiguate WPF types
using Color = System.Windows.Media.Color;
using UserControl = System.Windows.Controls.UserControl;

namespace IPScan.GUI.Controls;

public partial class ColorPickerControl : UserControl
{
    private bool _isUpdating;

    public ColorPickerControl()
    {
        InitializeComponent();
        UpdatePreview();
    }

    public static readonly DependencyProperty SelectedColorProperty =
        DependencyProperty.Register(
            nameof(SelectedColor),
            typeof(Color),
            typeof(ColorPickerControl),
            new PropertyMetadata(Color.FromRgb(0, 255, 0), OnSelectedColorChanged));

    public Color SelectedColor
    {
        get => (Color)GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ColorPickerControl control && !control._isUpdating)
        {
            control.UpdateControlsFromColor((Color)e.NewValue);
        }
    }

    private void UpdateControlsFromColor(Color color)
    {
        _isUpdating = true;

        RedSlider.Value = color.R;
        GreenSlider.Value = color.G;
        BlueSlider.Value = color.B;

        RedTextBox.Text = color.R.ToString();
        GreenTextBox.Text = color.G.ToString();
        BlueTextBox.Text = color.B.ToString();

        HexTextBox.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        UpdatePreview();

        _isUpdating = false;
    }

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdating) return;

        // Controls may not be initialized yet during construction
        if (RedTextBox == null || GreenTextBox == null || BlueTextBox == null || HexTextBox == null)
            return;

        _isUpdating = true;

        // Update text boxes
        RedTextBox.Text = ((int)RedSlider.Value).ToString();
        GreenTextBox.Text = ((int)GreenSlider.Value).ToString();
        BlueTextBox.Text = ((int)BlueSlider.Value).ToString();

        // Update hex text box
        var r = (byte)RedSlider.Value;
        var g = (byte)GreenSlider.Value;
        var b = (byte)BlueSlider.Value;
        HexTextBox.Text = $"#{r:X2}{g:X2}{b:X2}";

        UpdatePreview();
        UpdateSelectedColor();

        _isUpdating = false;
    }

    private void RgbTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating) return;

        _isUpdating = true;

        try
        {
            if (int.TryParse(RedTextBox.Text, out var r) && r >= 0 && r <= 255)
                RedSlider.Value = r;

            if (int.TryParse(GreenTextBox.Text, out var g) && g >= 0 && g <= 255)
                GreenSlider.Value = g;

            if (int.TryParse(BlueTextBox.Text, out var b) && b >= 0 && b <= 255)
                BlueSlider.Value = b;

            // Update hex text box
            var red = (byte)RedSlider.Value;
            var green = (byte)GreenSlider.Value;
            var blue = (byte)BlueSlider.Value;
            HexTextBox.Text = $"#{red:X2}{green:X2}{blue:X2}";

            UpdatePreview();
            UpdateSelectedColor();
            HideValidationError();
        }
        catch
        {
            ShowValidationError("Invalid RGB value. Must be 0-255.");
        }

        _isUpdating = false;
    }

    private void HexTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating) return;

        _isUpdating = true;

        try
        {
            var hex = HexTextBox.Text.Trim();

            // Validate hex format
            if (!Regex.IsMatch(hex, @"^#[0-9A-Fa-f]{6}$"))
            {
                if (!string.IsNullOrEmpty(hex) && hex != "#")
                {
                    ShowValidationError("Invalid hex format. Use #RRGGBB (e.g., #00FF00)");
                }
                _isUpdating = false;
                return;
            }

            // Parse hex color
            var r = Convert.ToByte(hex.Substring(1, 2), 16);
            var g = Convert.ToByte(hex.Substring(3, 2), 16);
            var b = Convert.ToByte(hex.Substring(5, 2), 16);

            // Update sliders and text boxes
            RedSlider.Value = r;
            GreenSlider.Value = g;
            BlueSlider.Value = b;

            RedTextBox.Text = r.ToString();
            GreenTextBox.Text = g.ToString();
            BlueTextBox.Text = b.ToString();

            UpdatePreview();
            UpdateSelectedColor();
            HideValidationError();
        }
        catch
        {
            ShowValidationError("Invalid hex color format.");
        }

        _isUpdating = false;
    }

    private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // Only allow numeric input
        e.Handled = !IsTextNumeric(e.Text);
    }

    private static bool IsTextNumeric(string text)
    {
        return Regex.IsMatch(text, @"^[0-9]+$");
    }

    private void UpdatePreview()
    {
        // Controls may not be initialized yet during construction
        if (PreviewBrush == null || RedSlider == null || GreenSlider == null || BlueSlider == null)
            return;

        try
        {
            var r = (byte)RedSlider.Value;
            var g = (byte)GreenSlider.Value;
            var b = (byte)BlueSlider.Value;

            PreviewBrush.Color = Color.FromRgb(r, g, b);
        }
        catch
        {
            // Ignore errors during preview update
        }
    }

    private void UpdateSelectedColor()
    {
        // Controls may not be initialized yet during construction
        if (RedSlider == null || GreenSlider == null || BlueSlider == null)
            return;

        try
        {
            var r = (byte)RedSlider.Value;
            var g = (byte)GreenSlider.Value;
            var b = (byte)BlueSlider.Value;

            SelectedColor = Color.FromRgb(r, g, b);
        }
        catch
        {
            // Ignore errors during color update
        }
    }

    private void ShowValidationError(string message)
    {
        // Validation control may not be initialized yet during window construction
        if (ValidationText == null) return;

        ValidationText.Text = message;
        ValidationText.Visibility = Visibility.Visible;
    }

    private void HideValidationError()
    {
        // Validation control may not be initialized yet during window construction
        if (ValidationText == null) return;

        ValidationText.Visibility = Visibility.Collapsed;
    }

    public string GetHexColor()
    {
        return HexTextBox.Text;
    }

    public void SetHexColor(string hexColor)
    {
        if (!string.IsNullOrEmpty(hexColor))
        {
            HexTextBox.Text = hexColor;
        }
    }
}
