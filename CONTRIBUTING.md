# Contributing to PS5 BackPork Kitchen

Thank you for your interest in contributing to PS5 BackPork Kitchen! This document provides guidelines and instructions for contributing.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [How to Contribute](#how-to-contribute)
- [Coding Standards](#coding-standards)
- [Testing](#testing)
- [Pull Request Process](#pull-request-process)
- [Project Structure](#project-structure)

## Code of Conduct

- Be respectful and inclusive
- Focus on constructive feedback
- Help maintain a positive community
- Report unacceptable behavior to project maintainers

## Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/YOUR-USERNAME/PS5-BACKPORK-KITCHEN.git
   cd PS5-BACKPORK-KITCHEN
   ```
3. **Create a branch** for your changes:
   ```bash
   git checkout -b feature/your-feature-name
   ```

## Development Setup

### Prerequisites

- Windows 10/11 (64-bit)
- Visual Studio 2022 or later
- .NET 10 SDK
- Git for Windows

### Building the Project

1. Open `PS5 BACKPORK KITCHEN.slnx` in Visual Studio
2. Restore NuGet packages:
   - Right-click solution → Restore NuGet Packages
3. Build the solution (Ctrl+Shift+B)
4. Run the project (F5)

### Dependencies

- **Newtonsoft.Json** (13.0.4): JSON serialization
- **ReaLTaiizor** (3.8.1.4): UI components

## How to Contribute

### Reporting Bugs

When reporting bugs, include:
- **Clear description** of the issue
- **Steps to reproduce** the problem
- **Expected behavior** vs actual behavior
- **Screenshots** if applicable
- **System information** (Windows version, .NET version)
- **Log files** if available

### Suggesting Features

Feature requests should include:
- **Clear description** of the feature
- **Use case** explaining why it's needed
- **Proposed implementation** (optional)
- **UI mockups** if relevant (optional)

### Code Contributions

We welcome code contributions! Areas that need help:
- Bug fixes
- New features from the roadmap
- Performance improvements
- Documentation improvements
- UI/UX enhancements
- Test coverage

## Coding Standards

### VB.NET Style Guide

```vb.net
' ✓ Good: PascalCase for public members
Public Function CalculateChecksum(filePath As String) As String

' ✓ Good: camelCase for local variables
Dim totalSize As Long = 0

' ✓ Good: Descriptive names
Public SelectedGameFolder As String

' ✗ Bad: Abbreviations or unclear names
Public selFldr As String

' ✓ Good: XML documentation for public APIs
''' <summary>
''' Validates the game folder structure
''' </summary>
Public Function ValidateGameFolder(path As String) As Boolean
```

### File Organization

```
PS5 BACKPORK KITCHEN/
├── 01Frms/          # UI Forms
├── 01global/        # Global utilities and variables
├── core/            # Core patching logic
├── Services/        # Service modules
├── Downloadslibs/   # Download management
└── Resources/       # Images and assets
```

### Naming Conventions

- **Classes/Modules**: PascalCase (`BackupService`, `InputValidator`)
- **Methods**: PascalCase (`CreateBackup`, `ValidateFolder`)
- **Variables**: camelCase for locals, PascalCase for public
- **Constants**: UPPER_SNAKE_CASE (`MAX_LOG_FILE_SIZE`)

### Error Handling

```vb.net
' ✓ Good: Specific exception handling
Try
    Dim data = File.ReadAllText(path)
Catch ex As FileNotFoundException
    Logger.Log(rtb, $"File not found: {path}", Color.Red, True, LogLevel.Error)
    Return False
Catch ex As UnauthorizedAccessException
    Logger.Log(rtb, "Permission denied", Color.Red, True, LogLevel.Error)
    Return False
End Try

' ✗ Bad: Generic catch-all without logging
Try
    ' code
Catch ex As Exception
End Try
```

### Logging

Always use the Logger module with appropriate log levels:

```vb.net
Logger.Log(rtbStatus, "Starting backup process", Color.Blue, True, Logger.LogLevel.Info)
Logger.Log(rtbStatus, "Backup completed", Color.Green, True, Logger.LogLevel.Success)
Logger.Log(rtbStatus, "Warning: Large file detected", Color.Orange, True, Logger.LogLevel.Warning)
Logger.Log(rtbStatus, "Error during patch", Color.Red, True, Logger.LogLevel.Error)
```

## Testing

### Manual Testing Checklist

Before submitting a PR, test:
- [ ] Fresh install scenario
- [ ] Patch operation on sample game folder
- [ ] Backup creation and restore
- [ ] Error handling (invalid paths, missing permissions)
- [ ] UI responsiveness during operations
- [ ] Log file creation and rotation

### Test Data

Create a test folder structure:
```
TestGame_PPSA12345/
├── sce_sys/
│   ├── param.json
│   └── icon0.png
├── eboot.bin
└── sce_module/
    ├── libSceAgc.prx
    └── libSceAudio3d.sprx
```

## Pull Request Process

1. **Update your branch** with latest main:
   ```bash
   git fetch upstream
   git rebase upstream/main
   ```

2. **Commit your changes** with clear messages:
   ```bash
   git commit -m "feat: add integrity verification system"
   git commit -m "fix: handle null reference in ElfReader"
   git commit -m "docs: update README with new features"
   ```

3. **Push to your fork**:
   ```bash
   git push origin feature/your-feature-name
   ```

4. **Create Pull Request** on GitHub with:
   - Clear title describing the change
   - Detailed description of what changed and why
   - Reference any related issues
   - Screenshots for UI changes

### Commit Message Format

Use conventional commits:
- `feat:` New feature
- `fix:` Bug fix
- `docs:` Documentation changes
- `refactor:` Code refactoring
- `perf:` Performance improvements
- `test:` Adding tests
- `chore:` Maintenance tasks

Examples:
```
feat: add post-patch integrity verification
fix: handle null path in GetTempFolder
docs: update CONTRIBUTING with coding standards
refactor: simplify ElfPatcher logic
```

## Project Structure

### Key Modules

- **Form1.vb**: Main application form and UI logic
- **ElfPatcher.vb**: Core ELF patching functionality
- **ElfReader.vb**: ELF file reading and inspection
- **Logger.vb**: Logging system with file output
- **BackupService.vb**: Backup creation and restoration
- **InputValidator.vb**: Input validation and checks
- **IntegrityVerifier.vb**: Post-patch verification
- **ConfigurationManager.vb**: Settings management

### Adding New Features

1. Create new service module in `Services/` folder
2. Add XML documentation comments
3. Follow existing naming conventions
4. Add error handling and logging
5. Update relevant documentation

## Questions?

If you have questions:
- Open an issue with the `question` label
- Check existing issues and discussions
- Review the [README](README.md) and documentation

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

---

Thank you for contributing to PS5 BackPork Kitchen! 🎮✨
