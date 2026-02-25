# IPScan

Windows network device discovery, monitoring, and remote access tool.

## Build Commands

```bash
dotnet restore          # Restore dependencies
dotnet build            # Build all projects
dotnet test             # Run tests
dotnet publish -c Release  # Publish release build
```

## Architecture

```text
IPScan/
├── src/
│   ├── IPScan.Core/     # Shared business logic, models, services
│   ├── IPScan.CLI/      # System.CommandLine CLI interface
│   └── IPScan.GUI/      # WPF desktop application
├── tests/
│   ├── IPScan.Core.Tests/
│   └── IPScan.CLI.Tests/
├── analyses/            # Code analysis reports
├── artifacts/           # Research artifacts
├── prompts/             # Claude prompts (completed work)
└── docs/                # Documentation
```

## Tech Stack

- .NET 10.0, WPF, C#
- SharpPcap for network scanning
- System.CommandLine for CLI
- Windows-only (admin elevation required)

## Key Decisions

- See [DECISIONS.md](DECISIONS.md) for all implementation decisions
- Phase 1 MVP: ping sweep, port scanning, device categorization, MAC OUI lookup
- IPv4 only for MVP, single instance via Mutex
- Storage: `%APPDATA%\IPScan\` with memory-only fallback

## Tooling Workarounds

### Spec Kit CLI on Windows

The `specify` CLI (from `uv tool install specify-cli`) has two issues on Windows:

**1. Unicode crash (Rich library)**

`specify version` and other commands crash with `UnicodeEncodeError: 'charmap' codec can't encode characters` due to Rich's `legacy_windows_render` on cp1252 encoding.

```bash
# Fix: set these environment variables before any specify command
export PYTHONIOENCODING=utf-8
export NO_COLOR=1
```

**2. Interactive prompt hang (`specify init`)**

`specify init` hangs at the `readchar`-based script-type selection prompt. This affects any non-interactive context (Claude Code, CI, subprocesses) regardless of terminal type. The `--ai`, `--force`, and `--no-git` flags don't bypass this prompt.

```bash
# Fix: always provide --script flag to bypass the interactive prompt
PYTHONIOENCODING=utf-8 NO_COLOR=1 specify init ./project --ai claude --no-git --force --script ps
```

Valid `--script` values: `ps` (PowerShell), `sh` (Bash/shell).

**Status**: Known upstream ([github/spec-kit#267](https://github.com/github/spec-kit/issues/267)). Workarounds above are reliable. Re-evaluate if these become a pain point.
