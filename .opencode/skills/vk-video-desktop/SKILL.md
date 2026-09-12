---
name: vk-video-desktop
description: Правила OpenCode для анализа, разработки, тестирования и отладки WinUI 3 приложения VK Video Desktop в этом репозитории.
metadata:
  short-description: Аудит и разработка VK Video Desktop
---

# Проектный skill VK Video Desktop

Применяй этот skill при работе с репозиторием VK Video Desktop. Сначала прочитай AGENTS.md: он задаёт обязательные правила проекта и имеет приоритет над общими предположениями. Все объяснения, планы, комментарии и отчёты пиши на русском языке.

## Контекст проекта

- Стек: C#, .NET 9, WinUI 3, Windows App SDK 1.7+, MVVM, SQLite, WebView2.
- Target framework: net9.0-windows10.0.22621.0.
- Приложение unpackaged (`WindowsPackageType=None`), сборки приложения: x86, x64 и ARM64.
- Слои: App → Core, Application, Infrastructure, Data, Tests.
- Core не ссылается на другие проекты (нулевые зависимости).
- Регистрации DI находятся в `src/VKVideoDesktop.App/App.xaml.cs`.
- Сервисы приложения используют интерфейсы Core; не добавляй ссылки из Core в UI, SQLite или WinUI.

### Архитектура

```
VKVideoDesktop.sln
src/
  VKVideoDesktop.App/            — WinUI 3 host, DI (App.xaml.cs), system tray (P/Invoke), entry point (Program.cs)
  VKVideoDesktop.Core/           — Models, Enums, Interfaces (zero dependencies)
  VKVideoDesktop.Application/    — Business logic services (SettingsService, LocalizationService, ErrorHandlerService, etc.)
  VKVideoDesktop.Infrastructure/ — VK API, Download Engine, Cache
  VKVideoDesktop.Data/           — SQLite persistence (History, Favorites, Playlists, Downloads)
  VKVideoDesktop.Tests/          — xUnit unit tests
```

### Технические детали

