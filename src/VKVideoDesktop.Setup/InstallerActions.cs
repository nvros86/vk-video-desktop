using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace VKVideoDesktop.Setup;

public static class InstallerActions
{
    public static class Install
    {
        public static void Run(string installPath, bool desktopShortcut, bool startMenuShortcut, bool registerProtocol, Action<string, int> progress, CancellationToken ct)
        {
            SetupLogger.Info("=== Начало установки ===");
            SetupLogger.Info($"Путь: {installPath}");

            EnsureWindowsAppRuntime(progress, ct);
            ct.ThrowIfCancellationRequested();

            ExtractApp(installPath, progress);
            ct.ThrowIfCancellationRequested();

            ExtractUninstaller(installPath);

            if (desktopShortcut) CreateDesktopShortcut(installPath);
            if (startMenuShortcut)
            {
                CreateStartMenuShortcut(installPath);
                CreateUninstallShortcut(installPath);
            }
            if (registerProtocol) RegisterProtocol(installPath);

            RegisterUninstallEntry(installPath);

            SetupLogger.Info("=== Установка завершена ===");
        }

        private static void ExtractApp(string installPath, Action<string, int> progress)
        {
            progress("Извлечение файлов...", 10);
            Directory.CreateDirectory(installPath);

            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var zipName = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("app.zip", StringComparison.OrdinalIgnoreCase))
                ?? assembly.GetManifestResourceNames().First(n => n.Contains("VKVideoDesktop-win-x64"));

            using var stream = assembly.GetManifestResourceStream(zipName)!;
            var zipPath = Path.Combine(installPath, "_app.zip");
            using (var fs = File.Create(zipPath))
                stream.CopyTo(fs);

            progress("Распаковка архива...", 30);
            ZipFile.ExtractToDirectory(zipPath, installPath, overwriteFiles: true);
            File.Delete(zipPath);
        }

