# IPScan Requirements Specification

## Overview

IPScan is a Windows network device discovery tool that locates HTTP-enabled devices on the local subnet and presents them in an accessible interface for configuration.

## Implementation Status

### Completed Features

#### Core Functionality ✓
- **Network Scanning**: Async ping sweep of subnet with concurrent operations (configurable, default 100)
- **Device Discovery**: Automatic detection and storage of devices with IP, hostname, MAC, response time
- **Device Persistence**: JSON-based storage in `%APPDATA%\IPScan\devices.json`
- **Settings Management**: JSON-based configuration in `%APPDATA%\IPScan\settings.json`
- **Real-time Updates**: Live device list population during scans with progress tracking

#### GUI Features ✓
- **Splash Screen**: Auto-dismiss with configurable timeout, theme-aware, displays version/author info
- **Main Window**: Responsive WPF interface with device tree, details panel, and status bar
- **Theme System**: Automatic dark/light mode detection following Windows theme
- **Dynamic Theming**: Real-time response to Windows theme changes with accent color integration
- **Device Tree View**: Hierarchical display grouped by online/offline status
- **Search/Filter**: Real-time search across device name, IP, hostname, and MAC address
- **Device Details**: Show/hide offline devices toggle, device selection with detail panel
- **Progress Display**: Real-time scan progress with percentage and device count

#### Window Management ✓
- **Smart Sizing**: Initial window size adapts to screen (80% of working area, max 1400x900)
- **Position Memory**: Saves and restores window position, size, and maximized state
- **Multi-Monitor Support**: Remembers which monitor window was displayed on
- **Monitor Fallback**: Falls back to primary monitor if saved monitor is unavailable
- **Position Validation**: Ensures window is visible on screen before restoring position
- **Startup Modes**:
  - `RememberLast`: Restore last position/size/state (default)
  - `AlwaysMaximized`: Always start maximized
  - `DefaultCentered`: Center with smart default size
  - `SpecificMonitor`: Start on preferred monitor

### In Progress

#### GUI Dialogs
- Settings dialog (access via menu/toolbar)
- Edit device dialog (modify name, notes)
- Export/Import devices (JSON file operations)

#### Planned Features
- Port scanning and service detection
- Device categorization by hardware type
- Connection type detection (wired/wireless)
- Credentials management
- CLI interface

## Functional Requirements

### Core Features

1. **Network Scanning**
   - Upon startup Scan local or defined subnet for new IP devices not previously discovered.
   - Automatically detect and identify device types (Router, Switch, Server, etc.)
   - Automatically detect known ports on each device.
   - Present discovered devices in a hierarchical view with name, IP, device type, ports, etc...
   - Include links to access devices and ports as appropriate.
   - 

2. **Device Management**
   - Remember previously discovered devices, update their info if rescan option selected after each scan.
   - Store and recall login credentials per device
   - Quick access to device configuration pages

3. **Dual Interface**
   - Full-featured command line interface (CLI)
   - Graphical user interface (GUI) using WPF
   - All features accessible through both interfaces
   - Display GUI if no options on command line
   - Option to display GUI with command line options set.
   - Update settings from CLI or GUI.

4. **GUI Theming**
   - Splash screen, logo.png, and windows icons should have a toned down iconic CRT green theme, medium-contrast, monochrome display style of 1970s-80s computer terminals, typically using P1 phosphor for a bright green, "glowing" look on a black background.
   - Use three or more shades of green with additional accent colors as propriate to make information readable and distinct where emphisis is needed.
   - Use iconic CRT green theme for highlights and interactive elements with settings to use Windows accent colors 
   - Respond to real-time theme changes from Windows
   - User override options in Settings:
     - Theme: Windows System, Light, Dark, iconic CRT (Default)
     - Accent Color: iconic CRT, System (default), or user custom color picker
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

