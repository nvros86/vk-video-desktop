using System.Diagnostics;

namespace VKVideoDesktop.Setup;

public class InstallerForm : Form
{
    private readonly Panel _sidebar;
    private readonly Label _title;
    private readonly Label _subtitle;
    private readonly Panel _content;
    private readonly ProgressBar _progress;
    private readonly Label _status;
    private readonly Button _next;
    private readonly Button _back;
    private readonly Button _cancel;
    private readonly Label[] _stepLabels;

    private int _page;
    private string _installPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        "VK Video Desktop");
    private CancellationTokenSource? _cts;

    private static readonly Color Accent = Color.FromArgb(76, 175, 80);
    private static readonly Color Bg = Color.FromArgb(250, 250, 250);
    private static readonly Color Txt = Color.FromArgb(33, 33, 33);
    private static readonly Color Sub = Color.FromArgb(117, 117, 117);

    private static readonly string[] Steps = ["Приветствие", "Лицензия", "Папка установки", "Установка", "Завершение"];

    public InstallerForm()
    {
        SetupLogger.Info("=== Установщик VK Video Desktop ===");
        SetupLogger.Info($"ОС: {Environment.OSVersion}, x64: {Environment.Is64BitOperatingSystem}");

        Text = "VK Video Desktop — Установка";
        Size = new Size(720, 520);
        MinimumSize = MaximumSize = new Size(720, 520);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        BackColor = Bg;

        _sidebar = new Panel { Dock = DockStyle.Left, Width = 210, BackColor = Color.FromArgb(33, 33, 33) };
        Controls.Add(_sidebar);

        _sidebar.Controls.Add(new Label
        {
            Text = "VK Video\nDesktop", Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = Color.White, Size = new Size(200, 80), Location = new Point(10, 30),
            TextAlign = ContentAlignment.MiddleLeft
        });

        _sidebar.Controls.Add(new Label
        {
            Text = "v1.0.0", Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(150, 150, 150), AutoSize = true, Location = new Point(14, 115)
        });

        _stepLabels = new Label[Steps.Length];
        for (int i = 0; i < Steps.Length; i++)
        {
            _stepLabels[i] = new Label
            {
                Text = $"  {i + 1}.  {Steps[i]}", Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(140, 140, 140), AutoSize = true,
                Location = new Point(14, 160 + i * 28)
            };
            _sidebar.Controls.Add(_stepLabels[i]);
        }

        _title = new Label
        {
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = Txt, AutoSize = true, Location = new Point(240, 30)
        };
        Controls.Add(_title);

        _subtitle = new Label
        {
            Font = new Font("Segoe UI", 9.5f), ForeColor = Sub,
            AutoSize = false, Size = new Size(440, 40), Location = new Point(240, 70)
        };
        Controls.Add(_subtitle);

        _content = new Panel
        {
            Location = new Point(240, 115), Size = new Size(440, 300), BackColor = Bg
        };
        Controls.Add(_content);

        _progress = new ProgressBar
        {
            Location = new Point(240, 430), Size = new Size(440, 6),
            Style = ProgressBarStyle.Continuous, Visible = false
        };
        Controls.Add(_progress);

        _status = new Label
        {
            Font = new Font("Segoe UI", 8.5f), ForeColor = Sub,
            AutoSize = false, Size = new Size(440, 30), Location = new Point(240, 440),
            Visible = false, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true
        };
        Controls.Add(_status);

        var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = Color.FromArgb(240, 240, 240) };
        Controls.Add(btnPanel);

        _cancel = MakeBtn("Отмена", 590, Color.White, Txt, (_, _) => Close());
        btnPanel.Controls.Add(_cancel);

        _next = MakeBtn("Далее →", 470, Accent, Color.White, NextClick);
        _next.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        btnPanel.Controls.Add(_next);

        _back = MakeBtn("← Назад", 375, Color.White, Txt, (_, _) => ShowPage(_page - 1));
        _back.Visible = false;
        btnPanel.Controls.Add(_back);

        TrySetIcon();
        ShowPage(0);
    }

    private void ShowPage(int page)
    {
        _page = page;
        _content.Controls.Clear();
        _back.Visible = page is > 0 and < 3;
        _next.Visible = page < 4;

        for (int i = 0; i < _stepLabels.Length; i++)
            _stepLabels[i].ForeColor = i == page ? Accent : i < page ? Color.FromArgb(100, 200, 130) : Color.FromArgb(140, 140, 140);

        switch (page)
        {
            case 0: ShowWelcome(); break;
            case 1: ShowLicense(); break;
            case 2: ShowPath(); break;
            case 3: StartInstall(); break;
            case 4: ShowComplete(); break;
        }
    }

    private void ShowWelcome()
    {
        _title.Text = "Добро пожаловать!";
        _subtitle.Text = "Мастер установки поможет установить VK Video Desktop на ваш компьютер.";
        _content.Controls.Add(new Label { Text = "▶", Font = new Font("Segoe UI", 48f), ForeColor = Accent, AutoSize = true, Location = new Point(180, 60), BackColor = Color.Transparent });
        _content.Controls.Add(new Label { Text = "VK Video Desktop — десктопный клиент для просмотра и\nскачивания видео из VK. Включает видеоплеер, менеджер\nзагрузок, мини-плеер, системный трей и горячие клавиши.", Font = new Font("Segoe UI", 10f), ForeColor = Sub, AutoSize = false, Size = new Size(420, 80), Location = new Point(10, 180), TextAlign = ContentAlignment.TopCenter });
        _next.Text = "Далее →";
    }

    private void ShowLicense()
    {
        _title.Text = "Лицензионное соглашение";
        _subtitle.Text = "Прочитайте и примите условия лицензии для продолжения.";
        _content.Controls.Add(new RichTextBox
        {
            ReadOnly = true, ScrollBars = RichTextBoxScrollBars.Vertical,
            Location = new Point(0, 0), Size = new Size(440, 240),
            Font = new Font("Segoe UI", 9f), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle,
            Text = "MIT License\n\nCopyright (c) 2026 VK Video Desktop Contributors\n\nPermission is hereby granted, free of charge, to any person obtaining a copy\nof this software and associated documentation files (the \"Software\"), to deal\nin the Software without restriction, including without limitation the rights\nto use, copy, modify, merge, publish, distribute, sublicense, and/or sell\ncopies of the Software, and to permit persons to whom the Software is\nfurnished to do so, subject to the following conditions:\n\nThe above copyright notice and this permission notice shall be included in all\ncopies or substantial portions of the Software.\n\nTHE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR\nIMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,\nFITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE\nAUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER\nLIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,\nOUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE\nSOFTWARE."
        });
        _next.Text = "Принимаю";
    }

    private TextBox? _pathBox;
    private CheckBox? _desktopCheck;
    private CheckBox? _startMenuCheck;
    private CheckBox? _protocolCheck;

    private void ShowPath()
    {
        _title.Text = "Папка установки";
        _subtitle.Text = "Выберите папку для установки VK Video Desktop.";
        _content.Controls.Add(new Label { Text = "Папка установки:", Font = new Font("Segoe UI", 9.5f), ForeColor = Txt, AutoSize = true, Location = new Point(0, 10) });

        _pathBox = new TextBox { Text = _installPath, Font = new Font("Segoe UI", 10f), Location = new Point(0, 38), Size = new Size(340, 28), BorderStyle = BorderStyle.FixedSingle };
        _content.Controls.Add(_pathBox);

        var browse = new Button { Text = "...", Font = new Font("Segoe UI", 10f), Location = new Point(348, 37), Size = new Size(50, 30), FlatStyle = FlatStyle.Flat, BackColor = Color.White, Cursor = Cursors.Hand };
        browse.Click += (_, _) => { using var d = new FolderBrowserDialog { SelectedPath = _installPath }; if (d.ShowDialog() == DialogResult.OK) { _installPath = d.SelectedPath; _pathBox.Text = _installPath; } };
        _content.Controls.Add(browse);

        _desktopCheck = new CheckBox { Text = "Ярлык на рабочем столе", Checked = true, Font = new Font("Segoe UI", 9.5f), AutoSize = true, Location = new Point(0, 90) };
        _startMenuCheck = new CheckBox { Text = "Ярлык в меню «Пуск»", Checked = true, Font = new Font("Segoe UI", 9.5f), AutoSize = true, Location = new Point(0, 120) };
        _protocolCheck = new CheckBox { Text = "Регистрация протокола vkvideo://", Checked = true, Font = new Font("Segoe UI", 9.5f), AutoSize = true, Location = new Point(0, 150) };
        _content.Controls.Add(_desktopCheck);
        _content.Controls.Add(_startMenuCheck);
        _content.Controls.Add(_protocolCheck);

        _content.Controls.Add(new Label { Text = "Требуется ~120 МБ свободного места на диске.", Font = new Font("Segoe UI", 9f), ForeColor = Sub, AutoSize = true, Location = new Point(0, 200) });
        _next.Text = "Установить";
    }

    private void NextClick(object? s, EventArgs e)
    {
        if (_page == 1) ShowPage(2);
        else if (_page == 2) { _installPath = _pathBox?.Text ?? _installPath; ShowPage(3); }
        else ShowPage(_page + 1);
    }

    private async void StartInstall()
    {
        SetupLogger.Info("Начало установки");
        _title.Text = "Установка";
        _subtitle.Text = "Подождите, пока VK Video Desktop устанавливается...";
        _progress.Visible = true; _progress.Value = 0;
        _status.Visible = true;
        _next.Visible = _back.Visible = _cancel.Visible = false;

        _cts = new CancellationTokenSource();
        try
        {
            await Task.Run(() => InstallerActions.Install.Run(
                _installPath,
                _desktopCheck?.Checked ?? true,
                _startMenuCheck?.Checked ?? true,
                _protocolCheck?.Checked ?? true,
                SetProgress, _cts.Token));
            ShowPage(4);
        }
        catch (OperationCanceledException)
        {
            _status.Text = "Установка отменена.";
            _next.Text = "Закрыть"; _next.Visible = true; _cancel.Visible = false;
        }
        catch (Exception ex)
        {
            SetupLogger.Fatal("Ошибка установки", ex);
            _status.Text = $"Ошибка: {ex.Message}";
            _next.Text = "Закрыть"; _next.Visible = true; _cancel.Visible = false;
        }
    }

    private void ShowComplete()
    {
        _title.Text = "Установка завершена!";
        _subtitle.Text = "VK Video Desktop успешно установлен.";
        _progress.Visible = _status.Visible = false;
        _next.Visible = _back.Visible = _cancel.Visible = false;

        for (int i = 0; i < _stepLabels.Length; i++)
        {
            _stepLabels[i].ForeColor = i == 4 ? Accent : Color.FromArgb(100, 200, 130);
            _stepLabels[i].Text = $"  ✓  {Steps[i]}";
        }

        _content.Controls.Add(new Label { Text = "✓", Font = new Font("Segoe UI", 48f), ForeColor = Accent, AutoSize = true, Location = new Point(190, 15), BackColor = Color.Transparent });
        _content.Controls.Add(new Label { Text = $"Папка: {_installPath}\n\nVK Video Desktop готов к использованию.\nВы можете запустить приложение из папки установки\nили через ярлык на рабочем столе.", Font = new Font("Segoe UI", 10f), ForeColor = Sub, AutoSize = false, Size = new Size(420, 100), Location = new Point(10, 110), TextAlign = ContentAlignment.TopCenter });

        var launchCheck = new CheckBox { Text = "Запустить VK Video Desktop", Checked = true, Font = new Font("Segoe UI", 10f), AutoSize = true, Location = new Point(130, 220) };
        _content.Controls.Add(launchCheck);

        var finish = new Button
        {
            Text = "Готово", Size = new Size(140, 40), Location = new Point(150, 260),
            FlatStyle = FlatStyle.Flat, BackColor = Accent, ForeColor = Color.White,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold), Cursor = Cursors.Hand
        };
        finish.FlatAppearance.BorderSize = 0;
        finish.Click += (_, _) =>
        {
            if (launchCheck.Checked)
            {
                var exe = Path.Combine(_installPath, "VKVideoDesktop.App.exe");
                if (File.Exists(exe)) Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
            }
            Close();
        };
        _content.Controls.Add(finish);
    }

    private void SetProgress(string status, int percent)
    {
        if (InvokeRequired) Invoke(() => SetProgress(status, percent));
        else { _status.Text = status; _progress.Value = percent; }
    }

    private void TrySetIcon()
    {
        try
        {
            var stream = System.Reflection.Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("VKVideoDesktop.Setup.icon.ico");
            if (stream != null) { Icon = new Icon(stream); stream.Dispose(); }
        }
        catch { }
    }

    private static Button MakeBtn(string text, int x, Color bg, Color fg, EventHandler onClick)
    {
        var btn = new Button
        {
            Text = text, Size = new Size(text.Length > 10 ? 110 : 90, 34),
            Location = new Point(x, 11), FlatStyle = FlatStyle.Flat,
            BackColor = bg, ForeColor = fg, Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = bg == Accent ? 0 : 1;
        btn.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
        btn.Click += onClick;
        return btn;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _cts?.Cancel();
        base.OnFormClosing(e);
    }
}
