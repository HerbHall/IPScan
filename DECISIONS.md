# IPScan Implementation Decisions

**Document Purpose**: Actionable summary of all design decisions from REQUIREMENTS_QUESTIONS.md
**Status**: Final - Ready for Implementation
**Date**: 2026-01-26
**Related Documents**: [requirements.md](requirements.md), [REQUIREMENTS_QUESTIONS.md](REQUIREMENTS_QUESTIONS.md)

---

## Executive Summary

**Project Philosophy**: User flexibility and customization is the top priority. The application should feel "light and fast" with optional time-consuming work controlled by the user.

**MVP Scope**: Full Phase 1 (Option C) - includes ping sweep, port scanning, device categorization, and MAC manufacturer lookup. **No deadline - completeness over speed.**

---

## Phase 1: Core Implementation (MVP)

### 1. Network Interface Handling

**Decision**: Hybrid approach starting with primary interface

**Implementation**:
```
1. Auto-detect and scan primary interface (with default gateway) first
2. Exclude VPN interfaces by default (identify but don't scan)
3. After primary scan completes, IF other non-VPN interfaces exist:
   - Prompt user: "Additional interfaces detected. Scan them too?"
   - Show interface names/types
   - Let user select one or more to scan
4. Remember user's interface preferences in settings
```

**Key Files**:
- `NetworkInterfaceService.cs`: Interface detection and VPN filtering
- `NetworkScanner.cs`: Scan workflow with user prompts
- Settings UI: Interface selection options

**User Experience Goal**: Fast initial scan, optional comprehensive coverage

---

### 2. Storage and Data Persistence

**Decision**: Hybrid memory-only mode with user choice

**Implementation**:
```
On startup:
1. Try to create/access %APPDATA%\IPScan
2. If it fails (permissions, disk full, network drive):
   a. Run in memory-only mode (no persistence)
   b. Show persistent warning banner: "Running in memory-only mode - data will not be saved"
   c. Prompt user: "Storage unavailable. Choose:"
      - "Use local directory" (save next to exe)
      - "Continue without saving" (memory-only)
   d. Save user's choice in registry for next launch
```

**Key Files**:
- `JsonSettingsService.cs`: Fallback logic
- `JsonDeviceRepository.cs`: Memory-only mode support
- GUI: Warning banner component

**User Experience Goal**: Maximum flexibility, clear communication

---

### 3. Administrative Privileges

**Decision**: Always require administrator elevation

**Implementation**:
```
1. Add requireAdministrator to app.manifest for both GUI and CLI
2. Application always runs elevated
3. No graceful degradation - full network capabilities always available
```

**Rationale**: "Get it out of the way up front" - simplifies development, enables all features

**Key Files**:
- `app.manifest` (GUI and CLI projects)
- Documentation: explain why admin is required

---

### 4. Port Scanning Workflow

**Decision**: Background queue with priority for new devices

**Implementation**:
```
Scan workflow:
1. Ping sweep of subnet (fast, completes in seconds)
2. Display discovered devices immediately
3. Queue port scans in background:
   a. Priority 1: Newly discovered devices (never scanned before)
   b. Priority 2: Existing devices (update stale port data)
4. UI updates in real-time as port scans complete
5. Visual indicator shows scan progress per device

Settings:
- Port scan timeout per port (default: 100ms)
- Max concurrent port scans (default: 10)
- Common ports list (customizable)
```

**Key Files**:
- `NetworkScanner.cs`: Background queue implementation
- `DeviceManager.cs`: Priority logic
- GUI: Progress indicators

**User Experience Goal**: "Fastest method to get the user the info they need"

---

### 5. Concurrent Access Control

**Decision**: Single instance mode initially, file locking later

**Phase 1 Implementation**:
```
1. Use named Mutex to prevent multiple instances
2. Show error dialog: "IPScan is already running"
3. Option to bring existing window to front
```

**Phase 2+ Enhancement**:
```
1. Implement file-based locking with FileStream
2. Allow CLI and GUI to run simultaneously
3. Handle lock conflicts gracefully
4. Evaluate performance overhead before enabling
```

