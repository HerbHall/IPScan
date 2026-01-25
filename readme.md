# IPScan

A cross-platform CLI and GUI tool for discovering and managing HTTP-enabled devices on your local network.

## Features

- **Network Discovery** - Scan your local subnet for devices with HTTP interfaces
- **Device Identification** - Automatically detect device types (routers, switches, servers, etc.)
- **Credential Management** - Securely store and recall login credentials per device
- **Dual Interface** - Full functionality via both command line and graphical interface
- **Cross-Platform** - Runs on Windows, Linux, and Android

## Installation

### Prerequisites

- .NET 8.0 or later

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
# Scan the local network
ipscan scan

# Scan a specific subnet
ipscan scan --subnet 192.168.1.0/24

# List saved devices
ipscan list

# Open a device's web interface
ipscan open <device-name>

# View all commands
ipscan --help
```

### GUI

Launch the application without arguments to open the graphical interface:

```bash
ipscan
```

Access help documentation via **File > Help** in the menu.

## Configuration

Configuration files are stored in:

- **Windows:** `%APPDATA%\IPScan\`
- **Linux:** `~/.config/IPScan/`

## Security

Device credentials are encrypted using platform-native secure storage:

- **Windows:** Windows Credential Manager
- **Linux:** Secret Service API / libsecret

## Building

```bash
# Build for current platform
dotnet build

# Run tests
dotnet test

# Publish release build
dotnet publish -c Release
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

## Acknowledgments

- Built with .NET
