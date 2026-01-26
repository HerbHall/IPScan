# IPScan Requirements Questions

**Document Purpose**: Capture open questions and decisions needed for implementation
**Status**: Awaiting Response
**Created**: 2026-01-26
**Last Updated**: 2026-01-26

---

## How to Use This Document

1. Read each question in priority order
2. Fill in your answer in the `**ANSWER:**` section
3. Mark status as `ANSWERED` when complete
4. Reference this document when implementing related features

**Status Values:**
- `OPEN` - Not yet answered
- `RESEARCHING` - Investigating options
- `ANSWERED` - Decision made
- `DEFERRED` - Will decide later, use default for now

---

## Priority 1: BLOCKERS (Needed for Phase 1 Development)

These questions must be answered before implementing core network scanning functionality.

---

### Q1.1: Multiple Network Interfaces - Scanning Behavior

**Priority**: BLOCKER
**Status**: ANSWERED
**Impacts**: `NetworkInterfaceService.cs`, `NetworkScanner.cs`, Settings UI
**Phase**: 1 (Core Functionality)

**QUESTION:**

When a computer has multiple active network interfaces (Ethernet, Wi-Fi, VPN, etc.):
- Should the application scan all interfaces simultaneously?
- Let the user choose one interface?
- Scan primary interface only?

**OPTIONS:**

A. **Scan primary interface only** (simplest, good for MVP)
   - Auto-detect the interface with the default gateway
   - Ignore VPN interfaces by default

B. **Scan all interfaces** (most comprehensive)
   - Deduplicate devices found on multiple subnets
   - Could discover more devices but slower

C. **User selects interface** (most flexible)
   - Settings dialog shows dropdown of available interfaces
   - Remember selection across sessions
   - Default to primary if selection no longer available

**RECOMMENDED**: Option A for MVP, then add Option C in Phase 2

**ANSWER:**

[A first then, then if there are other interfaces, prompt the user if they want to scan one or more additional interfaces.]

**DECISION RATIONALE:**

[Include option A now, add optional prompt as a future feature to add later.]

**IMPLEMENTATION NOTES:**

[We want the program to feel light and fast, with more time consuming work optional with user prompts, settings options that let the user know it will impact the speed of the scan.]

---

### Q1.2: VPN Interface Handling

**Priority**: BLOCKER
**Status**: ANSWERED
**Impacts**: `NetworkInterfaceService.cs`
**Phase**: 1 (Core Functionality)

**QUESTION:**

Should VPN interfaces (Tailscale, Wireguard, OpenVPN, etc.) be:
- Excluded from scanning by default?
- Included but flagged?
- Treated the same as physical interfaces?

**OPTIONS:**

A. **Exclude VPN interfaces by default** (recommended)
   - Filter out by interface type or naming patterns
   - Prevents scanning remote VPN networks unintentionally

B. **Include but show warning**
   - Scan VPN interfaces but alert user they're scanning a remote network

C. **Treat all interfaces equally**
   - No special handling

**RECOMMENDED**: Option A (exclude by default)

**ANSWER:**

[A]

**DECISION RATIONALE:**

[We want to identify the interface as a VPN but not scan it unless user specifically requests it. ]

---

### Q1.3: Storage Failures - Fallback Strategy

**Priority**: BLOCKER
**Status**: ANSWERED
**Impacts**: `JsonSettingsService.cs`, `JsonDeviceRepository.cs`
**Phase**: 1 (Core Functionality)

**QUESTION:**

If `%APPDATA%\IPScan` can't be created/written (permissions, disk full, network drive):
What should the application do?

**OPTIONS:**

A. **Fall back to local directory** (executable location)
   - Store `devices.json` and `settings.json` next to exe
   - Log warning to user

B. **Run in memory-only mode**
   - Function normally but don't persist anything
   - Show persistent warning banner

C. **Hard fail with error message**
   - Refuse to start, show error dialog
   - User must fix permissions first

**RECOMMENDED**: Option A (fallback to local directory)

**ANSWER:**

[Hybred solution default B with a warning and prompt user to choose option of A or B]

**DECISION RATIONALE:**

[I want to give the user as much flexibility to make decisions about they use the program as possible.]

---

### Q1.4: Minimum Viable Product (MVP) Feature Set

**Priority**: BLOCKER
**Status**: ANSWERED
**Impacts**: Development roadmap, Phase 1 scope
**Phase**: 1 (Core Functionality)

**QUESTION:**

For the first functional release that you'd actually use, what's the absolute minimum feature set?

**OPTIONS:**

A. **Minimal** - Ping sweep + device storage only
   - Discover devices by IP
   - Store name, IP, online status
   - No port scanning, no categorization

