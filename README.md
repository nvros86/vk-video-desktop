# VK Video Desktop

Production-ready Windows desktop client for watching VK Video with a full-featured download manager.

## Tech Stack

- C# / .NET 9
- WinUI 3 / Windows App SDK
- MVVM architecture
- SQLite
- WebView2 (fallback)

## Architecture

```
VKVideoDesktop.sln
src/
  VKVideoDesktop.App/          — WinUI 3 Application
  VKVideoDesktop.Core/          — Models, Enums, Interfaces
  VKVideoDesktop.Application/   — Business logic services
  VKVideoDesktop.Infrastructure/ — VK API, Download Engine, Cache
  VKVideoDesktop.Data/          — SQLite persistence
  VKVideoDesktop.Tests/         — Unit tests
```

## Features

- Video browsing and search
- Channel/subscriber pages
- Video player with playback controls
- Favorites and local playlists
- Watch history with resume
- Playback queue
- Fullscreen, Picture-in-Picture, Mini Player
- Hotkeys
- **Full download manager:**
  - Streaming download engine
  - Progress, speed, ETA
  - Pause / Resume / Cancel / Retry
  - HTTP Range resume
  - Concurrent download queue
  - Disk space checks
  - Speed limiting
  - Atomic file completion
  - Crash recovery
  - Windows notifications
- Dark / Light / System themes
- Russian and English localization

## Requirements

- Windows 10/11 64-bit
- .NET 9 SDK
- Windows App SDK 1.7+

## Building

```bash
dotnet restore
dotnet build
```

## Testing

```bash
dotnet test
```

## License

MIT
