<objective>
Implement a comprehensive theme system for IPScan that includes an iconic CRT green terminal theme (default), along with user-configurable theme and accent color settings. The theme system must support Windows System, Light, Dark, and CRT modes, with accent color options for CRT green, System accent, or custom colors. All windows (splash screen, main window, and dialogs) must respect the user's theme preferences, and settings must persist across sessions.

This feature adds professional theming capabilities while honoring the retro CRT aesthetic that defines IPScan's visual identity.
</objective>

<context>
Read and follow project conventions from @CLAUDE.md

The REQUIREMENTS.md file (section 4: GUI Theming, lines 79-86) specifies:
- Default theme: Iconic CRT green (high-contrast monochrome, P1 phosphor green glow on black background, 1970s-80s terminal aesthetic)
- User override options in Settings:
  - Theme: Windows System, Light, Dark, iconic CRT (Default)
  - Accent Color: iconic CRT, System (default), or custom color picker
- Must respond to real-time Windows theme changes
- Must persist theme preferences across sessions

Current implementation status:
- Basic dark/light mode detection exists via Windows.UI.ViewManagement.UISettings
- Theme colors are hardcoded in MainWindow.xaml resources
- ApplyTheme() and ApplyThemeToWindow() methods exist for dynamic theming
- No user control over theme selection
- No CRT green theme implemented

Technology stack:
- .NET 10.0, WPF, C#
- Windows.UI.ViewManagement for system theme detection
- JSON settings persistence in %APPDATA%\IPScan\settings.json

Key files to examine and modify:
@src/IPScan.Core/Models/AppSettings.cs - Add theme settings properties
@src/IPScan.GUI/SettingsWindow.xaml - Add theme UI controls
@src/IPScan.GUI/SettingsWindow.xaml.cs - Handle theme settings
@src/IPScan.GUI/MainWindow.xaml.cs - Apply selected theme
@src/IPScan.GUI/SplashScreen.xaml.cs - Apply theme to splash screen
</context>

<requirements>

## 1. AppSettings Model Updates

Add to AppSettings.cs:
- `ThemeMode` property: enum with values `WindowsSystem`, `Light`, `Dark`, `CrtGreen` (default: `CrtGreen`)
- `AccentColorMode` property: enum with values `CrtGreen`, `System`, `Custom` (default: `System`)
- `CustomAccentColor` property: string for hex color (e.g., "#00FF00"), default: "#00FF00"

## 2. CRT Green Theme Definition