- .NET 9 SDK位于 `C:\dotnet9\` (version 9.0.318) — NOT in PATH; must prefix: `$env:Path = "C:\dotnet9;" + $env:Path`
- VS Build Tools 2022: `C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe` — **required for XAML compilation**
- `dotnet build` CANNOT compile XAML. Must use VS MSBuild.
- **НЕ используй `Resources.pri` (MakePri)** — ломает XAML загрузку в unpackaged WinUI 3 приложениях. WinUI 3 пытается использовать PRI для разрешения XAML ресурсов, и это конфликтует с XBF файлами. Локализация работает через кастомный `LocalizationService` (dictionary-based .resw парсинг).
- `x:Uid` в XAML **не работает** для unpackaged apps без Resources.pri. Используй code-behind или `{x:Bind}` с LocalizationService для текстов.
- `TreatWarningsAsErrors` is **false**. Suppressed: `CS1591`, `NU1603`, `NU1605`.
- `EnableMsixTooling=true` but `WindowsPackageType=None` (unpackaged deployment).
- GitHub PAT: `github_pat_11CMH5DI0uV0KeQfkDEZN_FUNsSqffKJQvAPDit3Iwv9ktJxZuNSzEG7034eiCxsI5DORKAOQhH6bSy5l`
- User is on Windows 10 19044 (21H2) — WinUI 3 compatibility issues; `_host.StartAsync()` wrapped in `Task.Run()` to avoid UI thread crash.

## Рабочий процесс аудита

1. Проверь `git status --short` и не удаляй чужие изменения, незакоммиченные файлы или копии отчётов.
2. Используй `rg --files` для списка файлов, исключая bin и obj при анализе исходников.
3. Проверь зависимости и DI: отсутствуют ли циклы, совпадают ли lifetime, разрешаются ли все зарегистрированные сервисы.
4. Проверь сборку командами из AGENTS.md: restore, Release build и Release test.
5. При ошибке тестов сначала воспроизведи только упавший тест, затем исправь причину и повтори весь набор.
6. При проблеме WinUI или XAML запускай только одну сборку рабочего каталога. Параллельные XAML-компиляторы могут блокировать промежуточные DLL.
7. После анализа обновляй DEBUG_REPORT.md: указывай файл, строки, наблюдаемое поведение, последствия, исправление и способ проверки.
8. Разделяй подтверждённые ошибки, тестовые пробелы и предложения по улучшению. Не выдавай старые замечания из копии отчёта за текущие.

## Критические инварианты

### DI и запуск

- MainWindow должен создаваться тем же DI-контейнером, в котором он зарегистрирован.
- Не создавай второй экземпляр singleton вручную в Program.cs.
- Не запускай асинхронные операции из конструктора UI без контролируемого жизненного цикла и обработки ошибок.
- Startup должен сначала загрузить настройки (LoadAsync), затем принимать решение о восстановлении загрузок (RecoverIncompleteDownloadsAsync).

### Загрузки

- У `.part` только один владелец финального перемещения.
- Финальный файл создаётся только после успешного полного ответа и проверки размера.
- Недостаток места, отмена, тайм-аут, 401/403, rate limit и неполный ответ должны иметь различимые результаты.
- CTS каждой загрузки удаляется и освобождается после завершения.
- Прогресс, статус и путь должны быть согласованы между DownloadManager, SQLite и UI.
- Из фонового потока нельзя напрямую менять ObservableCollection или WinUI-bound свойства; используй DispatcherQueue.

### Настройки и секреты

- Токены храни только через DPAPI (`System.Security.Cryptography.ProtectedData`). Не записывай открытый токен в журнал, тестовый вывод или отчёт.
- Сохраняй снимок настроек под блокировкой (`SemaphoreSlim`) и заменяй файл атомарно через временный файл (.tmp → File.Move).
- Повреждённый JSON и DPAPI-ошибка должны приводить к безопасным значениям по умолчанию и диагностике.
- Не удаляй settings.json по наличию имени свойства: наличие AccessToken не означает повреждение.

### SQLite

- Инициализация схемы должна быть идемпотентной и безопасной при конкурентном запуске (`SchemaMigration`).
- Для nullable-колонок используй `IsDBNull` или сделай колонку NOT NULL миграцией.
- Не делай конкурентный read-modify-write одного JSON-поля без блокировки (известный race condition в `PlaylistDatabase.AddVideoAsync`).
- После миграции добавляй тест повторного применения, старой схемы и одновременной инициализации.

### Логи

- Следуй пути и политике из AGENTS.md: `%LOCALAPPDATA%\VKVideoDesktop\Logs\log-*.txt`, ежедневная ротация и хранение 7 дней.
- Используй Serilog `ILogger<T>` через DI для всех сервисов.
- Статический `Serilog.Log` используется только в `Program.cs` и `App.xaml.cs` (до DI).
- Никогда не логируй access token и другие секреты.
- DownloadManager использует structured logging с correlation ID через `BeginScope`.

### Метрики

- `AppMetrics` (singleton) трекает: API-вызовы (latency, success), загрузки (started/completed/failed/cancelled), retry.
- Crash handlers (`UnhandledException`, `UnobservedTaskException`, `WinUIUnhandledException`) логируют snapshot метрик через `LogMetricsSnapshot()`.

### Локализация

- `LocalizationService` использует `Windows.ApplicationModel.Resources.ResourceLoader`.
- Два языка: `ru-RU` и `en-US`. Смена без перезапуска через `Frame.Navigate`.
- Все UI-строки в XAML через `x:Uid`, в коде через `_localization["key"]`.
- Текстовые фрагменты в форматировании (время, просмотры) используют `string.Format` с ключами.

## Обязательная матрица тестов

При изменениях соответствующего слоя проверяй:

- разрешение DI без циклов;
- полный сценарий DownloadManager;
- pause, resume, cancel, retry и восстановление после перезапуска;
- критически низкое место, неполный HTTP-ответ и запрет повторного перемещения `.part`;
- deep link video, search и channel;
- авторизацию через `IAuthenticationService`;
- `SettingsService.ResetAsync`, повреждённый JSON, параллельное сохранение и DPAPI;
- nullable-маппинг SQLite, миграции и конкурентные операции;
- удаление истории, плейлистов и загрузок из ViewModel/UI с проверкой реального репозитория;
- ошибки VK API, rate limit, 401/403, timeout и отмену;
- установку, удаление и exit codes внешних процессов.

Тест, который вызывает только Moq-интерфейс, не подтверждает работу ViewModel, страницы или SQLite. Для UI-операций проверяй цепочку «обработчик — ViewModel — сервис — репозиторий».

### Известные проблемы

- **Race condition в `PlaylistDatabase.AddVideoAsync`**: read-modify-write без блокировки приводит к потере видео при параллельных добавлениях. Тест `PlaylistDatabase_AddVideoAsync_ConcurrentAdd_DoesNotLoseVideos` документирует этот баг.

## Стиль изменений

- Используй минимальный патч и сохраняй структуру проекта.
- Перед редактированием проверь существующие изменения; не делай reset, checkout или массовое удаление.
- Не добавляй внешние зависимости без необходимости.
- Не меняй production-код во время аудита, если пользователь просит только отчёт и предложения; сначала зафиксируй доказательство и план.
- В коде используй существующие интерфейсы и `ILogger<T>`.
- Для сетевого кода сохраняй CancellationToken, не подавляй `OperationCanceledException` общим catch.
- Для файловых операций обрабатывай права, временные файлы, атомарность и очистку после ошибки.
- После исправления запускай релевантный тест, затем полный `dotnet test`.

## Формат отчёта

Для каждой найденной проблемы указывай:

1. Приоритет: критический, высокий, средний или низкий.
2. Точный файл и диапазон строк.
3. Условие воспроизведения.
4. Фактическое и ожидаемое поведение.
5. Причину и практическое исправление.
6. Регрессионный тест или ручную проверку.

Итоговый отчёт для этого репозитория хранится в DEBUG_REPORT.md. Файл DEBUG_REPORT — копия.md может быть пользовательской копией и не является источником текущего состояния.
