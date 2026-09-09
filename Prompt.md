VK VIDEO DESKTOP — ЕДИНЫЙ ПОДРОБНЫЙ ПРОМТ ДЛЯ СОЗДАНИЯ WINDOWS-ПРИЛОЖЕНИЯ

Версия документа: 1.0
Назначение: production-ready Windows desktop client для просмотра VK Видео с полноценным менеджером разрешённых загрузок.

======================================================================
1. ОБЩАЯ ЗАДАЧА
======================================================================

Создай современное Windows-приложение «VK Video Desktop» для просмотра видеоконтента VK Видео.

Приложение должно предоставлять:
- просмотр видео;
- поиск видео;
- просмотр каналов и авторов;
- категории и рекомендации, если доступны;
- авторизацию через официальный механизм VK;
- подписки/плейлисты, где это поддерживается;
- локальное избранное;
- локальные плейлисты;
- историю просмотров;
- очередь воспроизведения;
- полноэкранный режим;
- Picture-in-Picture;
- Mini Player;
- горячие клавиши;
- кэширование thumbnails;
- полноценный Download Manager;
- скачивание доступного пользователю контента;
- очередь и параллельные загрузки;
- pause/resume;
- докачку через HTTP Range, если сервер её поддерживает;
- retry;
- восстановление после перезапуска;
- проверку свободного места;
- ограничение скорости;
- Windows-уведомления;
- настройки;
- SQLite;
- WebView2 fallback.

ВАЖНО:
Функция скачивания должна быть полностью реализована как production-ready subsystem.
При этом приложение не должно обходить DRM, access control, платный доступ, авторизацию, технические ограничения или другие механизмы защиты VK/правообладателей. Использовать только официально доступные приложению ресурсы и разрешённые способы сохранения.

======================================================================
2. ПЛАТФОРМА
======================================================================

Цель:
- Windows 10 64-bit;
- Windows 11 64-bit.

Минимальное разрешение:
1280×720.

Оптимальное:
1920×1080 и выше.

Поддержать:
1366×768;
1920×1080;
2560×1440;
3840×2160.

Обязательно:
- HiDPI;
- масштабирование 100–200%;
- несколько мониторов;
- keyboard + mouse;
- responsive desktop layout.

======================================================================
3. ТЕХНОЛОГИЧЕСКИЙ СТЕК
======================================================================

Использовать:

- C#;
- .NET 8/9;
- WinUI 3;
- Windows App SDK;
- MVVM;
- Dependency Injection;
- async/await;
- HttpClient;
- SQLite;
- WebView2.

Для видеоплеера использовать стабильный Windows-compatible backend:
- Media Foundation / Windows APIs либо другой надёжный backend.

WebView2 использовать для официальных веб-компонентов VK и fallback-сценариев.

Не строить приложение как простой wrapper над сайтом. Основной интерфейс должен быть настоящим desktop UI.

======================================================================
4. АРХИТЕКТУРА
======================================================================

Создать:

VKVideoDesktop.sln

src/
  VKVideoDesktop.App/
  VKVideoDesktop.Core/
  VKVideoDesktop.Application/
  VKVideoDesktop.Infrastructure/
  VKVideoDesktop.Data/
  VKVideoDesktop.Tests/

Архитектура:

UI
 ↓
ViewModels
 ↓
Application Services
 ↓
Interfaces
 ↓
Infrastructure Providers
 ↓
VK API / WebView2 / Local Database / File System

Главное правило:
UI не должен знать, каким образом получены данные.

======================================================================
5. PROVIDER ABSTRACTION
======================================================================

Создать:

public interface IVideoProvider
{
    Task<IReadOnlyList<Video>> SearchAsync(
        string query,
        CancellationToken cancellationToken);

    Task<Video?> GetVideoAsync(
        string videoId,
        CancellationToken cancellationToken);

