# AGENTS.md — VK Video Desktop
Always respond exclusively in Russian. No matter what language the code or the prompt is in, all explanations, comments, plans, and answers must be in Russian.

## Stack & Build

C# / .NET 9, WinUI 3 (Windows App SDK 1.7+), MVVM, SQLite, WebView2.
Target: `net9.0-windows10.0.22621.0`. Unpackaged (`WindowsPackageType=None`).

```bash
dotnet restore VKVideoDesktop.sln
dotnet build VKVideoDesktop.sln -c Release
dotnet test VKVideoDesktop.sln -c Release
```

CI runs on `windows-latest` (`.github/workflows/ci.yml`). Publish: `dotnet publish src/VKVideoDesktop.App/VKVideoDesktop.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o publish`.

## Architecture

```
VKVideoDesktop.sln
src/
  VKVideoDesktop.App/            — WinUI 3 host, DI (App.xaml.cs), system tray (P/Invoke), entry point (Program.cs)
  VKVideoDesktop.Core/           — Models, Enums, Interfaces (zero dependencies)
  VKVideoDesktop.Application/    — Business logic services
  VKVideoDesktop.Infrastructure/ — VK API, Download Engine, Cache
  VKVideoDesktop.Data/           — SQLite persistence
  VKVideoDesktop.Tests/          — xUnit unit tests (Mocks/ and Unit/)
  VKVideoDesktop.Installer/      — Installer
```

Dependency flow: `App → Infrastructure, Application, Data → Core`. Core has no project references.

## Key Conventions

- **DI**: All registrations in `App.xaml.cs` constructor. Singletons for services, transient for `SearchService`, `VideoService`, `DownloadService`.
- **System tray**: Custom Win32 P/Invoke in `App.xaml.cs` (WinUI 3 lacks native tray). Uses `Shell_NotifyIcon`, hidden window for message pump.
- **Localization**: `.resw` files in `Resources/Strings/{en-US,ru-RU}/`. `LocalizationService` reads XML format.
- **Logging**: Serilog to `%LOCALAPPDATA%\VKVideoDesktop\Logs\log-.txt` (rolling daily, 7-day retention).
- **Download engine**: Custom `DownloadEngine` with streaming, resume via HTTP Range, atomic file completion, speed limiting.
- **Settings**: DPAPI encryption for access tokens in `SettingsService`.

## Build Gotchas

- `TreatWarningsAsErrors` is **false**. Suppressed: `CS1591`, `NU1603`, `NU1605`.
- VKVideoDesktop.App uses platform-specific builds (`x86;x64;ARM64`). Not `AnyCPU`.
- Some `.g.cs` files are hand-written stubs (e.g. `App.g.cs`, `MainWindow.g.cs`) — WinUI XAML codegen quirk.
- `EnableMsixTooling=true` but `WindowsPackageType=None` (unpackaged deployment).