Define CRT green theme colors following P1 phosphor specifications:
- Background: Pure black (#000000)
- Panel/Surface: Very dark gray (#0A0A0A or #0F0F0F)
- Primary text: Bright phosphor green (#00FF00 or #00FF41)
- Secondary text: Dimmed green (#00AA00)
- Accent/highlights: Bright green with slight glow effect
- Borders: Dark green (#003300)

The CRT theme should evoke a high-contrast, monochrome terminal display with that characteristic green glow.

## 3. Settings Window UI

Add to SettingsWindow.xaml (new section or existing sections):

**Theme Selection:**
- Section header: "Appearance"
- ComboBox for theme mode with 4 options:
  - "Windows System" (follows OS theme)
  - "Light" (light theme)
  - "Dark" (dark theme)
  - "CRT Green Terminal" (iconic retro green)
- Description text explaining the CRT theme aesthetic

**Accent Color Selection:**
- ComboBox or radio buttons for accent color mode:
  - "CRT Green" (iconic green)
  - "Windows System" (follows OS accent)
  - "Custom Color" (user-defined)
- When "Custom Color" selected, show a color picker control
- Preview swatch showing currently selected accent color

## 4. Color Picker Control

Create a reusable color picker user control (`ColorPickerControl.xaml` + `.xaml.cs`):
- RGB sliders (0-255 for Red, Green, Blue)
- Hex color text input (e.g., "#00FF00")
- Color preview rectangle showing the selected color
- Real-time updates as user adjusts sliders or types hex value
- Validation for hex input format

This control should be embedded in SettingsWindow when Custom Color mode is selected.

## 5. Theme Application Logic

Update MainWindow.xaml.cs:
- Modify `ApplyTheme()` to check AppSettings.ThemeMode first
- If ThemeMode is WindowsSystem, use current behavior (detect from UISettings)
- If ThemeMode is Light/Dark/CrtGreen, apply that theme explicitly
- Update `ApplyAccentColor()` to respect AccentColorMode setting
- Ensure `ApplyThemeToWindow()` applies the correct theme to child dialogs

Add to SplashScreen.xaml.cs:
- Apply theme on initialization based on saved AppSettings
- Ensure splash screen matches selected theme mode

## 6. Real-time Theme Updates

Maintain existing behavior for Windows System theme mode:
- Continue listening to UISettings.ColorValuesChanged
- Only respond to system theme changes if ThemeMode == WindowsSystem
- Ignore system changes for other theme modes

## 7. Settings Persistence

Ensure theme preferences save/load:
- Load settings on app startup before showing splash screen
- Apply theme before any window is rendered
- Save settings when user clicks Save in SettingsWindow
- Settings should persist across application restarts

</requirements>

<implementation>

Follow these guidelines:

**Theme Priority Logic:**
1. Check AppSettings.ThemeMode first
2. If WindowsSystem, detect from UISettings (existing behavior)
3. Otherwise, apply the explicitly selected theme
4. Apply accent color based on AccentColorMode

**Color Application:**
- Use WPF DynamicResource for all theme colors
- Define theme resources in MainWindow.Resources or App.Resources
- Update resources at runtime when theme changes
- Ensure consistent color application across all windows

**CRT Theme Aesthetics:**
The CRT green theme should feel authentic to 1970s-80s terminals:
- Use pure black backgrounds (no gray)
- Green text should be bright and high-contrast
- Consider subtle glow effects (OuterGlowBitmapEffect or DropShadowEffect in green)
- Borders and dividers should be dark green
- Interactive elements (buttons, links) should use bright green
- Avoid any warm colors (no reds, oranges, yellows in CRT mode)

**Why These Constraints:**
- Theme mode enum allows explicit control vs automatic detection
- Accent color separation enables mixing CRT aesthetic with system colors
- Color picker provides full customization for power users
- Persistence ensures user preferences are respected across sessions
- Real-time updates maintain responsiveness to system theme changes

**Testing Considerations:**
After implementation, verify:
- All four theme modes work correctly
- All three accent color modes work correctly
- Custom color picker validates input and updates preview
- Settings persist after app restart
- Splash screen, main window, and dialogs all respect theme
- Real-time Windows theme changes work only in WindowsSystem mode

</implementation>

<output>
Modify or create the following files:

**Core Models:**
- `./src/IPScan.Core/Models/AppSettings.cs` - Add ThemeMode, AccentColorMode, CustomAccentColor properties with enums

**GUI Controls:**
- `./src/IPScan.GUI/Controls/ColorPickerControl.xaml` - New color picker user control
- `./src/IPScan.GUI/Controls/ColorPickerControl.xaml.cs` - Color picker logic

**Settings Window:**
- `./src/IPScan.GUI/SettingsWindow.xaml` - Add Appearance section with theme and accent color selectors
- `./src/IPScan.GUI/SettingsWindow.xaml.cs` - Handle theme settings, show/hide color picker

**Main Window:**
- `./src/IPScan.GUI/MainWindow.xaml.cs` - Update ApplyTheme(), ApplyAccentColor(), ApplyThemeToWindow() to respect user settings

**Splash Screen:**
- `./src/IPScan.GUI/SplashScreen.xaml.cs` - Apply theme on initialization

**Optional:**
- `./src/IPScan.GUI/App.xaml` - Consider defining theme resources here for global access
</output>

<verification>
Before declaring complete, verify your implementation:

1. **Theme Switching:**
   - Launch app, open Settings, change theme mode to each option
   - Confirm main window updates immediately
   - Close and reopen Settings dialog, confirm it respects the theme

2. **CRT Theme Accuracy:**
   - Select CRT Green Terminal theme
   - Verify black background, bright green text, appropriate contrast
   - Check that the aesthetic matches 1970s-80s terminal monitors

3. **Accent Color Options:**
   - Test CRT Green accent mode
   - Test Windows System accent mode (should use Windows accent color)
   - Test Custom Color mode with color picker
   - Verify accent color applies to buttons, links, status bar

4. **Color Picker:**
   - Adjust RGB sliders, verify preview updates
   - Type hex color in textbox, verify it parses and updates preview
   - Try invalid hex input, verify validation feedback

5. **Persistence:**
   - Set theme to CRT Green, accent to Custom Color (#FF00FF)
   - Close app completely
   - Reopen app
   - Verify theme and accent color are restored

6. **All Windows:**
   - With CRT theme active, open Settings dialog - should be CRT themed
   - Open Edit Device dialog - should be CRT themed
   - Check splash screen on next launch - should be CRT themed

7. **Windows System Mode:**
   - Set theme to Windows System
   - Change Windows theme (dark to light or vice versa)
   - Verify IPScan updates to match

8. **Build and Run:**
   - Ensure project builds without errors
   - Run the application and test all scenarios above

</verification>

<success_criteria>
- AppSettings includes ThemeMode, AccentColorMode, and CustomAccentColor properties
- SettingsWindow has UI for selecting theme mode and accent color mode
- Color picker control is fully functional with RGB sliders, hex input, and preview
- CRT Green theme provides authentic 1970s-80s terminal aesthetic
- All four theme modes (WindowsSystem, Light, Dark, CrtGreen) work correctly
- All three accent color modes work correctly
- Theme and accent preferences persist across sessions
- Splash screen, main window, and all dialogs respect selected theme
- Real-time Windows theme changes only affect WindowsSystem mode
- Application builds and runs without errors
</success_criteria>
