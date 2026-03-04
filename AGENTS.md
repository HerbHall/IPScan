<!--
  Scope: AGENTS.md guides the Copilot coding agent and Copilot Chat.
  For code completion and code review patterns, see .github/copilot-instructions.md
  For Claude Code, see CLAUDE.md
-->

# IPScan

Network scanner for IP range discovery with a WPF desktop interface.

## Tech Stack

- **Language**: C# 13 / .NET 10.0
- **UI**: WPF (Windows Presentation Foundation) with XAML
- **Target**: `net10.0` (Core, CLI), `net10.0-windows10.0.19041.0` (GUI)
- **Testing**: xUnit, NSubstitute (mocking), Coverlet (coverage)
- **Networking**: SharpPcap for packet capture
- **Versioning**: MinVer (automatic from Git tags, `v` prefix)
- **Nullable reference types**: enabled globally

## Build and Test Commands

```bash
# Build the entire solution
dotnet build

# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Format check (no changes, just verify)
dotnet format --check

# Format and fix
dotnet format

# Full verification (run before any PR)
dotnet build && dotnet test && dotnet format --check
```

## Project Structure

```text
IPScan/
├── src/
│   ├── IPScan.Core/          - Core library: scanning, discovery, models
│   ├── IPScan.CLI/           - Command-line interface entry point
│   └── IPScan.GUI/           - WPF desktop application (Windows only)
├── tests/
│   ├── IPScan.Core.Tests/    - Unit tests for Core library
│   └── IPScan.CLI.Tests/     - Unit tests for CLI
├── docs/                     - Design documents and specs
├── scripts/                  - Build and utility scripts
├── analyses/                 - Analysis artifacts
├── artifacts/                - Build output artifacts
├── Directory.Build.props     - Shared MSBuild properties (MinVer, metadata)
├── IPScan.slnx               - Solution file
├── CLAUDE.md                 - Claude Code instructions
└── .github/                  - CI workflows and Copilot config
```

## Workflow Rules

### Always Do

- Create a feature branch for every change (`feature/issue-NNN-description`)
- Use conventional commits: `feat:`, `fix:`, `refactor:`, `docs:`, `test:`, `chore:`
- Run build, test, and format before opening a PR
- Write xUnit tests with descriptive `[Fact]` and `[Theory]` names
- Use nullable reference types -- avoid `null!` suppression unless justified
- Fix every error you find, regardless of who introduced it

### Ask First

- Adding new NuGet dependencies (check if the BCL covers the need)
- Architectural changes (new projects, major interface changes)
- Changes to the WPF UI layout or navigation structure
- Changes to CI/CD workflows
- Removing or renaming public APIs

### Never Do

- Commit directly to `main` -- always use feature branches
- Skip tests or format checks -- even for "small changes"
- Use `--no-verify` or `--force` flags
- Commit secrets, credentials, or API keys
- Add TODO comments without a linked issue number
- Mark work as complete when build, test, or format failures remain

## Core Principles

These are unconditional -- no optimization or time pressure overrides them:

1. **Quality**: Once found, always fix, never leave. There is no "pre-existing" error.
2. **Verification**: Build, test, and format must pass before any commit.
3. **Safety**: Never force-push `main`. Never skip hooks. Never commit secrets.
4. **Honesty**: Never mark work as complete when it is not.

## Error Handling

```csharp
// Throw specific exception types with context
throw new InvalidOperationException($"Failed to scan range {range}: {reason}");

// Use custom exception types for domain-specific errors
public class ScanException : Exception
{
    public ScanException(string message) : base(message) { }
    public ScanException(string message, Exception inner) : base(message, inner) { }
}

// Guard clauses for argument validation
ArgumentNullException.ThrowIfNull(scanner);
ArgumentException.ThrowIfNullOrWhiteSpace(ipRange);
```

## Testing Conventions

```csharp
// xUnit tests with descriptive names
public class ScannerTests
{
    [Fact]
    public void Scan_ValidRange_ReturnsDiscoveredDevices()
    {
        // Arrange
        var scanner = new Scanner();

        // Act
        var result = scanner.Scan("192.168.1.0/24");

        // Assert
        Assert.NotEmpty(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-ip")]
    [InlineData("999.999.999.999")]
    public void Scan_InvalidRange_ThrowsArgumentException(string range)
    {
        var scanner = new Scanner();
        Assert.Throws<ArgumentException>(() => scanner.Scan(range));
    }
}

// Use NSubstitute for mocking interfaces
var logger = Substitute.For<ILogger<Scanner>>();
var scanner = new Scanner(logger);
```

## Commit Format

```text
feat: add subnet mask auto-detection

Implements CIDR notation parsing and automatic subnet mask calculation
from the provided IP range.

Closes #15
Co-Authored-By: GitHub Copilot <copilot@github.com>
```

Types: `feat` (new feature), `fix` (bug fix), `refactor` (no behavior change),
`docs` (documentation only), `test` (tests only), `chore` (build/tooling).