**Rationale**: Prevent data corruption first, optimize later if overhead is acceptable

---

### 6. IPv6 Support

**Decision**: IPv4 only for MVP

**Implementation**:
```
1. Device model stores single IPv4 address (string)
2. Network scanner uses IPv4 only
3. Add TODO comments for IPv6 support in Phase 2+
```

**Rationale**: Target audience doesn't commonly use IPv6 yet, can upgrade as need arises

---

## Phase 2: Enhanced Features

### 7. Device Discovery Notifications

**Decision**: Visual highlight by default, user-configurable enhancements

**Implementation**:
```
Default behavior (Phase 1):
- New devices shown in bold or highlighted color
- Fade to normal after first selection/view

Phase 2 Settings:
- Notification level dropdown:
  - None (silent)
  - Visual only (default)
  - Visual + toast notification
  - Visual + toast + sound alert
- Sound customization: different sounds for new vs returning devices
```

**User Experience Goal**: User will want to see new devices, with full control over notifications

---

### 8. Background Scanning Options

**Decision**: Manual by default, fully user-configurable

**Settings Implementation** (Phase 2):
```
Rescan Mode:
- Manual only (default - user clicks "Scan" button)
- Periodic automatic (every N minutes: 5, 10, 15, 30, 60)
- On network change (event-driven)
- Combination of above

Default: Manual only
```

**Rationale**: User customization is a priority

---

### 9. System Tray Behavior

**Decision**: Standard window by default, optional tray in Phase 2/3

**Phase 1**: Normal window, minimize to taskbar
**Phase 2/3 Settings**:
```
- No tray icon (default)
- Minimize to tray (background daemon style)
- Always show tray (with status indicator)
```

**Rationale**: Give the user power to configure the tool how they like

---

### 10. CLI Output Formats

**Decision**: Plain text for MVP, add JSON in Phase 1

**Implementation**:
```
Phase 1 MVP:
- ipscan list          # Human-readable table
- ipscan show <id>     # Formatted device details

Phase 1 Enhancement:
- ipscan list --format json
- ipscan show <id> --format json

Phase 2 (if requested):
- --format csv
- --format xml
```

---

### 11. Large Device List Handling

**Decision**: Simple list for MVP (Class C subnet), hierarchical view for Phase 2

**Implementation**:
```
Phase 1 (MVP):
- Simple WPF list, loads all devices
- Optimized for home networks (10-50 devices)
- Acceptable for small business (<100 devices)

Phase 2:
- Hierarchical view by subnet/VLAN
- Group devices by network segment
- Support multiple Class C networks
- Target: IoT device segregation, security camera VLANs
```

**Target Use Case**: "Typical users will have a single Class C, but might have multiple Class C's, or VLANs"

---

## Phase 3+: Advanced Features

### 12. MAC OUI Database Updates

**Decision**: Bundled database with optional manual updates, auto-update in Phase 2

**Phase 1**:
```
- Bundle OUI database with installer
- Include in application resources
```

**Phase 1 Enhancement**:
```
- Settings: "Update MAC Database" button
- Download latest from IEEE
- Manual update only
```

**Phase 2**:
```
- Settings: Enable auto-update checkbox
- Check frequency: weekly/monthly
- Background download and update
```

---

### 13. Device Data Retention

**Decision**: Keep forever by default, optional archival

**Settings Implementation**:
```
Retention Policy:
- Keep all devices (default)
- Archive devices offline for:
  - 30 days
  - 60 days
  - 90 days
  - Custom

Archived Device Behavior:
- Separate "Archived" list/section
- Can restore from archive
- Archive data persisted to separate JSON file
```

---

### 14. Network Scanning Ethics Warning

**Decision**: First-run disclaimer + splash screen reminder

**Implementation**:
```
First Launch:
- Dialog: "Only scan networks you own or have permission to scan"
- "I understand" checkbox
- Don't show again (stored in settings)

Every Launch (Splash Screen):
- Brief text: "Scan responsibly - own/authorized networks only"
- Small disclaimer in corner
```

**Rationale**: Remind users of their responsibility without being annoying

---