        private static void ExtractUninstaller(string installPath)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var name = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("Uninstall.exe", StringComparison.OrdinalIgnoreCase));

            if (name == null) return;

            using var stream = assembly.GetManifestResourceStream(name);
            if (stream == null) return;

            var dest = Path.Combine(installPath, "Uninstall.exe");
            using var fs = File.Create(dest);
            stream.CopyTo(fs);
        }

        private static void EnsureWindowsAppRuntime(Action<string, int> progress, CancellationToken ct)
        {
            progress("Проверка Windows App Runtime...", 3);
            if (IsWindowsAppRuntimeInstalled()) return;

            progress("Установка Windows App Runtime...", 5);
            TryInstallViaWinget(ct);
            if (IsWindowsAppRuntimeInstalled()) return;

            TryInstallViaDownload(progress, ct);
        }

        private static void TryInstallViaWinget(CancellationToken ct)
        {
            try
            {
                var psi = new ProcessStartInfo("winget",
                    "install --id Microsoft.WindowsAppRuntime.1.7 --accept-source-agreements --accept-package-agreements")
                {
                    UseShellExecute = false, RedirectStandardOutput = true,
                    RedirectStandardError = true, CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc == null) return;
                proc.WaitForExit(300_000);
                SetupLogger.Info($"winget exit: {proc.ExitCode}");
            }
            catch (Exception ex)
            {
                SetupLogger.Info($"winget недоступен: {ex.Message}");
            }
        }

        private static void TryInstallViaDownload(Action<string, int> progress, CancellationToken ct)
        {
            var urls = new[]
            {
                "https://aka.ms/windowsappsdk/1.7/1.7.260224002/windowsappruntimeinstall-x64.exe",
                "https://aka.ms/windowsappsdk/1.7/1.7.250310001/windowsappruntimeinstall-x64.exe"
            };

            var tempFile = Path.Combine(Path.GetTempPath(), $"VKSetup_WinRT_{Guid.NewGuid():N}.exe");
            try
            {
                foreach (var url in urls)
                {
                    try
                    {
                        progress($"Скачивание Windows App Runtime...", 5);
                        using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromMinutes(5) };
                        client.DefaultRequestHeaders.UserAgent.ParseAdd("VKVideoDesktop/1.0");
                        var data = client.GetAsync(url, ct).GetAwaiter().GetResult()
                            .Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                        if (data.Length < 1_000_000) continue;
                        File.WriteAllBytes(tempFile, data);
                        break;
                    }
                    catch { }
                }

                if (!File.Exists(tempFile))
                {
                    Process.Start(new ProcessStartInfo(
                        "https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads-archive")
                    { UseShellExecute = true });
                    return;
                }

                progress("Установка Windows App Runtime...", 8);
                var psi = new ProcessStartInfo(tempFile, "--quiet --accept-license --force")
                { UseShellExecute = true, Verb = "runas" };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(300_000);
            }
            finally
            {
                try { File.Delete(tempFile); } catch { }
            }
        }

        public static bool IsWindowsAppRuntimeInstalled()
        {
            try
            {
                var systemDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
                if (File.Exists(Path.Combine(systemDir, "Microsoft.WindowsAppRuntime.dll"))) return true;

                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (Directory.Exists(Path.Combine(localAppData, "Microsoft", "WindowsAppRuntime"))) return true;

                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows App Runtime");
                if (key?.GetValue("Version") != null) return true;

                var windowsApps = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps");
                if (Directory.Exists(windowsApps))
                {
                    try
                    {
                        if (Directory.GetDirectories(windowsApps, "Microsoft.WindowsAppRuntime*").Length > 0) return true;
                    }
                    catch { }
                }
            }
            catch { }
            return false;
        }
    }

    public static class Uninstall
    {
        public static void Run(string installPath, bool silent, bool cleanData, Action<string>? log = null)
        {
            var L = log ?? ((_) => { });
            L("=== Удаление VK Video Desktop ===");

            if (string.IsNullOrEmpty(installPath))
                installPath = GetInstallPathFromRegistry();

            if (string.IsNullOrEmpty(installPath))
            {
                L("Путь установки не найден");
                return;
            }

            L($"Путь: {installPath}");
            KillProcesses(L);
            RemoveCertificate(L);
            RemoveProtocol(L);
            RemoveShortcuts(L);
            RemoveUninstallEntry(L);

            if (Directory.Exists(installPath))
            {
                L($"Удаление папки: {installPath}");
                Directory.Delete(installPath, true);
            }

            if (cleanData)
            {
                var appData = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "VKVideoDesktop");
                if (Directory.Exists(appData))
                {
                    L($"Удаление данных: {appData}");
                    Directory.Delete(appData, true);
                }
            }

            L("=== Удаление завершено ===");
        }

        public static string GetInstallPathFromRegistry()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Uninstall\VKVideoDesktop");
                return key?.GetValue("InstallLocation")?.ToString() ?? "";
            }
            catch { return ""; }
        }

        private static void KillProcesses(Action<string> log)
        {
            foreach (var proc in Process.GetProcessesByName("VKVideoDesktop.App"))
            {
                try
                {
                    log($"Остановка PID {proc.Id}");
                    proc.Kill();
                    proc.WaitForExit(3000);
                }
                catch (Exception ex) { log($"Ошибка остановки PID {proc.Id}: {ex.Message}"); }
            }
        }

        private static void RemoveCertificate(Action<string> log)
        {
            try
            {
                var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);
                foreach (var cert in store.Certificates.Find(X509FindType.FindBySubjectName, "VKVideoDesktop", false))
                {
                    log($"Удаление сертификата: {cert.Subject}");
                    store.Remove(cert);
                }
                store.Close();
            }
            catch (Exception ex) { log($"Ошибка удаления сертификата: {ex.Message}"); }
        }

        private static void RemoveProtocol(Action<string> log)
        {
            try
            {
                Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\vkvideo", false);
                log("Протокол vkvideo:// удалён");
            }
            catch (Exception ex) { log($"Ошибка удаления протокола: {ex.Message}"); }
        }

        private static void RemoveShortcuts(Action<string> log)
        {
            try
            {
                var desktop = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    "VK Video Desktop.lnk");
                if (File.Exists(desktop)) { File.Delete(desktop); log("Ярлык на рабочем столе удалён"); }

                var startMenu = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                    "Programs", "VK Video Desktop");
                if (Directory.Exists(startMenu)) { Directory.Delete(startMenu, true); log("Ярлыки в меню Пуск удалены"); }
            }
            catch (Exception ex) { log($"Ошибка удаления ярлыков: {ex.Message}"); }
        }

        private static void RemoveUninstallEntry(Action<string> log)
        {
            try
            {
                Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(
                    @"Software\Microsoft\Windows\CurrentVersion\Uninstall\VKVideoDesktop", false);
                log("Запись удаления удалена из реестра");
            }
            catch (Exception ex) { log($"Ошибка удаления записи из реестра: {ex.Message}"); }
        }
    }

    private static void CreateDesktopShortcut(string installPath)
    {
        var exePath = Path.Combine(installPath, "VKVideoDesktop.App.exe");
        if (!File.Exists(exePath)) return;
        var shortcutPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            "VK Video Desktop.lnk");
        CreateShortcut(shortcutPath, exePath, "VK Video Desktop");
    }

    private static void CreateStartMenuShortcut(string installPath)
    {
        var exePath = Path.Combine(installPath, "VKVideoDesktop.App.exe");
        if (!File.Exists(exePath)) return;
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            "Programs", "VK Video Desktop");
        Directory.CreateDirectory(dir);
        CreateShortcut(Path.Combine(dir, "VK Video Desktop.lnk"), exePath, "VK Video Desktop");
    }

    private static void CreateUninstallShortcut(string installPath)
    {
        var uninstallExe = Path.Combine(installPath, "Uninstall.exe");
        if (!File.Exists(uninstallExe)) return;
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            "Programs", "VK Video Desktop");
        Directory.CreateDirectory(dir);
        CreateShortcutWithArgs(Path.Combine(dir, "Удалить VK Video Desktop.lnk"),
            uninstallExe, "", "Удалить VK Video Desktop");
    }

    private static void CreateShortcut(string path, string target, string desc)
    {
        var workDir = Path.GetDirectoryName(target) ?? target;
        var script = $@"$w = New-Object -ComObject WScript.Shell
$l = $w.CreateShortcut('{path.Replace("'", "''")}')
$l.TargetPath = '{target.Replace("'", "''")}'
$l.WorkingDirectory = '{workDir.Replace("'", "''")}'
$l.Description = '{desc.Replace("'", "''")}'
$l.IconLocation = '{target.Replace("'", "''")},0'
$l.Save()";
        RunPowerShell(script);
    }

    private static void CreateShortcutWithArgs(string path, string target, string args, string desc)
    {
        var workDir = Path.GetDirectoryName(target) ?? target;
        var script = $@"$w = New-Object -ComObject WScript.Shell
$l = $w.CreateShortcut('{path.Replace("'", "''")}')
$l.TargetPath = '{target.Replace("'", "''")}'
$l.Arguments = '{args.Replace("'", "''")}'
$l.WorkingDirectory = '{workDir.Replace("'", "''")}'
$l.Description = '{desc.Replace("'", "''")}'
$l.IconLocation = '{target.Replace("'", "''")},0'
$l.Save()";
        RunPowerShell(script);
    }

    private static void RunPowerShell(string script)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"VKSetup_{Guid.NewGuid():N}.ps1");
        try
        {
            File.WriteAllText(tempFile, script, Encoding.UTF8);
            var psi = new ProcessStartInfo("powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -File \"{tempFile}\"")
            {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(15000);
        }
        catch (Exception ex) { SetupLogger.Error("Ошибка PowerShell", ex); }
        finally { try { File.Delete(tempFile); } catch { } }
    }

    private static void RegisterProtocol(string installPath)
    {
        var exePath = Path.Combine(installPath, "VKVideoDesktop.App.exe");
        if (!File.Exists(exePath)) return;

        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Classes\vkvideo");
            key?.SetValue("", "URL: VK Video Desktop");
            key?.SetValue("URL Protocol", "");
            using var icon = key?.CreateSubKey("DefaultIcon");
            icon?.SetValue("", $"\"{exePath}\",0");
            using var cmd = key?.CreateSubKey(@"shell\open\command");
            cmd?.SetValue("", $"\"{exePath}\" \"%1\"");
        }
        catch (Exception ex) { SetupLogger.Error("Ошибка регистрации протокола", ex); }
    }

    private static void RegisterUninstallEntry(string installPath)
    {
        var uninstallExe = Path.Combine(installPath, "Uninstall.exe");
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Uninstall\VKVideoDesktop");
            key?.SetValue("DisplayName", "VK Video Desktop");
            key?.SetValue("DisplayVersion", "1.0.0");
            key?.SetValue("Publisher", "VK Video Desktop Contributors");
            key?.SetValue("InstallLocation", installPath);
            key?.SetValue("UninstallString", $"\"{uninstallExe}\"");
            key?.SetValue("QuietUninstallString", $"\"{uninstallExe}\" --silent");
            key?.SetValue("NoModify", 1);
            key?.SetValue("NoRepair", 1);
            key?.SetValue("EstimatedSize", 119000);
        }
        catch (Exception ex) { SetupLogger.Error("Ошибка создания записи в реестре", ex); }
    }
}
