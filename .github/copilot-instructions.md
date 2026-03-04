# IPScan -- Copilot Instructions

Network scanner for IP range discovery with a WPF desktop interface.

## Tech Stack

- **Language**: C# 13 / .NET 10.0
- **UI**: WPF with XAML (`net10.0-windows10.0.19041.0` for GUI)
- **Libraries**: Core targets `net10.0` (cross-platform), GUI targets Windows
- **Testing**: xUnit, NSubstitute, Coverlet
- **Networking**: SharpPcap for packet capture
- **Nullable reference types**: enabled project-wide

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
├── Directory.Build.props     - Shared MSBuild properties (MinVer, metadata)
├── IPScan.slnx               - Solution file
└── CLAUDE.md                 - Claude Code instructions
```

## Code Style

- Conventional commits: `feat:`, `fix:`, `refactor:`, `docs:`, `test:`, `chore:`
- Co-author tag: `Co-Authored-By: GitHub Copilot <noreply@github.com>`
- Use nullable reference types -- avoid `null!` suppression unless justified
- xUnit tests with `[Fact]` and `[Theory]` attributes, descriptive method names
- All format checks must pass before committing (`dotnet format --check`)

## Coding Guidelines

- Fix errors immediately -- never classify them as pre-existing
- Build, test, and format must pass before any commit
- Never skip hooks (`--no-verify`) or force-push main
- Validate only at system boundaries (user input, external APIs)
- Remove unused code completely; no backwards-compatibility hacks
- WPF GUI project only builds on Windows (`net10.0-windows10.0.19041.0`)

## Available Resources

```bash
dotnet build                 # Build the entire solution
dotnet test                  # Run all tests
dotnet format --check        # Verify code formatting
dotnet format                # Auto-fix formatting issues
dotnet test --collect:"XPlat Code Coverage"  # Tests with coverage
```

## Do NOT

- Suppress nullable warnings (`null!`) without a clear justification comment
- Use `#pragma warning disable` without fixing the root cause first
- Commit generated files without regenerating them first
- Add NuGet dependencies without verifying the BCL does not already provide the functionality
- Use `Thread.Sleep` in async code; use `Task.Delay` instead
- Store secrets, tokens, or credentials in code or config files
- Mark work as complete when known errors remain
- Reference WPF types from the Core library (Core must stay cross-platform)
