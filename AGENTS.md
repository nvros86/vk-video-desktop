# AGENTS.md — VK Video Desktop

## Stack

- C# / .NET 9, WinUI 3 (Windows App SDK 1.7+), target `net9.0-windows10.0.22621.0`
- MVVM, DI via `Microsoft.Extensions.Hosting`, SQLite, Serilog, WebView2
- Test framework: xUnit + Moq

## Build & Test

```bash
dotnet restore
dotnet build
dotnet test
```

Single test: `dotnet test --filter "FullyQualifiedName~DownloadEngineTests"`

No separate linter/formatter/analyzer configured. `TreatWarningsAsErrors` is false.
Suppressed warnings: `CS1591`, `NU1603`, `NU1605`.

## Solution Structure

```
src/
  VKVideoDesktop.App/           — WinUI 3 host (DI in App.xaml.cs, system tray P/Invoke)
  VKVideoDesktop.Core/          — Models, Enums, Interfaces (zero dependencies)
  VKVideoDesktop.Application/   — Business logic services (depends on Core only)
  VKVideoDesktop.Infrastructure/ — VK API, Download Engine, Cache, WebView
  VKVideoDesktop.Data/          — SQLite repositories
  VKVideoDesktop.Tests/         — Unit tests
  VKVideoDesktop.Installer/     — WPF installer (separate target: net9.0-windows)
```

Dependency flow: `App → Infrastructure → Application → Core ← Data`

## WinUI 3 Gotchas

- Entry point is `Program.cs` with `DispatcherQueueSynchronizationContext`
- `App.xaml.cs` has a `Directory.Build.targets` that stubs out XAML compilation targets — this is intentional to work around build issues. Do not modify without understanding why.
- **XAML compilation cannot run on machines without Windows SDK installed.** To build the App project on such machines, create minimal `.xbf` stub files in `obj/{Platform}/Debug/net9.0-windows10.0.226210/` and an empty `intermediatexaml/VKVideoDesktop.App.dll`. The `Directory.Build.targets` also removes `.xbf` from `Content` and `ContentWithTargetPath` to avoid copy errors.
- No MSIX packaging (`WindowsPackageType=None`) — runs as unpackaged
- `VKVideoDesktop.App` uses platform-specific builds (`x86;x64;ARM64`). Do not build with `AnyCPU`.
- `VKVideoDesktop.Installer` targets plain `net9.0-windows` (not Windows-specific TFM), uses WPF + WinForms

## Conventions

- File-scoped namespaces (`namespace X;`)
- Implicit usings enabled, nullable enabled
- EditorConfig: 4-space indent for `.cs`/`.xaml`, 2-space for `.csproj`/`.xml`/`.json`, LF endings
- Models use `init`-only properties with `= string.Empty` defaults
- ViewModels inherit `ViewModelBase` (INotifyPropertyChanged with `SetProperty`)
- DI registrations in `App.xaml.cs` constructor — all singletons except `SearchService`, `VideoService`, `DownloadService` (transient)
- Logging via `ILogger<T>` (Serilog backed), log path: `%LOCALAPPDATA%\VKVideoDesktop\Logs`

## Key Patterns

- **Provider abstraction**: `IVideoProvider`, `IVideoDownloadProvider`, `IDownloadSourceResolver` — VK implementation lives in `Infrastructure/VkApi/`
- **Download system**: `DownloadEngine` → `DownloadManager` → `DownloadDatabase`. Uses `.part` temp files, HTTP Range resume, atomic `File.Move` on completion
- **Security**: Download URLs validated (HTTPS only, no `file://`/`javascript:`/`data:`). Redirect limit enforced. No secrets in logs or SQLite
- **Localization**: `.resw` files in `Resources/Strings/{en-US,ru-RU}/`. All UI strings must go through resources
- **System tray**: Custom P/Invoke implementation in `App.xaml.cs` (WinUI 3 lacks native tray support)

## Testing

Tests reference Core, Application, Infrastructure, and Data projects. Use `Mock<T>` from Moq for service dependencies. Tests are pure unit tests — no integration or UI test infrastructure currently.

## What NOT to Do

- Do not add comments unless explicitly asked
- Do not commit secrets, tokens, or API keys
- Do not block UI thread with I/O or network calls
- Do not use `TreatWarningsAsErrors` (it's intentionally false)
- Do not introduce new frameworks/libraries without verifying they work with WinUI 3 unpackaged mode
- Do not modify `Directory.Build.targets` in the App project without understanding the XAML compilation workarounds
- Do not build `VKVideoDesktop.App` with `AnyCPU` — it requires platform-specific builds