6. **Device Categorization**
   - Categorize devices by hardware type and detected services
   - Filter/show/hide devices by category in GUI
   - Differentiate between wired and wireless connections
   - User can override auto-detected categories
   - Categories persist across sessions

   **Connection Types:**
   | Type | Description | Detection Method |
   |------|-------------|------------------|
   | Wired | Ethernet-connected devices | MAC OUI (network adapters), switch port detection via SNMP/LLDP |
   | Wireless | Wi-Fi connected devices | MAC OUI (wireless adapters), router wireless client list |
   | Unknown | Connection type undetermined | Default when detection not possible |

   **Hardware Categories:**
   | Category | Description | Detection Method |
   |----------|-------------|------------------|
   | Network Infrastructure | Routers, switches, access points, firewalls, modems | MAC OUI, ports 22/23/80/443, SNMP (161) |
   | Servers | Physical/virtual servers, NAS devices | Multiple service ports, hostname patterns |
   | Workstations | Desktop PCs, laptops | RDP (3389), SMB (445), hostname |
   | IoT Devices | Smart home devices, sensors, cameras | MQTT, CoAP, mDNS, specific ports |
   | Mobile Devices | Phones, tablets | mDNS, AirPlay, limited open ports, wireless connection |
   | Printers | Network printers, print servers | Ports 515, 631, 9100, mDNS _printer._tcp |
   | Media Devices | Smart TVs, streaming devices, speakers | DLNA (1900), AirPlay, Chromecast ports |
   | Access Points | Wireless access points | MAC OUI, management ports, wireless indicators |
   | Unknown | Unclassified devices | Default category |

   **Service Categories:**
   | Category | Description | Example Services |
   |----------|-------------|------------------|
   | Media & Entertainment | Streaming, media servers | Plex, Jellyfin, Emby, DLNA |
   | Home Automation | Smart home control | Home Assistant, OpenHAB, Hubitat |
   | Storage & Backup | File storage, NAS | SMB, NFS, AFP, Synology, UNRAID |
   | Security & Surveillance | Cameras, NVR, access control | RTSP, ONVIF, Blue Iris |
   | Network Services | DNS, DHCP, VPN | Pi-hole, AdGuard, WireGuard |
   | Virtualization | Hypervisors, containers | Proxmox, ESXi, Docker, Portainer |
   | Database | Database servers | MySQL, PostgreSQL, MongoDB |
   | Web Services | Web servers, reverse proxies | Nginx, Apache, Traefik, Caddy |
   | Communication | Chat, email, VoIP | Matrix, XMPP, SIP |
   | Development | Dev tools, CI/CD | Git, Jenkins, Gitea |
   | Monitoring | System monitoring | Grafana, Prometheus, Netdata |
   | Photo & Documents | Photo management, documents | Immich, PhotoPrism, Paperless |

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
- All code should implement null checking and arguement validation techniques to ensure data validity to prevent runtime crashes.

## Data Storage

### Device Data (JSON)

Location: `%APPDATA%\IPScan\devices.json`

```json
{
  "devices": [
    {
      "id": "guid",
      "name": "Router",
      "hostname": "router.local",
      "ipAddress": "192.168.1.1",
      "macAddress": "AA:BB:CC:DD:EE:FF",
      "manufacturer": "Cisco",
      "hardwareCategory": "NetworkInfrastructure",
      "serviceCategories": ["NetworkServices"],
      "connectionType": "Wired",
      "isOnline": true,
      "ports": [
        { "port": 80, "protocol": "tcp", "service": "HTTP", "serviceCategory": "Web", "url": "http://192.168.1.1" },
        { "port": 443, "protocol": "tcp", "service": "HTTPS", "serviceCategory": "Web", "url": "https://192.168.1.1" },
        { "port": 22, "protocol": "tcp", "service": "SSH", "serviceCategory": "RemoteAccess" }
      ],
      "firstDiscovered": "2026-01-20T08:00:00Z",
      "lastSeen": "2026-01-25T10:30:00Z",
      "userOverrides": {
        "name": null,
        "hardwareCategory": null,
        "connectionType": null
      },
      "notes": ""
    }
  ]
}
```