B. **Basic** - Option A + port scanning
   - Ping sweep to find devices
   - Scan common ports (22, 80, 443, etc.)
   - Show clickable HTTP/HTTPS links

C. **Full Phase 1** - Option B + categorization
   - Everything in Option B
   - Plus device categorization
   - Plus MAC manufacturer lookup

**RECOMMENDED**: Option B (Basic - ping + port scanning)

**ANSWER:**

[C]

**DECISION RATIONALE:**

[I want the initial release to be as complete as possible. No deadline for release, prefer completeness over speed.]

**TARGET COMPLETION:**

[WHen it's ready it's ready]

---

## Priority 2: HIGH (Needed Soon, Can Start with Defaults)

These questions affect Phase 1 implementation but can proceed with reasonable defaults.

---

### Q2.1: Administrative Privileges Required?

**Priority**: HIGH
**Status**: ANSWERED
**Impacts**: Deployment, Windows manifest, documentation
**Phase**: 1 (Core Functionality)

**QUESTION:**

Does the application require administrator/elevated privileges to run?

**CONTEXT:**
- Ping (ICMP) can work without admin using managed `Ping` class
- Raw sockets (for ARP) typically require elevation
- SharpPcap may need admin for packet capture

**OPTIONS:**

A. **Always require admin** (simplest)
   - Add `requireAdministrator` to app manifest
   - Full network capabilities

B. **Function without admin, limited capabilities**
   - Ping works (no admin needed)
   - MAC discovery disabled (needs admin)
   - Show message explaining limitations

C. **Prompt for elevation when needed**
   - Start normally
   - If MAC discovery requested, prompt for elevation
   - Restart with elevation if granted

**RECOMMENDED**: Option B (graceful degradation) with clear UI indicators

**ANSWER:**

[A]

**DECISION RATIONALE:**

[Likely to require elevated privledges, get it out of the way up front.]

---

### Q2.2: Port Scanning - When to Trigger

**Priority**: HIGH
**Status**: ANSWERED
**Impacts**: `NetworkScanner.cs`, `DeviceManager.cs`, GUI scan workflow
**Phase**: 1 or 2 (depending on MVP scope)

**QUESTION:**

When should port scanning happen?

**OPTIONS:**

A. **Automatic for all devices** (slowest but most complete)
   - After ping discovery, automatically scan ports
   - User waits for complete scan

B. **Automatic for new devices only** (balanced)
   - Port scan only on first discovery
   - Re-scan on manual request

C. **Manual only** (fastest initial scan)
   - Ping sweep only by default
   - Right-click device > "Scan Ports"
   - Or global "Scan All Ports" button

D. **Background queue** (best UX)
   - Ping sweep shows results immediately
   - Port scans queued in background
   - UI updates as port scans complete

**RECOMMENDED**: Option D (background queue) or Option C (manual) for MVP

**ANSWER:**

[D, but prioritize new devices first then update existing after that is complete]

**DECISION RATIONALE:**

[fastest method to get the user the info they need.]

---

### Q2.3: CLI/GUI Concurrent Access - File Locking

**Priority**: HIGH
**Status**: ANSWERED
**Impacts**: `JsonDeviceRepository.cs`, `JsonSettingsService.cs`
**Phase**: 1 (Core Functionality)

**QUESTION:**

Can the CLI and GUI run simultaneously and access the same data files?

**OPTIONS:**

A. **No file locking** (simple but risky)
   - Last write wins
   - Possible data corruption if both save simultaneously

B. **File locking** (safe but complex)
   - Use FileStream with FileShare options
   - Handle lock conflicts gracefully
   - Show "file in use" errors

C. **Prevent concurrent access** (safest)
   - Mutex/lock file prevents second instance
   - Show "already running" message

**RECOMMENDED**: Option C for MVP (single instance), consider Option B for later

**ANSWER:**

[C initially, B in later builds ]

**DECISION RATIONALE:**