    Task<Channel?> GetChannelAsync(
        string channelId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Video>> GetRecommendationsAsync(
        CancellationToken cancellationToken);
}

Архитектура должна позволять в будущем добавлять другие видеосервисы без переписывания UI.

======================================================================
6. ГЛАВНОЕ ОКНО
======================================================================

Создать современный Fluent/Windows 11 интерфейс.

Основная тема:
Modern Dark Video Client.

Темы:
- Dark;
- Light;
- System.

Структура:

┌─────────────────────────────────────────────────────────────┐
│ VK Video Desktop   Search...                 Downloads  ⚙  │
├───────────────┬─────────────────────────────────────────────┤
│ 🏠 Главная    │                                             │
│ 🔎 Поиск      │              CONTENT AREA                  │
│ ▶ Подписки    │                                             │
│ ❤️ Избранное  │                                             │
│ 📚 Плейлисты  │                                             │
│ 🕘 История    │                                             │
│ ⬇ Загрузки    │                                             │
│ ⚙ Настройки   │                                             │
└───────────────┴─────────────────────────────────────────────┘

Sidebar collapsible.
На маленьком окне использовать navigation drawer.

======================================================================
7. TOP BAR
======================================================================

Слева:
VK Video Desktop.

По центру:
🔎 Поиск видео, каналов и авторов

Поддержать:
- Enter;
- Ctrl+K;
- очистку;
- autocomplete;
- историю поиска.

Справа:
- Downloads;
- Notifications;
- Profile;
- Settings.

======================================================================
8. HOME
======================================================================

Разделы:

Продолжить просмотр:
- thumbnail;
- title;
- author;
- duration;
- progress bar;
- процент просмотра.

Рекомендации:
адаптивная grid-сетка.

Карточка:
- thumbnail;
- duration;
- title;
- author;
- views;
- publication date;
- context menu.

Context menu:
- Смотреть;
- Открыть;
- Открыть в новой вкладке;
- Добавить в очередь;
- Добавить в избранное;
- Добавить в плейлист;
- Скачать, если доступно;
- Поделиться;
- Открыть в VK.

======================================================================
9. SEARCH
======================================================================

Экран поиска:

Результаты поиска.

Фильтры:
- Видео;
- Каналы;
- Авторы;
- Трансляции;
- другие типы, если официально доступны.

Сортировка:
- По релевантности;
- По дате;
- По популярности.

Использовать pagination/infinite scrolling.
Не загружать тысячи элементов сразу.

Debounce:
300–500 ms.

======================================================================
10. VIDEO PAGE
======================================================================

Структура:

VIDEO PLAYER

Название видео

Автор
Подписаться

Описание

[❤️] [➕ Плейлист] [⬇ Скачать] [↗ Поделиться]

Комментарии.

Показывать Download только когда доступен официальный/разрешённый источник.

======================================================================
11. VIDEO PLAYER
======================================================================

Поддержать:
- Play/Pause;
- seek;
- volume;
- mute;
- playback speed;
- quality;
- fullscreen;
- Picture-in-Picture;
- subtitles, если доступны;
- аудиодорожки, если доступны;
- buffering;
- current time;
- duration;
- autoplay;
- next video.

Скорости:
0.25x
0.5x
0.75x
1x
1.25x
1.5x
1.75x
2x

======================================================================
12. HOTKEYS
======================================================================

Space — Play/Pause
K — Play/Pause
F — Fullscreen
M — Mute
← — -5 sec
→ — +5 sec
Shift+← — -10 sec
Shift+→ — +10 sec
↑ — Volume +
↓ — Volume -
Ctrl+K — Search
Esc — Exit fullscreen

======================================================================
13. CHANNEL
======================================================================

Страница канала:
- banner;
- avatar;
- название;
- username;
- подписка;
- видео;
- трансляции;
- плейлисты;
- информация о канале.

Если конкретная возможность отсутствует в API, не имитировать её.

======================================================================
14. AUTHENTICATION
======================================================================

Использовать официальный OAuth/авторизационный механизм VK.

Авторизация:
- системный браузер;
- либо WebView2 с официальной страницей VK OAuth.

Не просить пароль VK напрямую.

Хранить credentials только безопасно:
- Windows Credential Manager;
- DPAPI;
- другой Windows secure storage.

Не:
- сохранять пароль;
- писать OAuth tokens в logs;
- хранить tokens в DownloadTask;
- хранить tokens в SQLite;
- отправлять credentials сторонним серверам.

======================================================================
15. PROFILE
======================================================================

После входа:
- Avatar;
- имя;
- username;
- мои видео, если API позволяет;
- мои плейлисты;
- избранное;
- история;
- настройки;
- logout.

======================================================================
16. FAVORITES
======================================================================

Создать локальное избранное.

Поддержать:
- add;
- remove;
- search;
- sorting;
- queue;
- playlists.

======================================================================
17. PLAYLISTS
======================================================================

Создать локальные плейлисты.

Примеры:
- Лучшие видео;
- Музыка;
- Обучение;
- Посмотреть позже.

Поддержать:
- create;
- rename;
- delete;
- add video;
- remove video;
- reorder.

======================================================================
18. HISTORY
======================================================================

Хранить:
- Video ID;
- title;
- author;
- thumbnail;
- last position;
- duration;
- last viewed.

Показывать:
«Продолжить с 12:43».

======================================================================
19. QUEUE
======================================================================

Queue Manager:
- add;
- remove;
- move up;
- move down;
- clear;
- play all;
- autoplay next.

======================================================================
20. DOWNLOAD SYSTEM — ОСНОВНОЙ МОДУЛЬ
======================================================================

Создать полноценную production-ready систему загрузок.

Структура:

Download/
  Models/
    DownloadTask.cs
    DownloadOption.cs
    DownloadProgress.cs
    DownloadResult.cs

