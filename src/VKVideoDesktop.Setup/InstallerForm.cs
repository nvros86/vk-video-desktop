using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace VKVideoDesktop.Setup;

public class InstallerForm : Form
{
    private Panel _contentPanel = null!;
    private Label _titleLabel = null!;
    private Label _subtitleLabel = null!;
    private Button _nextButton = null!;
    private Button _backButton = null!;
    private Button _cancelButton = null!;
    private Panel _sidebarPanel = null!;
    private ProgressBar _progressBar = null!;
    private Label _statusLabel = null!;
    private TextBox _pathBox = null!;
    private RichTextBox _licenseBox = null!;
    private CheckBox _desktopCheck = null!;
    private CheckBox _startMenuCheck = null!;
    private CheckBox _protocolCheck = null!;

    private int _currentPage;
    private string _installPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        "VK Video Desktop");
    private CancellationTokenSource? _installCts;

    private static readonly Color AccentColor = Color.FromArgb(76, 175, 80);
    private static readonly Color SidebarColor = Color.FromArgb(33, 33, 33);
    private static readonly Color BackgroundColor = Color.FromArgb(250, 250, 250);
    private static readonly Color TextColor = Color.FromArgb(33, 33, 33);
    private static readonly Color SubtextColor = Color.FromArgb(117, 117, 117);

    public InstallerForm()
    {
        SetupLogger.Info("=== Установщик VK Video Desktop запущен ===");
        SetupLogger.Info($"Версия ОС: {Environment.OSVersion}");
        SetupLogger.Info($"64-bit: {Environment.Is64BitOperatingSystem}");
        SetupLogger.Info($"Пользователь: {Environment.UserName}");
        InitializeUI();
        TrySetFormIcon();
    }

    private void TrySetFormIcon()
    {
        try
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "icon.ico"),
                Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets", "icon.ico")
            };
            foreach (var path in candidates)
            {
                var full = Path.GetFullPath(path);
                if (File.Exists(full))
                {
                    Icon = new Icon(full);
                    SetupLogger.Info($"Иконка загружена: {full}");
                    return;
                }
            }

            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var iconStream = assembly.GetManifestResourceStream("VKVideoDesktop.Setup.icon.ico");
            if (iconStream != null)
            {
                Icon = new Icon(iconStream);
                iconStream.Dispose();
                SetupLogger.Info("Иконка загружена из ресурсов сборки");
            }
            else
            {
                SetupLogger.Warn("Иконка не найдена");
            }
        }
        catch (Exception ex)
        {
            SetupLogger.Error("Ошибка загрузки иконки", ex);
        }
    }

    private void InitializeUI()
    {
        Text = "VK Video Desktop — Установка";
        Size = new Size(720, 520);
        MinimumSize = new Size(720, 520);
        MaximumSize = new Size(720, 520);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        BackColor = BackgroundColor;

        _sidebarPanel = new Panel
        {
            Dock = DockStyle.Left,
            Width = 210,
            BackColor = SidebarColor
        };

        var logoLabel = new Label
        {
            Text = "VK Video\nDesktop",
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = false,
            Size = new Size(200, 80),
            Location = new Point(10, 30),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _sidebarPanel.Controls.Add(logoLabel);

        var versionLabel = new Label
        {
            Text = "v1.0.0",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(150, 150, 150),
            AutoSize = true,
            Location = new Point(14, 115)
        };
        _sidebarPanel.Controls.Add(versionLabel);

        var steps = new[] { "Приветствие", "Лицензия", "Папка установки", "Установка", "Завершение" };
        for (int i = 0; i < steps.Length; i++)
        {
            var stepLabel = new Label
            {
                Text = $"  {i + 1}.  {steps[i]}",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = i == 0 ? AccentColor : Color.FromArgb(140, 140, 140),
                AutoSize = true,
                Location = new Point(14, 160 + i * 28),
                Tag = i
            };
            _sidebarPanel.Controls.Add(stepLabel);
        }

        Controls.Add(_sidebarPanel);

        _titleLabel = new Label
        {
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = TextColor,
            AutoSize = true,
            Location = new Point(240, 30)
        };
        Controls.Add(_titleLabel);

        _subtitleLabel = new Label
        {
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = SubtextColor,
            AutoSize = false,
            Size = new Size(440, 40),
            Location = new Point(240, 70)
        };
        Controls.Add(_subtitleLabel);

        _contentPanel = new Panel
        {
            Location = new Point(240, 115),
            Size = new Size(440, 300),
            BackColor = BackgroundColor
        };
        Controls.Add(_contentPanel);

        _progressBar = new ProgressBar
        {
            Location = new Point(240, 430),
            Size = new Size(440, 6),
            Style = ProgressBarStyle.Continuous,
            Visible = false
        };
        Controls.Add(_progressBar);

        _statusLabel = new Label
        {
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = SubtextColor,
            AutoSize = false,
            Size = new Size(440, 30),
            Location = new Point(240, 440),
            Visible = false,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        Controls.Add(_statusLabel);

        var buttonPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            BackColor = Color.FromArgb(240, 240, 240)
        };

        _cancelButton = new Button
        {
            Text = "Отмена",
            Size = new Size(90, 34),
            Location = new Point(590, 11),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = TextColor,
            Cursor = Cursors.Hand
        };
        _cancelButton.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
        _cancelButton.Click += (_, _) => Close();
        buttonPanel.Controls.Add(_cancelButton);

        _nextButton = new Button
        {
            Text = "Далее →",
            Size = new Size(110, 34),
            Location = new Point(470, 11),
            FlatStyle = FlatStyle.Flat,
            BackColor = AccentColor,
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
        };
        _nextButton.FlatAppearance.BorderSize = 0;
        _nextButton.Click += NextButton_Click;
        buttonPanel.Controls.Add(_nextButton);

        _backButton = new Button
        {
            Text = "← Назад",
            Size = new Size(90, 34),
            Location = new Point(375, 11),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = TextColor,
            Cursor = Cursors.Hand,
            Visible = false
        };
        _backButton.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
        _backButton.Click += (_, _) => ShowPage(_currentPage - 1);
        buttonPanel.Controls.Add(_backButton);

        Controls.Add(buttonPanel);

        ShowWelcomePage();
    }

    private void UpdateStepHighlights()
    {
        foreach (Control c in _sidebarPanel.Controls)
        {
            if (c is Label lbl && lbl.Tag is int idx)
            {
                lbl.ForeColor = idx == _currentPage ? AccentColor :
                    idx < _currentPage ? Color.FromArgb(100, 200, 130) :
                    Color.FromArgb(140, 140, 140);
            }
        }
    }

    private void ShowPage(int page)
    {
        _currentPage = page;
        _contentPanel.Controls.Clear();
        _backButton.Visible = page > 0 && page < 3;
        _nextButton.Visible = page < 4;
        UpdateStepHighlights();

        switch (page)
        {
            case 0: ShowWelcomePage(); break;
            case 1: ShowLicensePage(); break;
            case 2: ShowPathPage(); break;
            case 3: StartInstallation(); break;
            case 4: ShowCompletePage(); break;
        }
    }

    private void ShowWelcomePage()
    {
        _titleLabel.Text = "Добро пожаловать!";
        _subtitleLabel.Text = "Мастер установки поможет установить VK Video Desktop на ваш компьютер.";

        var iconLabel = new Label
        {
            Text = "▶",
            Font = new Font("Segoe UI", 48f),
            ForeColor = AccentColor,
            AutoSize = true,
            Location = new Point(180, 60),
            BackColor = Color.Transparent
        };
        _contentPanel.Controls.Add(iconLabel);

        var descLabel = new Label
        {
            Text = "VK Video Desktop — десктопный клиент для просмотра и\nскачивания видео из VK. Включает видеоплеер, менеджер\nзагрузок, мини-плеер, системный трей и горячие клавиши.",
            Font = new Font("Segoe UI", 10f),
            ForeColor = SubtextColor,
            AutoSize = false,
            Size = new Size(420, 80),
            Location = new Point(10, 180),
            TextAlign = ContentAlignment.TopCenter
        };
        _contentPanel.Controls.Add(descLabel);

        _nextButton.Text = "Далее →";
    }

    private void ShowLicensePage()
    {
        _titleLabel.Text = "Лицензионное соглашение";
        _subtitleLabel.Text = "Прочитайте и примите условия лицензии для продолжения.";

        _licenseBox = new RichTextBox
        {
            ReadOnly = true,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            Location = new Point(0, 0),
            Size = new Size(440, 240),
            Font = new Font("Segoe UI", 9f),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Text = @"MIT License

Copyright (c) 2026 VK Video Desktop Contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the ""Software""), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE."
        };
        _contentPanel.Controls.Add(_licenseBox);

        _nextButton.Text = "Принимаю";
    }

    private void ShowPathPage()
    {
        _titleLabel.Text = "Папка установки";
        _subtitleLabel.Text = "Выберите папку для установки VK Video Desktop.";

        var pathLabel = new Label
        {
            Text = "Папка установки:",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = TextColor,
            AutoSize = true,
            Location = new Point(0, 10)
        };
        _contentPanel.Controls.Add(pathLabel);

        _pathBox = new TextBox
        {
            Text = _installPath,
            Font = new Font("Segoe UI", 10f),
            Location = new Point(0, 38),
            Size = new Size(340, 28),
            BorderStyle = BorderStyle.FixedSingle
        };
        _contentPanel.Controls.Add(_pathBox);

        var browseButton = new Button
        {
            Text = "...",
            Font = new Font("Segoe UI", 10f),
            Location = new Point(348, 37),
            Size = new Size(50, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            Cursor = Cursors.Hand
        };
        browseButton.Click += BrowseButton_Click;
        _contentPanel.Controls.Add(browseButton);

        _desktopCheck = new CheckBox
        {
            Text = "Ярлык на рабочем столе",
            Checked = true,
            Font = new Font("Segoe UI", 9.5f),
            AutoSize = true,
            Location = new Point(0, 90)
        };
        _contentPanel.Controls.Add(_desktopCheck);

        _startMenuCheck = new CheckBox
        {
            Text = "Ярлык в меню «Пуск»",
            Checked = true,
            Font = new Font("Segoe UI", 9.5f),
            AutoSize = true,
            Location = new Point(0, 120)
        };
        _contentPanel.Controls.Add(_startMenuCheck);

        _protocolCheck = new CheckBox
        {
            Text = "Регистрация протокола vkvideo://",
            Checked = true,
            Font = new Font("Segoe UI", 9.5f),
            AutoSize = true,
            Location = new Point(0, 150)
        };
        _contentPanel.Controls.Add(_protocolCheck);

        var sizeInfo = new Label
        {
            Text = "Требуется ~120 МБ свободного места на диске.",
            Font = new Font("Segoe UI", 9f),
            ForeColor = SubtextColor,
            AutoSize = true,
            Location = new Point(0, 200)
        };
        _contentPanel.Controls.Add(sizeInfo);

        _nextButton.Text = "Установить";
    }

    private void NextButton_Click(object? sender, EventArgs e)
    {
        if (_currentPage == 1)
        {
            ShowPage(2);
        }
        else if (_currentPage == 2)
        {
            _installPath = _pathBox.Text;
            ShowPage(3);
        }
        else
        {
            ShowPage(_currentPage + 1);
        }
    }

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            SelectedPath = _installPath,
            Description = "Выберите папку для установки"
        };
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _installPath = dialog.SelectedPath;
            _pathBox.Text = _installPath;
        }
    }

    private async void StartInstallation()
    {
        SetupLogger.Info("Начало установки");
        SetupLogger.Info($"Путь установки: {_installPath}");
        SetupLogger.Info($"Ярлык на рабочем столе: {_desktopCheck.Checked}");
        SetupLogger.Info($"Ярлык в Пуск: {_startMenuCheck.Checked}");
        SetupLogger.Info($"Протокол vkvideo://: {_protocolCheck.Checked}");

        _titleLabel.Text = "Установка";
        _subtitleLabel.Text = "Подождите, пока VK Video Desktop устанавливается...";

        _progressBar.Visible = true;
        _progressBar.Value = 0;
        _statusLabel.Visible = true;
        _nextButton.Visible = false;
        _backButton.Visible = false;
        _cancelButton.Visible = false;

        _installCts = new CancellationTokenSource();

        try
        {
            await Task.Run(() => DoInstall(_installCts.Token));
            SetupLogger.Info("Установка завершена успешно");
            ShowPage(4);
        }
        catch (OperationCanceledException)
        {
            SetupLogger.Warn("Установка отменена пользователем");
            _statusLabel.Text = "Установка отменена.";
            _nextButton.Text = "Закрыть";
            _nextButton.Visible = true;
            _cancelButton.Visible = false;
        }
        catch (Exception ex)
        {
            SetupLogger.Fatal("Ошибка установки", ex);
            _statusLabel.Text = $"Ошибка: {ex.Message}";
            _nextButton.Text = "Закрыть";
            _nextButton.Visible = true;
            _cancelButton.Visible = false;
        }
    }

    private void DoInstall(CancellationToken ct)
    {
        SetProgress("Проверка Windows App Runtime...", 3);
        SetupLogger.Info("Проверка Windows App Runtime...");
        var winrtOk = EnsureWindowsAppRuntimeInstalled(ct);
        if (!winrtOk)
        {
            SetupLogger.Warn("Windows App Runtime не установлен");
            SetProgress("Windows App Runtime не установлен (приложение может потребовать его позже)...", 8);
            Thread.Sleep(1500);
        }
        else
        {
            SetupLogger.Info("Windows App Runtime установлен");
        }

        SetProgress("Извлечение файлов...", 10);
        SetupLogger.Info("Создание папки установки...");
        Directory.CreateDirectory(_installPath);

        var zipData = GetEmbeddedZip();
        SetupLogger.Info($"Размер встроенного архива: {zipData.Length / 1024} КБ");
        var zipPath = Path.Combine(_installPath, "_app.zip");
        File.WriteAllBytes(zipPath, zipData);

        SetProgress("Распаковка архива...", 30);
        SetupLogger.Info("Распаковка архива...");
        ZipFile.ExtractToDirectory(zipPath, _installPath, overwriteFiles: true);
        File.Delete(zipPath);
        SetupLogger.Info("Архив распакован");

        SetProgress("Создание ярлыков...", 60);
        if (_desktopCheck.Checked)
        {
            SetupLogger.Info("Создание ярлыка на рабочем столе...");
            CreateDesktopShortcut();
        }
        if (_startMenuCheck.Checked)
        {
            SetupLogger.Info("Создание ярлыка в меню Пуск...");
            CreateStartMenuShortcut();
        }

        SetProgress("Регистрация протокола...", 75);
        if (_protocolCheck.Checked)
        {
            SetupLogger.Info("Регистрация протокола vkvideo://...");
            RegisterProtocol();
        }

        SetProgress("Регистрация в реестре...", 85);
        SetupLogger.Info("Создание записи в реестре для удаления...");
        RegisterUninstallEntry();

        SetProgress("Завершение...", 95);
        Thread.Sleep(300);

        SetupLogger.Info("=== Установка завершена ===");
        SetupLogger.Info($"Путь установки: {_installPath}");
        SetupLogger.Info($"Лог установки: {SetupLogger.LogPath}");
        SetProgress("Готово!", 100);
    }

    private byte[] GetEmbeddedZip()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var names = assembly.GetManifestResourceNames();
        var zipName = names.FirstOrDefault(n => n.EndsWith("app.zip", StringComparison.OrdinalIgnoreCase))
            ?? names.First(n => n.Contains("VKVideoDesktop-win-x64"));

        using var stream = assembly.GetManifestResourceStream(zipName)!;
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    private void SetProgress(string status, int percent)
    {
        if (InvokeRequired)
        {
            Invoke(() => SetProgress(status, percent));
            return;
        }
        _statusLabel.Text = status;
        _progressBar.Value = percent;
    }

    private static bool IsWindowsAppRuntimeInstalled()
    {
        try
        {
            var systemDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
            var runtimeDll = Path.Combine(systemDir, "Microsoft.WindowsAppRuntime.dll");
            if (File.Exists(runtimeDll)) return true;

            var systemDirX86 = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
            if (!string.IsNullOrEmpty(systemDirX86))
            {
                var runtimeDllX86 = Path.Combine(systemDirX86, "Microsoft.WindowsAppRuntime.dll");
                if (File.Exists(runtimeDllX86)) return true;
            }

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dynamicDir = Path.Combine(localAppData, "Microsoft", "WindowsAppRuntime");
            if (Directory.Exists(dynamicDir)) return true;

            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows App Runtime");
            if (key != null)
            {
                var version = key.GetValue("Version")?.ToString();
                if (!string.IsNullOrEmpty(version)) return true;
            }

            using var keyWow = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\WOW6432Node\Microsoft\Windows App Runtime");
            if (keyWow != null)
            {
                var version = keyWow.GetValue("Version")?.ToString();
                if (!string.IsNullOrEmpty(version)) return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private bool EnsureWindowsAppRuntimeInstalled(CancellationToken ct)
    {
        SetupLogger.Info("Проверка наличия Windows App Runtime...");
        if (IsWindowsAppRuntimeInstalled())
        {
            SetupLogger.Info("Windows App Runtime уже установлен");
            return true;
        }

        SetupLogger.Warn("Windows App Runtime не найден");

        SetupLogger.Info("Попытка установки через winget...");
        try
        {
            SetProgress("Установка Windows App Runtime через winget...", 3);
            var wingetPsi = new ProcessStartInfo("winget",
                "install --id Microsoft.WindowsAppRuntime.1.7 --accept-source-agreements --accept-package-agreements")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var wingetProc = Process.Start(wingetPsi);
            if (wingetProc != null)
            {
                var wingetOutput = wingetProc.StandardOutput.ReadToEnd();
                var wingetError = wingetProc.StandardError.ReadToEnd();
                wingetProc.WaitForExit(300_000);
                SetupLogger.Info($"winget exit code: {wingetProc.ExitCode}");
                if (!string.IsNullOrEmpty(wingetOutput)) SetupLogger.Info($"winget stdout: {wingetOutput.Trim()}");
                if (!string.IsNullOrEmpty(wingetError)) SetupLogger.Info($"winget stderr: {wingetError.Trim()}");

                if (wingetProc.ExitCode == 0 && IsWindowsAppRuntimeInstalled())
                {
                    SetupLogger.Info("Windows App Runtime установлен через winget");
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            SetupLogger.Info($"winget недоступен: {ex.Message}");
        }

        SetupLogger.Info("Скачивание Windows App Runtime с direct URL...");
        var tempFile = Path.Combine(Path.GetTempPath(), $"VKSetup_WinRT_{Guid.NewGuid():N}.exe");
        try
        {
            var downloadUrls = new[]
            {
                "https://aka.ms/windowsappsdk/1.7/1.7.260224002/windowsappruntimeinstall-x64.exe",
                "https://aka.ms/windowsappsdk/1.7/1.7.250310001/windowsappruntimeinstall-x64.exe"
            };

            bool downloaded = false;
            foreach (var url in downloadUrls)
            {
                try
                {
                    SetProgress($"Скачивание Windows App Runtime...", 5);
                    SetupLogger.Info($"Скачивание с {url} ...");
                    using var handler = new System.Net.Http.HttpClientHandler
                    {
                        AllowAutoRedirect = true,
                        MaxAutomaticRedirections = 10
                    };
                    using var client = new System.Net.Http.HttpClient(handler);
                    client.Timeout = TimeSpan.FromMinutes(5);
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("VKVideoDesktop/1.0");

                    var response = client.GetAsync(url, ct).GetAwaiter().GetResult();
                    response.EnsureSuccessStatusCode();
                    var data = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                    File.WriteAllBytes(tempFile, data);
                    SetupLogger.Info($"Скачано: {data.Length / 1024} КБ из {response.Content.Headers.ContentLength / 1024} КБ");

                    if (data.Length < 1_000_000)
                    {
                        SetupLogger.Warn($"Файл слишком маленький ({data.Length} байт) — возможно HTML-страница, пропускаем");
                        try { File.Delete(tempFile); } catch { }
                        continue;
                    }

                    downloaded = true;
                    break;
                }
                catch (Exception ex)
                {
                    SetupLogger.Warn($"Не удалось скачать с {url}: {ex.Message}");
                }
            }

            if (!downloaded)
            {
                SetupLogger.Warn("Не удалось скачать Windows App Runtime, открываем страницу загрузки в браузере...");
                SetProgress("Не удалось скачать автоматически. Открываю страницу загрузки...", 5);
                Process.Start(new ProcessStartInfo(
                    "https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads-archive")
                { UseShellExecute = true });
                return false;
            }

            SetProgress("Установка Windows App Runtime... (может потребовать подтверждение UAC)", 8);
            SetupLogger.Info("Запуск установщика Windows App Runtime...");
            var psi = new ProcessStartInfo(tempFile, "--quiet --accept-license --force")
            {
                UseShellExecute = true,
                Verb = "runas"
            };
            using var proc = Process.Start(psi)!;
            proc.WaitForExit(300_000);
            var exitCode = proc.ExitCode;
            SetupLogger.Info($"Установщик WinRT завершён с кодом: {exitCode}");

            if (exitCode == 0 || exitCode == 0x80070005)
            {
                if (IsWindowsAppRuntimeInstalled()) return true;
            }

            var result = IsWindowsAppRuntimeInstalled();
            SetupLogger.Info($"Проверка после установки: {(result ? "установлен" : "не установлен")}");
            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            SetupLogger.Error("Ошибка установки Windows App Runtime", ex);
            return false;
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    private void CreateDesktopShortcut()
    {
        var exePath = Path.Combine(_installPath, "VKVideoDesktop.App.exe");
        if (!File.Exists(exePath))
        {
            SetupLogger.Warn($"Файл не найден, ярлык не создан: {exePath}");
            return;
        }

        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var shortcutPath = Path.Combine(desktopPath, "VK Video Desktop.lnk");
        CreateShortcut(shortcutPath, exePath, "VK Video Desktop");
    }

    private void CreateStartMenuShortcut()
    {
        var exePath = Path.Combine(_installPath, "VKVideoDesktop.App.exe");
        if (!File.Exists(exePath))
        {
            SetupLogger.Warn($"Файл не найден, ярлык не создан: {exePath}");
            return;
        }

        var startMenu = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
        var programsDir = Path.Combine(startMenu, "Programs", "VK Video Desktop");
        Directory.CreateDirectory(programsDir);
        var shortcutPath = Path.Combine(programsDir, "VK Video Desktop.lnk");
        CreateShortcut(shortcutPath, exePath, "VK Video Desktop");
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string description)
    {
        var workingDir = Path.GetDirectoryName(targetPath) ?? targetPath;
        var escapedShortcut = shortcutPath.Replace("'", "''");
        var escapedTarget = targetPath.Replace("'", "''");
        var escapedWorkDir = workingDir.Replace("'", "''");
        var escapedDesc = description.Replace("'", "''");

        var scriptContent = $@"$wsh = New-Object -ComObject WScript.Shell
$link = $wsh.CreateShortcut('{escapedShortcut}')
$link.TargetPath = '{escapedTarget}'
$link.WorkingDirectory = '{escapedWorkDir}'
$link.Description = '{escapedDesc}'
$link.IconLocation = '{escapedTarget},0'
$link.Save()";

        var tempScript = Path.Combine(Path.GetTempPath(), $"create_lnk_{Guid.NewGuid():N}.ps1");
        File.WriteAllText(tempScript, scriptContent, Encoding.UTF8);
        try
        {
            var psi = new ProcessStartInfo("powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -File \"{tempScript}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(15000);
            SetupLogger.Info($"Ярлык создан: {shortcutPath}");
        }
        catch (Exception ex)
        {
            SetupLogger.Error($"Ошибка создания ярлыка: {shortcutPath}", ex);
        }
        finally
        {
            try { File.Delete(tempScript); } catch { }
        }
    }

    private void RegisterProtocol()
    {
        var exePath = Path.Combine(_installPath, "VKVideoDesktop.App.exe");
        if (!File.Exists(exePath))
        {
            SetupLogger.Warn($"Файл не найден, протокол не зарегистрирован: {exePath}");
            return;
        }

        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Classes\vkvideo");
            key?.SetValue("", "URL: VK Video Desktop");
            key?.SetValue("URL Protocol", "");

            using var icon = key?.CreateSubKey("DefaultIcon");
            icon?.SetValue("", $"\"{exePath}\",0");

            using var command = key?.CreateSubKey(@"shell\open\command");
            command?.SetValue("", $"\"{exePath}\" \"%1\"");
            SetupLogger.Info("Протокол vkvideo:// зарегистрирован");
        }
        catch (Exception ex)
        {
            SetupLogger.Error("Ошибка регистрации протокола", ex);
        }
    }

    private void RegisterUninstallEntry()
    {
        var exePath = Path.Combine(_installPath, "VKVideoDesktop.App.exe");
        var uninstallExe = Path.Combine(_installPath, "VKVideoDesktopSetup.exe");

        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Uninstall\VKVideoDesktop");
            key?.SetValue("DisplayName", "VK Video Desktop");
            key?.SetValue("DisplayVersion", "1.0.0");
            key?.SetValue("Publisher", "VK Video Desktop Contributors");
            key?.SetValue("InstallLocation", _installPath);
            key?.SetValue("UninstallString", $"\"{uninstallExe}\" --uninstall");
            key?.SetValue("QuietUninstallString", $"\"{uninstallExe}\" --uninstall --silent");
            key?.SetValue("NoModify", 1);
            key?.SetValue("NoRepair", 1);
            key?.SetValue("EstimatedSize", 119000);
            SetupLogger.Info("Запись в реестре для удаления создана");
        }
        catch (Exception ex)
        {
            SetupLogger.Error("Ошибка создания записи в реестре", ex);
        }
    }

    private void ShowCompletePage()
    {
        _titleLabel.Text = "Установка завершена!";
        _subtitleLabel.Text = "VK Video Desktop успешно установлен.";
        _progressBar.Visible = false;
        _statusLabel.Visible = false;
        _nextButton.Visible = false;
        _backButton.Visible = false;
        _cancelButton.Visible = false;

        UpdateStepHighlightsToComplete();

        var successLabel = new Label
        {
            Text = "✓",
            Font = new Font("Segoe UI", 48f),
            ForeColor = AccentColor,
            AutoSize = true,
            Location = new Point(190, 15),
            BackColor = Color.Transparent
        };
        _contentPanel.Controls.Add(successLabel);

        var details = new Label
        {
            Text = $"Папка: {_installPath}\n\nVK Video Desktop готов к использованию.\nВы можете запустить приложение из папки установки\nили через ярлык на рабочем столе.",
            Font = new Font("Segoe UI", 10f),
            ForeColor = SubtextColor,
            AutoSize = false,
            Size = new Size(420, 100),
            Location = new Point(10, 110),
            TextAlign = ContentAlignment.TopCenter
        };
        _contentPanel.Controls.Add(details);

        var launchCheck = new CheckBox
        {
            Text = "Запустить VK Video Desktop",
            Checked = true,
            Font = new Font("Segoe UI", 10f),
            AutoSize = true,
            Location = new Point(130, 220)
        };
        _contentPanel.Controls.Add(launchCheck);

        var finishButton = new Button
        {
            Text = "Готово",
            Size = new Size(140, 40),
            Location = new Point(150, 260),
            FlatStyle = FlatStyle.Flat,
            BackColor = AccentColor,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        finishButton.FlatAppearance.BorderSize = 0;
        finishButton.Click += (_, _) =>
        {
            if (launchCheck.Checked)
            {
                var exePath = Path.Combine(_installPath, "VKVideoDesktop.App.exe");
                if (File.Exists(exePath))
                    Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true });
            }
            Close();
        };
        _contentPanel.Controls.Add(finishButton);
    }

    private void UpdateStepHighlightsToComplete()
    {
        var stepNames = new[] { "Приветствие", "Лицензия", "Папка установки", "Установка", "Завершение" };
        foreach (Control c in _sidebarPanel.Controls)
        {
            if (c is Label lbl && lbl.Tag is int idx)
            {
                if (idx < 4)
                {
                    lbl.ForeColor = Color.FromArgb(100, 200, 130);
                    lbl.Text = $"  ✓  {stepNames[idx]}";
                }
                else if (idx == 4)
                {
                    lbl.ForeColor = AccentColor;
                    lbl.Text = $"  ✓  {stepNames[idx]}";
                }
            }
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _installCts?.Cancel();
        base.OnFormClosing(e);
    }
}