#### Device Properties

| Property | Type | Description |
|----------|------|-------------|
| `id` | GUID | Unique device identifier |
| `name` | string | Display name (auto-detected or user-assigned) |
| `hostname` | string | DNS/mDNS hostname if discovered |
| `ipAddress` | string | IPv4 address |
| `macAddress` | string | MAC address (for OUI lookup) |
| `manufacturer` | string | Manufacturer from MAC OUI database |
| `hardwareCategory` | enum | Hardware type category |
| `serviceCategories` | array | Detected service categories |
| `connectionType` | enum | Wired, Wireless, or Unknown |
| `isOnline` | bool | Current online status |
| `ports` | array | Open ports with service info |
| `userOverrides` | object | User-specified overrides for auto-detected values |
| `notes` | string | User notes |

#### Connection Type Detection

| Type | Detection Method |
|------|------------------|
| **Wired** | MAC OUI indicates switch port, no wireless indicators |
| **Wireless** | MAC OUI indicates Wi-Fi adapter, mDNS wireless hints, router ARP table |
| **Unknown** | Cannot determine connection type |

*Note: Accurate wired/wireless detection may require router integration or SNMP access to network infrastructure.*

### Application Settings (JSON)

Location: `%APPDATA%\IPScan\settings.json`

```json
{
  "scanOnStartup": true,
  "subnet": "auto",
  "customSubnet": "",
  "preferredInterfaceId": "",
  "scanTimeoutMs": 1000,
  "maxConcurrentScans": 100,
  "autoRemoveMissingDevices": false,
  "missedScansBeforeRemoval": 5,
  "showOfflineDevices": true,
  "splashTimeoutSeconds": 5,
  "windowStartup": "RememberLast",
  "preferredMonitor": "",
  "lastWindowSettings": {
    "left": 100,
    "top": 100,
    "width": 1200,
    "height": 800,
    "isMaximized": false,
    "monitorDeviceName": "\\\\.\\DISPLAY1"
  },
  "categoryVisibility": {
    "hardware": {
      "NetworkInfrastructure": true,
      "Servers": true,
      "Workstations": true,
      "IoTDevices": true,
      "MobileDevices": true,
      "Printers": true,
      "MediaDevices": true,
      "AccessPoints": true,
      "Unknown": true
    },
    "services": {
      "MediaEntertainment": true,
      "HomeAutomation": true,
      "StorageBackup": true,
      "SecuritySurveillance": true,
      "NetworkServices": true,
      "Virtualization": true,
      "Database": true,
      "WebServices": true,
      "Communication": true,
      "Development": true,
      "Monitoring": true,
      "PhotoDocuments": true
    },
    "connectionType": {
      "Wired": true,
      "Wireless": true,
      "Unknown": true
    }
  },
  "showOfflineDevices": true
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

Ports are organized by category. The default scan includes commonly used ports for network device discovery and management. Reference: [Wikipedia - List of TCP and UDP port numbers](https://en.wikipedia.org/wiki/List_of_TCP_and_UDP_port_numbers)

#### Web Services

| Port | Protocol | Service | Description |
|------|----------|---------|-------------|
| 80 | TCP | HTTP | Hypertext Transfer Protocol |
| 443 | TCP | HTTPS | HTTP over TLS/SSL |
| 8080 | TCP | HTTP-Alt | Alternative HTTP port (proxies, dev servers) |
| 8443 | TCP | HTTPS-Alt | Alternative HTTPS port |
| 8000 | TCP | HTTP-Alt | Alternative HTTP port |
| 8888 | TCP | HTTP-Alt | Alternative HTTP port |

#### Remote Access & Administration

| Port | Protocol | Service | Description |
|------|----------|---------|-------------|
| 22 | TCP | SSH | Secure Shell, secure logins, file transfers (scp, sftp) |
| 23 | TCP | Telnet | Telnet protocol (unencrypted) |
| 135 | TCP | MS-EPMAP | Microsoft EPMAP/DCE RPC Locator service |
| 3389 | TCP | RDP | Remote Desktop Protocol (Microsoft Terminal Server) |
| 5900 | TCP | VNC | Virtual Network Computing |
| 5901-5909 | TCP | VNC | Additional VNC displays |

#### File Transfer & Sharing

| Port | Protocol | Service | Description |
|------|----------|---------|-------------|
| 20 | TCP | FTP-Data | FTP data transfer |
| 21 | TCP | FTP | FTP control/command |
| 69 | UDP | TFTP | Trivial File Transfer Protocol |
| 137 | UDP | NetBIOS-NS | NetBIOS Name Service |
| 138 | UDP | NetBIOS-DGM | NetBIOS Datagram Service |
| 139 | TCP | NetBIOS-SSN | NetBIOS Session Service |
| 445 | TCP | SMB | Server Message Block / CIFS (file sharing) |
| 548 | TCP | AFP | Apple Filing Protocol |
| 873 | TCP | rsync | rsync file synchronization |
| 2049 | TCP/UDP | NFS | Network File System |

#### Network Management & Monitoring

| Port | Protocol | Service | Description |
|------|----------|---------|-------------|
| 161 | UDP | SNMP | Simple Network Management Protocol |
| 162 | UDP | SNMP-Trap | SNMP trap messages |
| 199 | TCP | SMUX | SNMP Unix Multiplexer |
| 514 | UDP | Syslog | System logging |
| 601 | TCP | Syslog-TLS | Reliable Syslog Service |
| 830 | TCP | NETCONF-SSH | NETCONF over SSH |
| 5060 | TCP/UDP | SIP | Session Initiation Protocol |
| 5061 | TCP | SIP-TLS | SIP over TLS |

#### Network Infrastructure

| Port | Protocol | Service | Description |
|------|----------|---------|-------------|
| 53 | TCP/UDP | DNS | Domain Name System |
| 67 | UDP | DHCP-Server | DHCP server (BOOTP) |
| 68 | UDP | DHCP-Client | DHCP client (BOOTP) |
| 88 | TCP/UDP | Kerberos | Kerberos authentication |
| 123 | UDP | NTP | Network Time Protocol |
| 179 | TCP | BGP | Border Gateway Protocol |
| 389 | TCP | LDAP | Lightweight Directory Access Protocol |
| 464 | TCP/UDP | Kpasswd | Kerberos password change |
| 636 | TCP | LDAPS | LDAP over TLS/SSL |
| 853 | TCP | DoT | DNS over TLS |
| 1812 | UDP | RADIUS | RADIUS authentication |
| 1813 | UDP | RADIUS-Acct | RADIUS accounting |

#### VPN & Security

| Port | Protocol | Service | Description |
|------|----------|---------|-------------|
| 500 | UDP | IKE | Internet Key Exchange (IPSec) |
| 1194 | UDP | OpenVPN | OpenVPN |
| 1701 | UDP | L2TP | Layer 2 Tunneling Protocol |
| 1723 | TCP | PPTP | Point-to-Point Tunneling Protocol |
| 4500 | UDP | IPSec-NAT | IPSec NAT Traversal |

#### Email Services

| Port | Protocol | Service | Description |
|------|----------|---------|-------------|
| 25 | TCP | SMTP | Simple Mail Transfer Protocol |
| 110 | TCP | POP3 | Post Office Protocol v3 |
| 143 | TCP | IMAP | Internet Message Access Protocol |
| 465 | TCP | SMTPS | SMTP over TLS/SSL (implicit) |
| 587 | TCP | SMTP-Submit | SMTP submission (STARTTLS) |
| 993 | TCP | IMAPS | IMAP over TLS/SSL |
| 995 | TCP | POP3S | POP3 over TLS/SSL |

#### Database Services

| Port | Protocol | Service | Description |
|------|----------|---------|-------------|
| 1433 | TCP | MS-SQL | Microsoft SQL Server |
| 1434 | UDP | MS-SQL-Mon | Microsoft SQL Server Monitor |
| 1521 | TCP | Oracle | Oracle Database listener |
| 3306 | TCP | MySQL | MySQL/MariaDB |
| 5432 | TCP | PostgreSQL | PostgreSQL Database |
| 6379 | TCP | Redis | Redis key-value store |
| 27017 | TCP | MongoDB | MongoDB NoSQL database |

#### Virtualization & Cloud

| Port | Protocol | Service | Description |
|------|----------|---------|-------------|
| 902 | TCP | VMware | VMware ESXi |
| 903 | TCP | VMware | VMware ESXi remote console |
| 2375 | TCP | Docker | Docker REST API (plain) |
| 2376 | TCP | Docker | Docker REST API (SSL) |
| 5985 | TCP | WinRM | Windows Remote Management (HTTP) |
| 5986 | TCP | WinRM-S | Windows Remote Management (HTTPS) |

#### Apple Services

| Port | Protocol | Service | Description |
|------|----------|---------|-------------|
| 311 | TCP | AppleShare | macOS Server Admin |
| 548 | TCP | AFP | Apple Filing Protocol |
| 5353 | UDP | mDNS | Multicast DNS (Bonjour) |
| 5900 | TCP | ARD | Apple Remote Desktop |

#### Discovery Protocols

| Port | Protocol | Service | Description |
|------|----------|---------|-------------|
| 1900 | UDP | SSDP | Simple Service Discovery Protocol (UPnP) |
| 3702 | TCP/UDP | WS-Discovery | Web Services Dynamic Discovery |
| 5353 | UDP | mDNS | Multicast DNS / Bonjour |
| 5355 | TCP/UDP | LLMNR | Link-Local Multicast Name Resolution |

#### IoT & Messaging

| Port | Protocol | Service | Description |
|------|----------|---------|-------------|
| 1883 | TCP | MQTT | Message Queuing Telemetry Transport |
| 5222 | TCP | XMPP | Extensible Messaging and Presence Protocol |
| 5269 | TCP | XMPP-S2S | XMPP server-to-server |
| 5683 | UDP | CoAP | Constrained Application Protocol |
| 8883 | TCP | MQTT-TLS | MQTT over TLS/SSL |

#### Printing

| Port | Protocol | Service | Description |
|------|----------|---------|-------------|
| 515 | TCP | LPD | Line Printer Daemon |
| 631 | TCP | IPP | Internet Printing Protocol (CUPS) |
| 9100 | TCP | JetDirect | HP JetDirect raw printing |

#### Media & Streaming

| Port | Protocol | Service | Description |
|------|----------|---------|-------------|
| 554 | TCP | RTSP | Real Time Streaming Protocol |
| 1935 | TCP | RTMP | Real Time Messaging Protocol (Flash) |
| 3689 | TCP | DAAP | Digital Audio Access Protocol (iTunes) |
| 5004 | TCP/UDP | RTP | Real-time Transport Protocol |
| 5005 | TCP/UDP | RTCP | RTP Control Protocol |
| 7359 | UDP | Jellyfin | Jellyfin auto-discovery |
| 8096 | TCP | Jellyfin | Jellyfin HTTP |
| 8920 | TCP | Jellyfin | Jellyfin HTTPS |
| 32400 | TCP | Plex | Plex Media Server |
| 32410-32414 | UDP | Plex | Plex GDM network discovery |
| 32469 | TCP | Plex | Plex DLNA |
| 8096 | TCP | Emby | Emby HTTP |
| 8920 | TCP | Emby | Emby HTTPS |
| 8200 | TCP | Trivial | MiniDLNA/ReadyMedia |

#### Home Automation

| Port | Protocol | Service | Description |
|------|----------|---------|-------------|
| 8123 | TCP | Home Assistant | Home Assistant web interface |
| 1400 | TCP | Sonos | Sonos speaker control |
| 1443 | TCP | Sonos | Sonos speaker control (secure) |
| 6053 | TCP | ESPHome | ESPHome native API |
| 8080 | TCP | OpenHAB | OpenHAB web interface |
| 8443 | TCP | OpenHAB | OpenHAB HTTPS |
| 80 | TCP | Hubitat | Hubitat Elevation |
| 39500 | TCP | Hubitat | Hubitat Maker API |
| 8581 | TCP | Homebridge | Homebridge web UI |
| 51826 | TCP | HAP | HomeKit Accessory Protocol |
| 5540 | TCP | Matter | Matter protocol |

#### Smart Home Bridges & IoT Hubs

| Port | Protocol | Service | Description |
|------|----------|---------|-------------|
| 80/443 | TCP | Philips Hue | Hue Bridge web interface |
| 8080 | TCP | Zigbee2MQTT | Zigbee2MQTT web interface |
| 8081 | TCP | ZwaveJS | Z-Wave JS UI |
| 21063 | TCP | Zigbee | deCONZ Phoscon |
| 3000 | TCP | Zigbee | Zigbee2MQTT frontend |
| 6638 | TCP | Z-Wave | Z-Wave JS websocket |
| 8091 | TCP | SmartThings | SmartThings Hub |
| 9999 | TCP | TP-Link | TP-Link Kasa smart devices |
| 10001 | UDP | Ubiquiti | Ubiquiti device discovery |
| 10443 | TCP | Ubiquiti | Ubiquiti UniFi inform |

#### NAS & Storage Platforms

| Port | Protocol | Service | Description |
|------|----------|---------|-------------|
| 5000 | TCP | Synology | Synology DSM HTTP |
| 5001 | TCP | Synology | Synology DSM HTTPS |
| 6690 | TCP | Synology | Synology Drive |
| 80/443 | TCP | UNRAID | UNRAID web interface |
| 8080 | TCP | QNAP | QNAP QTS HTTP |
| 443 | TCP | QNAP | QNAP QTS HTTPS |
| 80/443 | TCP | TrueNAS | TrueNAS web interface |
| 3260 | TCP | iSCSI | iSCSI target |
| 111 | TCP/UDP | rpcbind | NFS portmapper |
| 2049 | TCP/UDP | NFS | Network File System |

#### Photo & Document Management

| Port | Protocol | Service | Description |
|------|----------|---------|-------------|
| 2283 | TCP | Immich | Immich photo management |
| 2342 | TCP | PhotoPrism | PhotoPrism web UI |
| 8000 | TCP | Paperless | Paperless-ngx |
| 3000 | TCP | Nextcloud | Nextcloud (when not 80/443) |

#### Security & Surveillance

| Port | Protocol | Service | Description |
|------|----------|---------|-------------|
| 554 | TCP | RTSP | IP camera streaming |
| 8554 | TCP | RTSP-Alt | Alternative RTSP port |
| 37777 | TCP | Dahua | Dahua cameras/NVR |
| 34567 | TCP | XMEye | XMEye DVR/NVR |
| 80/443 | TCP | Hikvision | Hikvision cameras |
| 8000 | TCP | Hikvision | Hikvision SDK |
| 81 | TCP | Amcrest | Amcrest cameras |
| 7443 | TCP | UniFi Protect | UniFi Protect |
| 7447 | TCP | UniFi Protect | UniFi Protect RTSP |
| 8081 | TCP | Frigate | Frigate NVR |
| 5000 | TCP | Frigate | Frigate API |
| 7474 | TCP | Blue Iris | Blue Iris web server |

#### Monitoring & Management

| Port | Protocol | Service | Description |
|------|----------|---------|-------------|
| 3000 | TCP | Grafana | Grafana dashboards |
| 9090 | TCP | Prometheus | Prometheus metrics |
| 9093 | TCP | Alertmanager | Prometheus Alertmanager |
| 19999 | TCP | Netdata | Netdata real-time monitoring |
| 8086 | TCP | InfluxDB | InfluxDB time-series database |
| 61208 | TCP | Glances | Glances system monitor |
| 9001 | TCP | Portainer | Portainer Docker management |
| 8006 | TCP | Proxmox | Proxmox VE web interface |
| 8007 | TCP | Proxmox | Proxmox Backup Server |
| 53 | TCP | Pi-hole | Pi-hole DNS |
| 80 | TCP | Pi-hole | Pi-hole web admin |
| 3000 | TCP | AdGuard | AdGuard Home |
| 8080 | TCP | Traefik | Traefik dashboard |

#### Container & Orchestration

| Port | Protocol | Service | Description |
|------|----------|---------|-------------|
| 2375 | TCP | Docker | Docker API (unencrypted) |
| 2376 | TCP | Docker | Docker API (TLS) |
| 2377 | TCP | Docker Swarm | Swarm cluster management |
| 9000 | TCP | Portainer | Portainer (legacy) |
| 9001 | TCP | Portainer | Portainer agent |
| 6443 | TCP | Kubernetes | Kubernetes API server |
| 10250 | TCP | Kubelet | Kubernetes Kubelet API |
| 8001 | TCP | kubectl | kubectl proxy |

#### Communication & Collaboration

| Port | Protocol | Service | Description |
|------|----------|---------|-------------|
| 8448 | TCP | Matrix | Matrix federation |
| 8008 | TCP | Matrix | Matrix Synapse |
| 6697 | TCP | IRC | IRC over TLS |
| 5222 | TCP | XMPP | XMPP client |
| 5269 | TCP | XMPP | XMPP server federation |
| 64738 | TCP/UDP | Mumble | Mumble voice chat |
| 9987 | UDP | TeamSpeak | TeamSpeak voice |
| 30033 | TCP | TeamSpeak | TeamSpeak file transfer |

#### Default Scan Ports

The application scans these ports by default (can be customized in settings):

```
21, 22, 23, 53, 80, 135, 139, 161, 389, 443, 445, 515, 548, 554, 631, 902,
1433, 1521, 1883, 1900, 2283, 3000, 3306, 3389, 5000, 5001, 5353, 5432,
5900, 8006, 8080, 8096, 8123, 8443, 9000, 9001, 9100, 19999, 32400
```

**Port categories in default scan:**
- **Core network:** 21, 22, 23, 53, 80, 135, 139, 161, 389, 443, 445
- **Printing:** 515, 631, 9100
- **Media services:** 548, 554, 8096, 32400 (AFP, RTSP, Jellyfin, Plex)
- **Home automation:** 1883, 8123 (MQTT, Home Assistant)
- **Discovery:** 1900, 5353 (UPnP, mDNS)
- **NAS/Storage:** 5000, 5001 (Synology)
- **Databases:** 1433, 3306, 5432 (MSSQL, MySQL, PostgreSQL)
- **Remote access:** 3389, 5900 (RDP, VNC)
- **Virtualization:** 902, 8006 (VMware, Proxmox)
- **Containers:** 9000, 9001 (Portainer)
- **Photo management:** 2283 (Immich)
- **Web apps:** 3000, 8080, 8443
- **Monitoring:** 19999 (Netdata)

## Future Considerations

- Cross-platform support (Linux, macOS) via Avalonia UI
- Network topology visualization
- Scheduled scanning
- Export/import device lists
- SNMP-based switch port mapping for accurate wired/wireless detection
- Router API integration for wireless client detection
- LLDP/CDP neighbor discovery