  Services/
    IDownloadManager.cs
    IDownloadEngine.cs
    IDownloadSourceResolver.cs
    IFileWriter.cs
    IDownloadQueue.cs

  Providers/
    VkVideoDownloadProvider.cs

  Storage/
    DownloadRepository.cs

  ViewModels/
    DownloadsViewModel.cs
    DownloadItemViewModel.cs

======================================================================
21. DOWNLOAD MANAGER
======================================================================

Создать:

public interface IDownloadManager
{
    IReadOnlyList<DownloadTask> Downloads { get; }

    Task<DownloadTask> AddAsync(
        DownloadRequest request,
        CancellationToken cancellationToken);

    Task PauseAsync(
        string downloadId,
        CancellationToken cancellationToken);

    Task ResumeAsync(
        string downloadId,
        CancellationToken cancellationToken);

    Task CancelAsync(
        string downloadId,
        CancellationToken cancellationToken);

    Task RetryAsync(
        string downloadId,
        CancellationToken cancellationToken);

    Task RemoveAsync(
        string downloadId,
        bool deleteFile,
        CancellationToken cancellationToken);
}

======================================================================
22. DOWNLOAD TASK
======================================================================

Модель:

public sealed class DownloadTask
{
    public string Id { get; init; }
    public string VideoId { get; init; }
    public string Title { get; init; }
    public string ThumbnailUrl { get; init; }
    public string SourceUrl { get; init; }
    public string DestinationPath { get; init; }
    public string TemporaryPath { get; init; }

    public long? TotalBytes { get; set; }
    public long DownloadedBytes { get; set; }
    public double Progress { get; set; }
    public double SpeedBytesPerSecond { get; set; }
    public TimeSpan? RemainingTime { get; set; }

    public DownloadStatus Status { get; set; }

    public DateTime CreatedAt { get; init; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
}

Статусы:
Queued
Resolving
Downloading
Paused
Completed
Failed
Cancelled
Retrying

======================================================================
23. DOWNLOAD OPTIONS
======================================================================

Перед скачиванием получить реальные варианты:

public sealed class DownloadOption
{
    public string Id { get; init; }
    public string VideoId { get; init; }
    public string Quality { get; init; }
    public string Format { get; init; }
    public long? Size { get; init; }
    public bool IsAvailable { get; init; }
    public string? Label { get; init; }
}

Например:

1080p MP4 1.8 GB
720p MP4 950 MB
480p MP4 520 MB

Показывать только реально доступные варианты.

Не генерировать URL и не угадывать media endpoints.

======================================================================
24. DOWNLOAD DIALOG
======================================================================

Диалог:

Скачать видео

Качество:
○ 1080p MP4 1.8 GB
● 720p MP4 950 MB
○ 480p MP4 520 MB

Формат:
MP4

Сохранить в:
C:\Users\User\Videos\VK Video

[Изменить]
[Отмена] [Скачать]

Если доступен только один формат — не показывать ненужный selector.

======================================================================
25. DOWNLOAD PROVIDER
======================================================================

Создать:

public interface IVideoDownloadProvider
{
    Task<IReadOnlyList<DownloadOption>>
        GetAvailableDownloadsAsync(
            Video video,
            CancellationToken cancellationToken);

