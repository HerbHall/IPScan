# IPScan Requirements Specification

## Overview

IPScan is a Windows network device discovery tool that locates HTTP-enabled devices on the local subnet and presents them in an accessible interface for configuration.

## Functional Requirements

### Core Features

1. **Network Scanning**
   - Upon startup Scan local or defined subnet for new IP devices not previously discovered.
   - Automatically detect and identify device types (Router, Switch, Server, etc.)
   - Automatically detect known ports on each device.
   - Present discovered devices in a hierarchical view with name, IP, device type, ports, etc...
   - Include links to access devices and ports as appropriate.

2. **Device Management**
   - Remember previously discovered devices, update their info if rescan option selected after each scan.
   - Store and recall login credentials per device
   - Quick access to device configuration pages

3. **Dual Interface**
   - Full-featured command line interface (CLI)
   - Graphical user interface (GUI) using WPF
   - All features accessible through both interfaces

4. **GUI Theming**
   - Follow Windows system theme by default (light/dark mode)
   - Use Windows accent colors for highlights and interactive elements
   - Respond to real-time theme changes from Windows
   - User override options in Settings:
     - Theme: System (default), Light, Dark
     - Accent Color: System (default), or custom color picker
   - Persist theme preferences across sessions

5. **Splash Screen**
   - Display on application startup
   - Visual network topology diagram (620x300)
   - Application information:
     - Project name and version (auto-populated from assembly)
     - Repository link (clickable)
     - Author name and email
     - License information
   - Follows Windows system theme (light/dark mode)
   - Uses Windows accent color
   - Click anywhere to dismiss immediately
   - Auto-dismiss after configurable timeout (default: 5 seconds)
   - Progress bar showing remaining time

### Scanning Behavior

1. **Startup Scan**
   - On launch, automatically scan for new devices not in the saved device list
   - Display previously known devices immediately from saved data
   - Highlight newly discovered devices

2. **Rescan Option**
   - User can trigger a full rescan of all IPs
   - Updates existing device information (ports, availability)
   - Marks devices as offline if not responding

3. **Port Detection**
   - Scan configurable list of known ports on each discovered IP
   - Identify service type based on port and response
   - Generate clickable links for web-accessible ports (HTTP/HTTPS)

### Documentation

- Standard CLI usage with `--help` documentation for each feature
- GUI help file accessible via File > Help menu

## Technical Requirements

### Platform

- **Target:** Windows 10/11
- **Future consideration:** Cross-platform port if demand warrants

### Technology Stack

- **Framework:** .NET 10.0
- **Language:** C#
- **GUI:** WPF (Windows Presentation Foundation)
- **CLI:** System.CommandLine
- **Versioning:** MinVer (automatic Git-based SemVer)

### Versioning Strategy

Uses [MinVer](https://github.com/adamralph/minver) for automatic semantic versioning based on Git tags:

| Scenario | Version Example |
|----------|-----------------|
| Tagged release `v1.0.0` | `1.0.0` |
| 3 commits after `v1.0.0` | `1.0.1-alpha.0.3` |
| No tags yet | `0.0.0-alpha.0.1` |

**Usage:**
- Create a release: `git tag v1.0.0 && git push --tags`
- Version auto-increments patch for commits after a tag
- Pre-release suffix added automatically between tags

### Libraries

| Purpose | Library |
|---------|---------|
| Network scanning | SharpPcap, System.Net |
| CLI parsing | System.CommandLine |
| Credential storage | Microsoft.Extensions.SecretManager |
| Data persistence | JSON files (System.Text.Json) |
| Logging | Microsoft.Extensions.Logging |
| Windows theming | WinRT APIs (via net10.0-windows10.0.19041.0) |

### Windows Theme Integration

- Target `net10.0-windows10.0.19041.0` for direct WinRT API access
- Detect system theme via `Windows.UI.ViewManagement.UISettings`
- Read accent color from `UISettings.GetColorValue(UIColorType.Accent)`
- Listen for theme changes via `UISettings.ColorValuesChanged` event
- Check dark mode via registry key `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme`
- Use WPF DynamicResource for theme-aware styling
- Implement custom ResourceDictionary for Light/Dark themes

### Project Structure

```
IPScan/
├── .vscode/                    # VS Code configuration
│   ├── launch.json
│   ├── tasks.json
│   └── settings.json
├── src/
│   ├── IPScan.Core/            # Shared business logic
│   │   ├── Models/             # Data models
│   │   ├── Services/           # Scanning, device detection
│   │   └── Storage/            # JSON persistence, credentials
│   ├── IPScan.CLI/             # Command line interface
│   └── IPScan.GUI/             # WPF application
├── tests/
│   ├── IPScan.Core.Tests/
│   └── IPScan.CLI.Tests/
├── docs/                       # Documentation
├── Directory.Build.props       # Shared build properties & versioning
├── IPScan.sln
├── README.md
├── REQUIREMENTS.md
└── LICENSE
```

## Non-Functional Requirements

### Performance

- Network scan should complete within reasonable time for /24 subnet
- Responsive UI during scanning operations (async/background tasks)

### Security

- Credentials stored using Windows Credential Manager via SecretManager
- No plaintext password storage
- Device data stored in user's AppData folder

### Quality Assurance

- Unit tests for core scanning and detection logic
- Integration tests for CLI commands
- Automated testing via `dotnet test`

## Data Storage

### Device Data (JSON)

Location: `%APPDATA%\IPScan\devices.json`

```json
{
  "devices": [
    {
      "id": "guid",
      "name": "Router",
      "ipAddress": "192.168.1.1",
      "macAddress": "AA:BB:CC:DD:EE:FF",
      "deviceType": "Router",
      "ports": [
        { "port": 80, "protocol": "http", "service": "Web Interface" },
        { "port": 443, "protocol": "https", "service": "Web Interface (Secure)" },
        { "port": 22, "protocol": "ssh", "service": "SSH" }
      ],
      "firstDiscovered": "2026-01-20T08:00:00Z",
      "lastSeen": "2026-01-25T10:30:00Z",
      "notes": ""
    }
  ]
}
```

### Application Settings (JSON)

Location: `%APPDATA%\IPScan\settings.json`

```json
{
  "scanOnStartup": true,
  "subnet": "auto",
  "customSubnet": "",
  "knownPorts": [21, 22, 23, 80, 443, 8080, 8443, 3389, 5900],
  "scanTimeoutMs": 1000,
  "rescanExistingDevices": false,
  "splashTimeoutSeconds": 5,
  "theme": {
    "mode": "system",
    "accentColor": "system"
  }
}
```

### Splash Screen Settings

| Setting | Default | Description |
|---------|---------|-------------|
| `splashTimeoutSeconds` | `5` | Auto-continue delay (0 to disable timeout) |

### Theme Settings

| Setting | Values | Description |
|---------|--------|-------------|
| `mode` | `system`, `light`, `dark` | Controls light/dark appearance |
| `accentColor` | `system` or hex color (e.g., `#0078D4`) | Accent color for highlights |

### Known Ports (Default Scan List)

| Port | Service |
|------|---------|
| 21 | FTP |
| 22 | SSH |
| 23 | Telnet |
| 80 | HTTP |
| 443 | HTTPS |
| 8080 | HTTP Alternate |
| 8443 | HTTPS Alternate |
| 3389 | RDP |
| 5900 | VNC |

## Future Considerations

- Cross-platform support (Linux, macOS) via Avalonia UI
- Device grouping/categorization
- Network topology visualization
- Scheduled scanning
- Export/import device lists
