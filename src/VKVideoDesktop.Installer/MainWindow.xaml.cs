using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using Microsoft.Win32;

namespace VKVideoDesktop.Installer;

public partial class MainWindow : System.Windows.Window
{
    private int _currentPage;
    private readonly string[] _pages = { "WelcomePage", "LicensePage", "LocationPage", "InstallingPage", "CompletePage" };
    private string _installPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VKVideoDesktop");

    public MainWindow()
    {
        InitializeComponent();
        InstallPathBox.Text = _installPath;
        SpaceInfo.Text = "Свободно: " + GetFreeSpace();
    }

    private void OnNextClick(object sender, System.Windows.RoutedEventArgs e)
    {
        switch (_currentPage)
        {
            case 0:
                ShowPage(1);
                break;
            case 1:
                if (AcceptLicenseCheck.IsChecked != true)
                {
                    System.Windows.MessageBox.Show("Примите условия лицензионного соглашения.", "Внимание", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                _installPath = InstallPathBox.Text;
                ShowPage(2);
                break;
            case 2:
                if (string.IsNullOrWhiteSpace(InstallPathBox.Text))
                {
                    System.Windows.MessageBox.Show("Выберите папку установки.", "Внимание", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                _installPath = InstallPathBox.Text;
                ShowPage(3);
                NextButton.Visibility = System.Windows.Visibility.Collapsed;
                BackButton.Visibility = System.Windows.Visibility.Collapsed;
                StartInstallation();
                break;
            case 4:
                if (LaunchAfterInstall.IsChecked == true)
                {
                    var exePath = Path.Combine(_installPath, "VKVideoDesktop.App.exe");
                    if (File.Exists(exePath))
                        Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true });
                }
                System.Windows.Application.Current.Shutdown();
                break;
        }
    }

    private void OnBackClick(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_currentPage > 0 && _currentPage < 3)
            ShowPage(_currentPage - 1);
    }

    private void ShowPage(int index)
    {
        foreach (var name in _pages)
        {
            var el = FindName(name) as System.Windows.FrameworkElement;
            if (el != null) el.Visibility = System.Windows.Visibility.Collapsed;
        }
        var target = FindName(_pages[index]) as System.Windows.FrameworkElement;
        if (target != null) target.Visibility = System.Windows.Visibility.Visible;
        _currentPage = index;
        BackButton.Visibility = index > 0 && index < 3 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        NextButton.Content = index == 4 ? "Готово" : "Далее";
        NextButton.Visibility = index == 3 ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
    }

    private void StartInstallation()
    {
        var worker = new BackgroundWorker { WorkerReportsProgress = true };

        worker.DoWork += (s, e) =>
        {
            try
            {
                if (!Directory.Exists(_installPath))
                    Directory.CreateDirectory(_installPath);

                worker.ReportProgress(5, "Извлечение файлов...");

                var assembly = Assembly.GetExecutingAssembly();
                var resourceNames = assembly.GetManifestResourceNames();
                int total = 0;

                foreach (var res in resourceNames)
                {
                    if (res.EndsWith(".zip"))
                    {
                        using var stream = assembly.GetManifestResourceStream(res);
                        if (stream != null)
                        {
                            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
                            archive.ExtractToDirectory(_installPath, true);
                        }
                    }
                    total++;
                    worker.ReportProgress(5 + (int)((double)total / Math.Max(resourceNames.Length, 1) * 55),
                        "Извлечение: " + res);
                }

                worker.ReportProgress(65, "Создание ярлыков...");

                if (CreateDesktopShortcut.IsChecked == true)
                {
                    var deskPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    CreateShortcut(Path.Combine(deskPath, "VK Video Desktop.lnk"), _installPath);
                }

                var startDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                    "VK Video Desktop");
                if (!Directory.Exists(startDir))
                    Directory.CreateDirectory(startDir);
                CreateShortcut(Path.Combine(startDir, "VK Video Desktop.lnk"), _installPath);

                worker.ReportProgress(80, "Установка сертификата...");

                var cerPath = Path.Combine(_installPath, "VKVideoDesktop.cer");
                if (File.Exists(cerPath))
                {
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "certutil",
                            Arguments = "-addstore -f Root \"" + cerPath + "\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        var p = Process.Start(psi);
                        if (p != null) p.WaitForExit(10000);
                    }
                    catch { }
                }

                worker.ReportProgress(95, "Регистрация протокола...");

                var appExe = Path.Combine(_installPath, "VKVideoDesktop.App.exe");
                if (File.Exists(appExe))
                {
                    try
                    {
                        var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\vkvideo\shell\open\command");
                        if (key != null)
                        {
                            key.SetValue("", "\"" + appExe + "\" \"%1\"");
                            key.Close();
                        }
                        var iconKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\vkvideo\DefaultIcon");
                        if (iconKey != null)
                        {
                            iconKey.SetValue("", "\"" + appExe + "\",0");
                            iconKey.Close();
                        }
                    }
                    catch { }
                }

                worker.ReportProgress(100, "Готово!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Ошибка установки: " + ex.Message, "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        };

        worker.ProgressChanged += (s, e) =>
        {
            InstallProgress.Value = e.ProgressPercentage;
            InstallStatus.Text = e.UserState != null ? e.UserState.ToString() : "";
            if (e.ProgressPercentage >= 100)
            {
                InstallPathInfo.Text = "Путь: " + _installPath;
                ShowPage(4);
            }
        };

        worker.RunWorkerAsync();
    }

    private static void CreateShortcut(string shortcutPath, string installPath)
    {
        try
        {
            var dir = Path.GetDirectoryName(shortcutPath);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string exePath = Path.Combine(installPath, "VKVideoDesktop.App.exe");
            string script =
                "Set ws = WScript.CreateObject(\"WScript.Shell\")\n" +
                "Set link = ws.CreateShortcut(\"" + shortcutPath.Replace("\\", "\\\\") + "\")\n" +
                "link.TargetPath = \"" + exePath.Replace("\\", "\\\\") + "\"\n" +
                "link.WorkingDirectory = \"" + installPath.Replace("\\", "\\\\") + "\"\n" +
                "link.Description = \"VK Video Desktop\"\n" +
                "link.Save\n";

            var vbsPath = Path.Combine(Path.GetTempPath(), "create_shortcut.vbs");
            File.WriteAllText(vbsPath, script);
            var psi = new ProcessStartInfo
            {
                FileName = "wscript.exe",
                Arguments = "\"" + vbsPath + "\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var p = Process.Start(psi);
            if (p != null) p.WaitForExit(5000);
            File.Delete(vbsPath);
        }
        catch { }
    }

    private void OnBrowseClick(object sender, System.Windows.RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog();
        dialog.SelectedPath = _installPath;
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            InstallPathBox.Text = dialog.SelectedPath;
            SpaceInfo.Text = "Свободно: " + GetFreeSpace();
        }
    }

    private string GetFreeSpace()
    {
        try
        {
            var root = Path.GetPathRoot(InstallPathBox.Text);
            if (string.IsNullOrEmpty(root)) root = "C:\\";
            var drive = new DriveInfo(root);
            var gb = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);
            return gb.ToString("F1") + " GB";
        }
        catch { return "N/A"; }
    }
}