    Task<DownloadResult> DownloadAsync(
        DownloadOption option,
        string destination,
        CancellationToken cancellationToken);
}

Также:

public interface IVkVideoSourceResolver
{
    Task<IReadOnlyList<DownloadOption>>
        ResolveAsync(
            Video video,
            CancellationToken cancellationToken);
}

Resolver использует только официально доступные/разрешённые источники.

Если источник недоступен:
«Для этого видео VK не предоставляет доступный способ сохранения.»

НЕ реализовывать:
- DRM bypass;
- извлечение DRM keys;
- обход encrypted media;
- обход access control;
- обход приватных видео;
- кражу cookies;
- извлечение session tokens;
- подмену cookies;
- обход платного доступа;
- обход технических ограничений.

======================================================================
26. DOWNLOAD ENGINE
======================================================================

Создать реальный asynchronous streaming Download Engine.

Алгоритм:

1. Resolve source.
2. Проверить доступность.
3. Проверить свободное место.
4. Создать .part файл.
5. Открыть HTTP connection.
6. Использовать ResponseHeadersRead.
7. Получить Content-Length, если доступен.
8. Читать потоковыми блоками.
9. Записывать на диск.
10. Обновлять progress.
11. Вычислять скорость.
12. Вычислять ETA.
13. Поддерживать CancellationToken.
14. После завершения flush/close.
15. Выполнить валидацию.
16. Атомарно переименовать .part в конечный файл.
17. Записать Completed в SQLite.

Никогда не загружать большой файл целиком в RAM.

Использовать:
HttpCompletionOption.ResponseHeadersRead

и потоковый Stream.

======================================================================
27. BUFFER
======================================================================

Использовать разумный buffer:
64 KB – 1 MB.

Не создавать гигантские buffers.

======================================================================
28. PROGRESS
======================================================================

Обновлять UI примерно каждые 100–250 ms.

Показывать:

82%
1.24 GB / 1.51 GB
8.4 MB/s
Осталось примерно 32 сек

Не отправлять UI update на каждый байт.

======================================================================
29. SPEED
======================================================================

Использовать sliding window для расчёта скорости.

Не полагаться только на среднюю скорость за всё время.

======================================================================
30. RESUME / HTTP RANGE
======================================================================

Если сервер поддерживает Range:

Range: bytes=<offset>-

Использовать .part.

Перед resume:
- проверить существование .part;
- определить размер;
- проверить Accept-Ranges;
- выполнить запрос;
- убедиться в HTTP 206 Partial Content.

Если сервер возвращает 200 OK вместо 206:
НЕ дописывать данные к .part.
Корректно начать новую загрузку либо сообщить пользователю.

Не считать resume поддерживаемым без подтверждения сервером.

======================================================================
31. TEMPORARY FILES
======================================================================

Во время загрузки:
video.mp4.part

После успешного завершения:
video.mp4

Алгоритм:

Download
↓
.part
↓
Flush
↓
Close
↓
Validate
↓
Move
↓
Completed

Не показывать незавершённый файл как готовый.

======================================================================
32. ATOMIC COMPLETION
======================================================================

Финальный файл должен появляться только после успешного завершения.

Если возможна атомарная операция Move — использовать её.

======================================================================
33. RETRY
======================================================================

При временной ошибке:
- автоматически retry;
- configurable retry count;
- exponential backoff;
- jitter.

По умолчанию:
3 попытки.

Например:
2 sec
4 sec
8 sec
16 sec

Не retry-ить бесконечно.

Не повторять автоматически ошибки типа:
- Forbidden;
- Unauthorized;
- NotFound;
- permanently unavailable.

======================================================================
34. NETWORK ERRORS
======================================================================

Обрабатывать:
- timeout;
- DNS failure;
- connection reset;
- server disconnect;
- network unavailable;
- Wi-Fi interruption;
- 5xx;
- rate limit.

При временной ошибке:
Downloading
↓
Network error
↓
Retry delay
↓
Resolve source
↓
Resume

Если source URL имеет короткий срок жизни, перед retry/resume заново выполнить source resolution официальным способом.

======================================================================
35. PAUSE
======================================================================

При Pause:
1. остановить чтение;
2. корректно закрыть текущий HTTP stream;
3. сохранить .part;
4. записать состояние в SQLite;
5. статус Paused.

Не удалять .part.

======================================================================
36. RESUME
======================================================================

При Resume:
1. прочитать состояние;
2. проверить .part;
3. получить новый source, если нужно;
4. проверить Range;
5. продолжить загрузку;
6. обновить progress.

======================================================================
37. CANCEL
======================================================================

При Cancel показать подтверждение.

После подтверждения:
CancellationToken.Cancel()

Удалить .part согласно настройке.

Статус:
Cancelled.

======================================================================
38. DOWNLOAD QUEUE
======================================================================

Пример:

1. Video A — Downloading
2. Video B — Queued
3. Video C — Queued

Настройка:
Max concurrent downloads.

Варианты:
1 / 2 / 3 / 4 / 5

По умолчанию:
2.

Использовать SemaphoreSlim или эквивалентный concurrency controller.

======================================================================
39. BATCH DOWNLOAD
======================================================================

Поддержать:
«Скачать все видео из плейлиста»

Перед добавлением:
24 видео
Примерный размер: 18.4 GB

[Отмена] [Добавить в очередь]

Каждое видео — отдельная DownloadTask.

======================================================================
40. DOWNLOAD SELECTED
======================================================================

В списке видео:
☐ Video 1
☐ Video 2
☐ Video 3

Кнопка:
[Скачать выбранные]

Каждый выбранный элемент проходит отдельную проверку доступности.

======================================================================
41. FILE NAMING
======================================================================

Формат:

{Title} [{VideoId}].{Extension}

Sanitize:
\ / : * ? " < > |

Если существует:
video.mp4
video (1).mp4
video (2).mp4

Не перезаписывать существующий пользовательский файл без явного согласия.

======================================================================
42. DOWNLOAD DIRECTORY
======================================================================

По умолчанию:
%USERPROFILE%\Videos\VK Video

В Settings:
Папка загрузок
[Изменить]

Поддержать выбор любой доступной пользователю папки.

======================================================================
43. DISK SPACE
======================================================================

Если размер известен:
requiredSpace = fileSize + safetyMargin

Проверить:
DriveInfo.AvailableFreeSpace

Если места мало:
«Недостаточно свободного места»

Показывать:
Требуется
Доступно

Во время длинной загрузки периодически проверять свободное место.

Если место закончилось:
перевести в Paused/Failed с понятным сообщением.

======================================================================
44. SPEED LIMIT
======================================================================

Settings:

Без ограничений
512 KB/s
1 MB/s
2 MB/s
5 MB/s
10 MB/s
20 MB/s
Пользовательское значение

Throttle должен работать на уровне Download Engine.

======================================================================
45. DOWNLOAD DATABASE
======================================================================

SQLite table:

Downloads
(
    Id TEXT PRIMARY KEY,
    VideoId TEXT NOT NULL,
    Title TEXT NOT NULL,
    SourceUrl TEXT,
    DestinationPath TEXT NOT NULL,
    TemporaryPath TEXT,
    Quality TEXT,
    Format TEXT,
    Status INTEGER NOT NULL,
    TotalBytes INTEGER,
    DownloadedBytes INTEGER,
    Speed REAL,
    RetryCount INTEGER,
    ErrorMessage TEXT,
    CreatedAt TEXT,
    StartedAt TEXT,
    CompletedAt TEXT
);

Не хранить access tokens/cookies в этой таблице.

======================================================================
46. CRASH RECOVERY
======================================================================

После запуска:
DownloadRecoveryService.

Найти задачи:
Downloading / Resolving / Retrying.

Проверить:
- .part;
- конечный файл;
- размер;
- доступность source;
- состояние базы.

Восстановить очередь.

Если возможно:
resume.

Если невозможно:
показать пользователю:
«Загрузка была прервана. Продолжить?»

======================================================================
47. DOWNLOAD HISTORY
======================================================================

Завершённые загрузки:
- thumbnail;
- title;
- quality;
- format;
- size;
- date.

Действия:
- Открыть;
- Открыть папку;
- Копировать путь;
- Удалить запись;
- Удалить запись и файл.

======================================================================
48. DOWNLOAD CARD
======================================================================

Карточка:

┌──────────────────────────────────────────────────────────┐
│ [Thumbnail] Название видео                           ⋮  │
│             VK Video • 720p • MP4                       │
│                                                          │
│             ███████████████░░░░ 76%                     │
│             722 MB / 950 MB                             │
│             8.4 MB/s • ~27 sec                          │
│                                                          │
│             ⏸ Пауза     ✕ Отмена                       │
└──────────────────────────────────────────────────────────┘

Статусы:
Queued
Resolving
Downloading
Paused
Completed
Failed
Retrying
Cancelled

======================================================================
49. GLOBAL DOWNLOAD INDICATOR
======================================================================

В sidebar:
⬇ Загрузки 3

В top bar:
⬇ 2

Показывать число активных загрузок.

======================================================================
50. WINDOWS NOTIFICATIONS
======================================================================

После завершения:
VK Video Desktop
Загрузка завершена
Название видео
[Открыть]

После ошибки:
Загрузка не удалась
Причина
[Повторить]

======================================================================
51. OPEN FILE
======================================================================

После завершения:
[Открыть]
[Открыть папку]

Открывать через Windows Shell.

Для папки желательно использовать Explorer с выделением файла.

======================================================================
52. DOWNLOAD SETTINGS
======================================================================

Settings → Downloads:

- Download folder;
- Max concurrent downloads;
- Speed limit;
- Retry count;
- Automatically resume after startup;
- Notifications;
- Delete .part on cancel;
- Ask folder before download;
- Download behavior.

Download behavior:
○ Показывать выбор качества
● Использовать последнее выбранное качество
○ Использовать максимальное доступное качество

======================================================================
53. SMART QUALITY
======================================================================

Опционально:

4K monitor → предпочитать 2160p
1440p monitor → предпочитать 1440p
1080p monitor → предпочитать 1080p

Но только если такое качество реально доступно.

======================================================================
54. AUDIO ONLY
======================================================================

Если официальный источник предоставляет отдельный audio resource и это разрешено:
- Download audio;
- M4A;
- MP3, только если такой вариант официально предоставлен или преобразование допустимо.

Не извлекать аудио из защищённого потока обходными способами.

======================================================================
55. DOWNLOAD SECURITY
======================================================================

Download Engine:
- HTTPS only;
- validate URL;
- reject unsupported schemes;
- validate final host after redirects;
- ограничить redirects;
- не логировать Authorization;
- не логировать cookies;
- не логировать tokens.

Запретить:
file://
javascript:
data:
vbscript:

и другие неподходящие схемы.

======================================================================
56. URL EXPIRATION
======================================================================

Не считать media URL постоянным.

Если source имеет expiration:
хранить SourceReference, а не рассчитывать на бессрочный URL.

Перед retry/resume:
ResolveSource()
↓
новый URL
↓
resume

только если это официально поддерживается источником.

======================================================================
57. AUTHORIZED DOWNLOADS
======================================================================

Если VK официально предоставляет авторизованному пользователю доступ к загрузке:
использовать официальный OAuth access token.

Token:
- Windows secure storage;
- не DownloadTask;
- не SQLite;
- не logs.

======================================================================
58. OFFICIAL WEB FALLBACK
======================================================================

Создать:
VKWebViewService.

Если API не предоставляет определённую функцию:
1. открыть официальную VK страницу;
2. показать через WebView2;
3. не вмешиваться в DRM/protected playback;
4. не извлекать защищённые media URLs.

======================================================================
59. OFFLINE MODE
======================================================================

Без интернета пользователь должен иметь доступ к:
- скачанным видео;
- истории;
- локальным плейлистам;
- избранному;
- настройкам.

Показывать:
«Нет подключения. Скачанные видео доступны офлайн.»

======================================================================
60. MINI PLAYER
======================================================================

Отдельное окно:
- Always on top;
- resize;
- drag;
- play/pause;
- previous/next;
- volume;
- close;
- restore.

======================================================================
61. PICTURE-IN-PICTURE
======================================================================

Отдельное floating window:
- Always on top;
- resize;
- reposition;
- close;
- restore.

======================================================================
62. SYSTEM TRAY
======================================================================

Tray menu:
- Продолжить;
- Пауза;
- Загрузки;
- Открыть;
- Выход.

======================================================================
63. CACHE
======================================================================

%LOCALAPPDATA%\VKVideoDesktop\Cache

Кэшировать:
- thumbnails;
- channel images;
- metadata.

Добавить:
«Очистить кэш».

======================================================================
64. LOGGING
======================================================================

%LOCALAPPDATA%\VKVideoDesktop\Logs

Уровни:
Debug
Info
Warning
Error
Critical

Никогда не писать в logs:
- passwords;
- OAuth tokens;
- cookies;
- Authorization headers;
- private credentials.

======================================================================
65. ERROR HANDLING
======================================================================

Типы:
NetworkError
AuthenticationError
AccessDenied
VideoUnavailable
DownloadUnavailable
RateLimited
ServerError
StorageError
InsufficientSpace
Timeout
RangeNotSupported
Cancelled
UnknownError

Пользователю показывать понятный текст, а не stack trace.

======================================================================
66. PERFORMANCE
======================================================================

Требования:
- UI never blocks;
- async networking;
- lazy loading;
- virtualized lists;
- thumbnail caching;
- limited concurrency;
- cancellation;
- debounce;
- memory-conscious video/download handling.

======================================================================
67. ACCESSIBILITY
======================================================================

Поддержать:
- keyboard navigation;
- Tab navigation;
- focus states;
- screen readers;
- high contrast;
- tooltips;
- UI scaling.

======================================================================
68. LOCALIZATION
======================================================================

С первого релиза:
- Русский;
- English.

Все строки через resources/RESW.

Не хранить UI strings непосредственно в XAML/C#.

======================================================================
69. SETTINGS
======================================================================

Общие:
- язык;
- тема;
- запуск с Windows;
- tray;
- notifications.

Видео:
- default quality;
- autoplay;
- volume;
- playback speed.

Загрузки:
- folder;
- concurrency;
- speed limit;
- retry;
- auto resume;
- notifications;
- .part cleanup;
- quality behavior.

Сеть:
- proxy;
- timeout;
- retry;
- bandwidth limit.

======================================================================
70. DEEP LINKS
======================================================================

Поддержать открытие VK Video links, если это возможно.

При открытии:
1. запустить приложение;
2. распознать video ID;
3. открыть Video Page.

Не принимать произвольные небезопасные URI schemes.

======================================================================
71. TELEMETRY
======================================================================

По умолчанию telemetry OFF.

Если потребуется:
- explicit opt-in;
- anonymous;
- отдельная настройка;
- не собирать credentials;
- не собирать просмотренные URL без согласия.

======================================================================
72. TESTING
======================================================================

Unit tests:

SearchServiceTests
VideoServiceTests
HistoryServiceTests
PlaylistServiceTests
DownloadServiceTests
DownloadManagerTests
DownloadRecoveryTests
SettingsServiceTests

Download Engine tests:
- basic download;
- empty response;
- network failure;
- timeout;
- cancellation;
- pause;
- resume;
- Range;
- retry;
- insufficient disk;
- duplicate filename;
- corrupted .part;
- server returns 200 on resume;
- server returns 206;
- checksum validation if available.

UI tests:
- open;
- search;
- play;
- pause;
- fullscreen;
- favorite;
- playlist;
- add download;
- pause download;
- resume;
- cancel;
- retry;
- open downloaded file.

======================================================================
73. DOWNLOAD STATE MACHINE
======================================================================

Допустимые переходы:

Queued
 ↓
Resolving
 ↓
Downloading
 ↓
Completed

Downloading
 ↓
Paused
 ↓
Downloading

Downloading
 ↓
Failed
 ↓
Retrying
 ↓
Resolving

Downloading
 ↓
Cancelled

Запретить некорректные переходы.

======================================================================
74. INSTALLER
======================================================================

Создать installer:
- MSIX либо WiX/Inno Setup.

Installer:
- Start Menu;
- Desktop shortcut по выбору;
- uninstall;
- upgrade;
- корректная регистрация приложения.

======================================================================
75. APP ICON
======================================================================

Современная иконка:
- V;
- play symbol;
- video frame.

Не копировать официальный логотип VK пиксель-в-пиксель.

======================================================================
76. EMPTY STATES
======================================================================

Для каждого раздела:
- понятная иллюстрация;
- описание;
- действие.

Пример:
«Ваш список пуст.
Добавляйте видео в избранное, чтобы быстро находить их здесь.»

======================================================================
77. LOADING STATES
======================================================================

Использовать skeleton loading.

Не показывать только «Loading...».

======================================================================
78. ПРОИЗВОДСТВЕННЫЕ ТРЕБОВАНИЯ
======================================================================

Не создавать проект одним огромным файлом.

Использовать:
- SOLID;
- MVVM;
- DI;
- Repository pattern там, где уместно;
- interfaces;
- cancellation;
- async/await;
- structured logging;
- separation of concerns.

Не оставлять production-заглушки:
TODO;
NotImplementedException;
FakeDownload;
MockDownload.

Mocks допускаются только внутри tests.

======================================================================
79. SECRETS
======================================================================

Создать:
.env.example

Не помещать:
- API keys;
- OAuth secrets;
- credentials;
- tokens

в Git.

Использовать конфигурационный слой:
appsettings.json
appsettings.Development.json

Секреты получать из безопасной среды/хранилища.

======================================================================
80. ПОРЯДОК РАЗРАБОТКИ
======================================================================

Этап 1:
Solution + architecture.

Этап 2:
WinUI Shell + navigation.

Этап 3:
Home/Search UI.

Этап 4:
Models + Provider abstraction.

Этап 5:
VK API integration.

Этап 6:
Video Page + Player.

Этап 7:
Authentication.

Этап 8:
SQLite + History/Favorites/Playlists.

Этап 9:
Queue + Mini Player + PiP.

Этап 10:
Download subsystem:
- models;
- database;
- source resolver;
- engine;
- queue;
- progress;
- pause/resume;
- retry;
- recovery;
- notifications;
- settings.

Этап 11:
WebView2 fallback.

Этап 12:
Performance/security review.

Этап 13:
Unit/integration/UI tests.

Этап 14:
Installer.

Этап 15:
Final QA.

После каждого этапа:
1. build;
2. run tests;
3. исправить compilation errors;
4. запустить приложение;
5. проверить UI;
6. обновить tests;
7. только затем переходить дальше.

======================================================================
81. FINAL USER WORKFLOW
======================================================================

Просмотр:

Запуск
↓
Главная
↓
Поиск
↓
Видео
↓
Play
↓
Fullscreen / PiP
↓
Favorite / Playlist
↓
Queue
↓
Next video

Скачивание:

Видео
↓
Скачать
↓
Получить реальные DownloadOptions
↓
Показать качество / формат / размер
↓
Проверить доступность
↓
Выбрать папку
↓
Добавить в очередь
↓
Resolve source
↓
Проверить диск
↓
Download streaming
↓
Progress
↓
Speed / ETA
↓
Pause / Resume при необходимости
↓
Retry при временной ошибке
↓
Validate
↓
Atomic rename
↓
SQLite Completed
↓
Windows notification
↓
Открыть файл

======================================================================
82. КРИТЕРИЙ ГОТОВНОСТИ
======================================================================

Приложение считается готовым, когда:

- Windows 10/11 запускает приложение без ошибок;
- UI не блокируется;
- работает поиск;
- открываются видео;
- работает плеер;
- работает fullscreen;
- работает PiP/Mini Player;
- работает авторизация официальным способом;
- работает история;
- работает избранное;
- работают плейлисты;
- работает очередь;
- работает полноценный Download Manager;
- реально доступные варианты скачивания определяются динамически;
- потоковая загрузка не съедает RAM;
- работает progress;
- работает speed;
- работает ETA;
- работает pause;
- работает resume;
- работает HTTP Range resume при поддержке сервером;
- работает retry;
- работает cancellation;
- работает recovery после перезапуска;
- проверяется свободное место;
- работает speed limit;
- работает batch download;
- работают Windows notifications;
- корректно обрабатываются ошибки;
- credentials не попадают в logs/database;
- поддерживается HiDPI;
- работает installer/uninstaller;
- есть unit/integration/UI tests.

======================================================================
83. ГЛАВНАЯ КОМАНДА AI-КОДЕРУ
======================================================================

Начни разработку VK Video Desktop с архитектуры и Shell интерфейса.

Не переходи сразу к реализации Download Engine.

Сначала создай компилируемый Windows-проект и базовую архитектуру, затем реализуй UI, модели, сервисы и provider abstraction.

После этого реализуй Video Page и Player.

Затем Authentication и SQLite.

После стабилизации основной части реализуй полноценный Download subsystem согласно этому документу.

Download subsystem не должен быть заглушкой. Он должен включать:
- Download Manager;
- Queue;
- Streaming;
- Progress;
- Speed;
- ETA;
- Pause;
- Resume;
- HTTP Range;
- Retry;
- Cancellation;
- Recovery;
- SQLite persistence;
- Disk checks;
- Notifications;
- Duplicate filename handling;
- Temporary files;
- Atomic completion;
- Error handling;
- Source resolution;
- Security validation.

Используй только официальные и разрешённые приложению источники контента.

Если конкретное видео не имеет доступного официального способа сохранения, не пытайся обходить это ограничение. Покажи пользователю понятное сообщение и продолжи работу остальных функций приложения.

Главный результат:
полноценное профессиональное Windows-приложение VK Video Desktop с качественным desktop UI, просмотром VK Видео и полноценным, надёжным менеджером разрешённых загрузок.