[We need prevent data corruption, later we can analyze the feasibility of improving performance with simultanious access if the overhead of file locking isn't too burdensome]

---

### Q2.4: IPv6 Support

**Priority**: HIGH
**Status**: ANSWERED
**Impacts**: Device model, scanner implementation
**Phase**: 1 or Future

**QUESTION:**

Should IPv6 addresses be supported?

**OPTIONS:**

A. **IPv4 only for now** (simplest MVP)
   - Focus on IPv4, add IPv6 in Phase 2+

B. **Dual stack support** (modern standard)
   - Support both IPv4 and IPv6
   - Store both addresses per device if detected

C. **IPv6 preferred** (future-proof)
   - Prioritize IPv6, fall back to IPv4

**RECOMMENDED**: Option A for MVP (IPv4 only)

**ANSWER:**

[A]

**DECISION RATIONALE:**

[IPv6 is not common in my intended audiance's networks yet. Future upgrade as the need arises.]

---

## Priority 3: MEDIUM (Improve UX, Can Defer to Phase 2)

These questions enhance user experience but aren't blocking for MVP.

---

### Q3.1: Device Discovery Notifications

**Priority**: MEDIUM
**Status**: ANSWERED
**Impacts**: GUI MainWindow, Settings
**Phase**: 2 (Enhanced Features)

**QUESTION:**

When new devices are discovered, should there be notifications?

**OPTIONS:**

A. **No notifications** (silent)
   - Just add to the list

B. **Visual highlight only** (subtle)
   - New devices shown in different color/bold
   - Fade to normal after selected/viewed first time seconds

C. **Toast notification** (noticeable)
   - Windows toast notification
   - Click to jump to device

D. **Optional sound alert** (configurable)
   - Settings toggle for sound
   - Different sound for first-time vs returning device

**RECOMMENDED**: Option B (visual highlight) + Option D as optional setting

**ANSWER:**

[B by defauly with configuration setting for choosing A or combination of B, C, and/or D]

**DECISION RATIONALE:**

[User will want to see new devices. User choice in how they use the application. ]

---

### Q3.2: Background Scanning - Periodic Rescans

**Priority**: MEDIUM
**Status**: ANSWERED
**Impacts**: Settings, background worker services
**Phase**: 2 (Enhanced Features)

**QUESTION:**

Should the application support automatic periodic rescanning?

**OPTIONS:**

A. **Manual scan only** (MVP approach)
   - User clicks "Scan" button

B. **Periodic automatic rescans** (daemon-like)
   - Settings: rescan every N minutes (5, 10, 15, 30, 60)
   - Default: disabled, user opts in

C. **Scan on network change** (event-driven)
   - Detect when network connects/disconnects
   - Automatically rescan

D. **Combination** (most flexible)
   - Options B + C, both configurable

**RECOMMENDED**: Option A for MVP, add Option B or D in Phase 2

**ANSWER:**

[A by default, user configurable settings for A, B, C, or D.]

**DECISION RATIONALE:**

[user customization is a priority]

---

### Q3.3: System Tray / Minimize Behavior

**Priority**: MEDIUM
**Status**: ANSWERED
**Impacts**: GUI MainWindow, App.xaml
**Phase**: 2 (Enhanced Features)

**QUESTION:**

Should the application support system tray minimization?

**OPTIONS:**

A. **No tray icon** (standard window only)
   - Minimize to taskbar like normal app

B. **Minimize to tray** (background daemon style)
   - X button minimizes to tray
   - Right-click tray for quick actions
   - Double-click tray to restore

C. **Tray icon always** (with status indicator)
   - Tray icon shows scan status (idle/scanning)
   - Tray menu: Scan Now, Open, Exit
   - Window can be open or minimized to tray

**RECOMMENDED**: Defer to Phase 2, start with Option A

**ANSWER:**

[A by default with user configurable B or C in Phase 2 or 3]

**DECISION RATIONALE:**

[Give the user the power to configure the tool how they like]

---

### Q3.4: CLI Output Format Options

**Priority**: MEDIUM
**Status**: ANSWERED
**Impacts**: CLI Program.cs command handlers
**Phase**: 1 or 2

**QUESTION:**

For CLI `list` and `show` commands, what output formats?

**OPTIONS:**

A. **Plain text only** (simple)
   - Human-readable table format

B. **Add JSON option** (scriptable)
   - `--format json` flag
   - Enables automation/scripting

C. **Multiple formats** (flexible)
   - `--format table|json|csv`
   - Maximum flexibility

**RECOMMENDED**: Option A for MVP, add Option B in Phase 1

**ANSWER:**

[Option A for MVP, add Option B in Phase 1]

**DECISION RATIONALE:**

[Give the user the power to configure the tool how they like]

---

### Q3.5: Large Device Lists - Performance

**Priority**: MEDIUM
**Status**: ANSWERED
**Impacts**: GUI virtualization, device repository
**Phase**: 2 (Enhanced Features)

**QUESTION:**

For networks with many devices (100+), what performance strategies?

**CONTEXT:**
- Home networks: typically 10-50 devices
- Small business: 50-200 devices
- Enterprise: 500+ devices (probably not target audience)

**OPTIONS:**

A. **No special handling** (works for <100 devices)
   - Simple list, loads all devices
   - Acceptable for home/small networks

B. **UI virtualization** (handles 100-1000 devices)
   - WPF VirtualizingStackPanel
   - Only render visible items

C. **Pagination** (enterprise scale)
   - Show 50-100 devices per page
   - Prev/Next buttons

**RECOMMENDED**: Option A for MVP, add Option B if needed

**ANSWER:**

[Option A for MVP (display expecting a single class c subnet), phase 2 implement higheracle view for Class b and higher sub nets]

**TARGET USE CASE:**

[Typical users will have a single class c, but might have multiple class c's, or vlans for segregation of IOT devices or security cameras etc... We will want to support these users needs.]

---

## Priority 4: LOW (Nice to Have, Defer to Phase 3+)

These questions are lower priority and can use reasonable defaults.

---

### Q4.1: MAC OUI Database - Update Strategy

**Priority**: LOW
**Status**: ANSWERED
**Impacts**: Phase 2 categorization feature
**Phase**: 2 or 3

**QUESTION:**

For MAC address manufacturer lookups, how to handle the OUI database?

**OPTIONS:**

A. **Bundle database with app** (simple, static)
   - Include OUI database in installer
   - Update only with app updates

B. **Download on first run** (always current)
   - Download from IEEE on first launch
   - Store locally

C. **Auto-update** (complex)
   - Check for updates weekly/monthly
   - Download and update in background

**RECOMMENDED**: Defer to Phase 2, then use Option A

**ANSWER:**

[Hybred approach. Include option A in first build with ability for user to optionally update the file. Phase 2 include logic and settings to allow user to configure optional auto updates]

---

### Q4.2: Device Data Retention

**Priority**: LOW
**Status**: ANSWERED
**Impacts**: Device cleanup logic
**Phase**: 2 or 3

**QUESTION:**

For devices that haven't been seen in a long time, what retention policy?

**OPTIONS:**

A. **Keep forever** (never auto-delete)
   - Manual cleanup only

B. **Archive after N days offline** (configurable)
   - Settings: archive after 30/60/90 days
   - Archived devices in separate list

C. **Auto-delete after N days** (aggressive)
   - Permanently remove old devices

**RECOMMENDED**: Option A for now, consider Option B later

**ANSWER:**

[B with Default configured to option A. User configurable to enable archival after specified time.]

---

### Q4.3: Network Scanning Ethics Warning

**Priority**: LOW
**Status**: ANSWERED
**Impacts**: First-run experience, documentation
**Phase**: 1 (nice to have)

**QUESTION:**

Should the application display a warning about only scanning networks you own/have permission for?

**OPTIONS:**

A. **No warning** (user responsibility)

B. **First-run disclaimer** (one-time)
   - Show on first launch
   - "I understand" checkbox

C. **Always show** (every scan)
   - Warning dialog before each scan
   - Probably too annoying

**RECOMMENDED**: Option B (first-run disclaimer)

**ANSWER:**

[Option B, but include a brief warning in the splash screen to remind the user of their responsibility]

---

### Q4.4: Data Export Formats

**Priority**: LOW
**Status**: ANSWERED
**Impacts**: Export/Import feature
**Phase**: 2 (Enhanced Features)

**QUESTION:**

Beyond JSON, what export formats?

**OPTIONS:**

A. **JSON only** (simple)
   - Already the storage format

B. **JSON + CSV** (spreadsheet friendly)
   - CSV for import into Excel/databases

C. **Multiple formats** (comprehensive)
   - JSON, CSV, XML, HTML report

**RECOMMENDED**: Option A for Phase 2, add CSV if requested

**ANSWER:**

[A as standard option, but allow user to select other formats in the configuration for any user reports.]

---

## Summary & Next Steps

### To Unblock Development Immediately

Please answer these questions first (in order):
1. **Q1.4**: MVP Feature Set - What's the minimum you need?
2. **Q1.1**: Network interface handling
3. **Q1.3**: Storage failure strategy
4. **Q2.2**: Port scanning trigger (if in MVP)

### Can Proceed with Defaults

These have recommended defaults that allow development to continue:
- Q1.2: VPN exclusion (default: exclude)
- Q2.1: Admin privileges (default: graceful degradation)
- Q2.3: File locking (default: single instance)
- Q2.4: IPv6 support (default: IPv4 only for MVP)

### Defer to Later Phases

All Priority 3 and 4 questions can be deferred or use suggested defaults.

---

## Response Template

You can copy this template to respond:

```markdown
## My Responses - [Date]

### Q1.4: MVP Feature Set
**ANSWER:** Option B (Basic - ping + port scanning)
**RATIONALE:** I want to see web interfaces right away, that's the main use case
**TARGET:** 2 weeks

### Q1.1: Multiple Network Interfaces
**ANSWER:** Option A (primary only) for now
**RATIONALE:** Simple, I only care about my home LAN
**NOTES:** Add interface selection in settings later

[Continue for other questions...]
```

---

**Document Maintained By**: Development Team (Me and Claude)
**Review Frequency**: Update as decisions are made
**Related Documents**: [requirements.md](requirements.md), [README.md](README.md)
