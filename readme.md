# IPScan

A Windows CLI and GUI tool for discovering and managing HTTP-enabled devices on your local network.

## Features

- **Auto-Discovery** - Automatically scans for new devices on startup
- **Port Scanning** - Detects common services (HTTP, HTTPS, SSH, RDP, etc.) on each device
- **Device Identification** - Automatically detect device types (routers, switches, servers, etc.)
- **Clickable Links** - Quick access to device web interfaces and services
- **Credential Management** - Securely store and recall login credentials per device
- **Dual Interface** - Full functionality via both command line and graphical interface

## Requirements

- Windows 10 or later
- .NET 10.0 Runtime

## Installation

### From Source

```bash
git clone https://github.com/HerbHall/IPScan.git
cd IPScan
dotnet build
```

### Releases

Download the latest release from the [Releases](https://github.com/HerbHall/IPScan/releases) page.

## Usage

### Command Line

```bash
# Launch GUI (no arguments)
ipscan

# Scan for new devices on the local network
ipscan scan

# Scan a specific subnet
ipscan scan --subnet 192.168.1.0/24

# Rescan all devices (update existing device info)
ipscan scan --rescan

# Scan and then open GUI
ipscan scan --gui

# List saved devices
ipscan list

# Show device details including open ports
ipscan show <device-name>

# Open a device's web interface
ipscan open <device-name>

# View/modify settings
ipscan settings get              # Show all settings
ipscan settings get scanOnStartup  # Show specific setting
ipscan settings set splashTimeoutSeconds 3

# Explicitly launch GUI
ipscan gui

# View all commands
ipscan --help
```

### GUI

Launch the GUI by running `ipscan` without arguments, or explicitly:

```bash
ipscan gui
```

Or run from the Start Menu after installation.

Access help documentation via **File > Help** in the menu.

## Configuration

Configuration and data files are stored in:

```
%APPDATA%\IPScan\
├── devices.json      # Saved device information
└── settings.json     # Application settings
```

## Security

Device credentials are securely stored using Windows Credential Manager.

## Building

### Prerequisites

- .NET 10.0 SDK
- Visual Studio Code (recommended) or Visual Studio 2022

### Build Commands

```bash
# Restore dependencies
dotnet restore

# Build all projects
dotnet build

# Run tests
dotnet test

# Publish release build
dotnet publish -c Release
```

### Project Structure

```
IPScan/
├── src/
│   ├── IPScan.Core/     # Shared business logic
│   ├── IPScan.CLI/      # Command line interface
│   └── IPScan.GUI/      # WPF application
├── tests/
│   ├── IPScan.Core.Tests/
│   └── IPScan.CLI.Tests/
└── docs/
```

## Contributing

Contributions are welcome! Please read the [REQUIREMENTS.md](REQUIREMENTS.md) for project specifications.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Tech Stack

- .NET 10.0
- WPF (Windows Presentation Foundation)
- SharpPcap for network scanning
- System.CommandLine for CLI