### 15. Data Export Formats

**Decision**: JSON standard, user-selectable formats in configuration

**Implementation**:
```
Phase 2 Export Feature:
- Default: JSON (already the storage format)
- Settings: "Export Format" dropdown
  - JSON (default)
  - CSV (for Excel/databases)
  - XML
  - HTML report

Export options:
- Export all devices
- Export filtered/selected devices
- Export with/without offline devices
```

---

## Implementation Priority Order

### Immediate (Phase 1 Core - Start Here)

1. **Network Interface Detection** (Q1.1, Q1.2)
   - Detect primary interface with default gateway
   - Filter out VPN interfaces
   - Scan primary interface only (defer multi-interface prompt)

2. **Storage System** (Q1.3)
   - Implement %APPDATA%\IPScan persistence
   - Add memory-only fallback mode
   - Warning banner for memory-only mode

3. **Admin Elevation** (Q2.1)
   - Update app.manifest for requireAdministrator
   - Test with full network capabilities

4. **Single Instance Mode** (Q2.3)
   - Named Mutex to prevent multiple instances
   - Error dialog with "already running" message

5. **Ping Sweep Implementation** (Q1.4)
   - IPv4 ping discovery on primary interface
   - Device list population
   - Basic online/offline detection

6. **Port Scanning** (Q2.2, Q1.4)
   - Background queue implementation
   - Priority queue (new devices first)
   - Real-time UI updates

7. **Device Categorization** (Q1.4)
   - Category assignment logic
   - UI for category display/editing

8. **MAC Manufacturer Lookup** (Q1.4, Q4.1)
   - Bundle OUI database
   - Lookup on device discovery
   - Display in UI

### Phase 1 Enhancements (After Core Works)

9. **CLI JSON Output** (Q3.4)
   - Add --format json flag
   - Scriptable output

10. **Multi-Interface Prompt** (Q1.1)
    - Detect additional interfaces
    - Prompt user after primary scan
    - Interface selection dialog

11. **Ethics Warning** (Q4.3)
    - First-run disclaimer dialog
    - Splash screen reminder text

### Phase 2 Features (After MVP Release)

12. **Notification System** (Q3.1)
    - Visual highlights for new devices
    - Settings for toast/sound notifications

13. **Background Scanning** (Q3.2)
    - Periodic rescan timer
    - Network change detection
    - Configurable intervals

14. **File Locking** (Q2.3)
    - Replace single-instance with file locks
    - Enable concurrent CLI/GUI access

15. **Hierarchical Device View** (Q3.5)
    - Group by subnet/VLAN
    - Support multiple networks

16. **MAC Database Updates** (Q4.1)
    - Manual update button
    - Optional auto-update

### Phase 3+ Features (Future)

17. **System Tray** (Q3.3)
18. **Data Retention/Archive** (Q4.2)
19. **Export Formats** (Q4.4)
20. **IPv6 Support** (Q2.4)

---

## Development Principles

**From User Responses:**

1. **User Control is Paramount**: Provide configuration options for everything
2. **Performance First**: Feel "light and fast" - optional features shouldn't slow mandatory operations
3. **Clear Communication**: Show warnings, prompt for choices, explain implications
4. **Completeness over Speed**: No artificial deadlines - "When it's ready it's ready"
5. **Start Simple, Add Options**: MVP with defaults, then add configuration in later phases

---

## Technical Constraints Summary

- **.NET 10.0** with WPF for GUI
- **Always requires admin elevation**
- **IPv4 only** for MVP
- **Single instance** initially (mutex)
- **Class C subnet optimization** (10-100 devices)
- **Windows-only** (no cross-platform requirements stated)

---

## Next Actions for Development

1. ✅ All blocker questions answered - development can proceed
2. ✅ Phase 1 scope defined - Full MVP with categorization
3. ⏭️ Implement items 1-8 from Priority Order (above)
4. ⏭️ Test with admin elevation on real networks
5. ⏭️ Iterate on UX based on actual usage

---

**Document Status**: Complete and actionable
**Review Date**: Update as implementation reveals new decisions needed
**Maintained By**: Development Team
