# MinerSearch

Tool for finding and removing hidden miners (cryptocurrency mining malware).

> [!CAUTION]
> ### Antivirus may give a false positive reaction to this application.

## Features

- 🚀 **.NET 8 WPF Application** - Modern, fast and cross-platform ready
- 🌙 **Dark/Light Theme** - System theme detection and manual switching
- ☁️ **Cloud Signature Updates** - Always up-to-date threat database
- 🤖 **Telegram Bot** - Remote control and notifications
- 🌐 **Web Interface** - SignalR-powered real-time scanning
- 📊 **System Tray** - Background monitoring with notifications

## Download

Download the latest release from the [Releases](https://github.com/yourusername/MinerSearch/releases) page.

### System Requirements

- Windows 10/11
- .NET 8.0 Runtime (for framework-dependent version)

## Quick Start

1. Extract the archive to a folder
2. Run `MinerSearch.exe`
3. Wait for scan to complete

## Command Line Options

| Short | Long | Description |
|-------|------|-------------|
| `-h` | `--help` | Display help message |
| `-cm` | `--console-mode` | Enable console mode |
| `-si` | `--silent` | Enable silent mode |
| `-fs` | `--full-scan` | Include all local drives |
| `-s=` | `--select=` | Scan specific directory |

## Building from Source

### Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 or VS Code

### Build Commands

```bash
# Restore dependencies
dotnet restore MinerSearch.sln

# Build Release
dotnet build MinerSearch.sln -c Release

# Run
dotnet run --project MinerSearch/MinerSearch.csproj
```

## Architecture

```
MinerSearch/
├── Core/              # Core types and enums
├── Models/            # Data models
├── Services/          # Business logic services
│   ├── ScannerService      # Main scanning engine
│   ├── QuarantineService  # File quarantine
│   ├── UpdateService      # Cloud updates
│   ├── ThemeService       # Theme management
│   ├── TrayIconService    # System tray
│   └── TelegramBotService # Telegram bot
├── UI/
│   ├── ViewModels/   # MVVM ViewModels
│   └── Views/        # WPF Views
├── Infrastructure/   # Utilities and converters
└── Resources/         # Themes and assets

MinerSearch.Web/      # ASP.NET Core web interface
├── Hubs/             # SignalR hubs
├── Services/         # Web services
└── wwwroot/          # SPA frontend
```

## GitHub Actions

Automatic builds and releases are configured. Every push to `main` branch triggers:
1. Build Release configuration
2. Run tests
3. Create ZIP archives
4. Publish to Releases

## License

MIT License - See LICENSE.MD for details

## Credits

Original project by BlendLog
