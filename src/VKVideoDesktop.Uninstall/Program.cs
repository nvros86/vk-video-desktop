using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace VKVideoDesktop.Uninstall;

static class Program
{
    private static readonly string LogDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VKVideoDesktop", "log");

    private static readonly string LogPath = Path.Combine(LogDir, $"uninstall-{DateTime.Now:yyyy-MM-dd_HHmmss}.log");

    static int Main(string[] args)
    {
        var silent = args.Contains("--silent", StringComparer.OrdinalIgnoreCase);
        var cleanData = args.Contains("--clean-data", StringComparer.OrdinalIgnoreCase);

        // Find install path: registry > own directory
        var installPath = GetInstallPathFromRegistry();
        if (string.IsNullOrEmpty(installPath))
        {
            installPath = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        Directory.CreateDirectory(LogDir);

        Log("=== Удаление VK Video Desktop ===");
        Log($"Путь установки: {installPath}");
        Log($"Тихий режим: {silent}");
        Log($"Очистка данных: {cleanData}");

        if (string.IsNullOrEmpty(installPath) || !Directory.Exists(installPath))
        {
            Log("Путь установки не найден");
            return 1;
        }

        Log($"Путь: {installPath}");

        if (!silent)
        {
            Console.WriteLine("Вы уверены, что хотите удалить VK Video Desktop? (y/n)");
            var key = Console.ReadKey(true);
            if (key.KeyChar != 'y' && key.KeyChar != 'Y')
            {
                Log("Отменено пользователем");
                return 0;
            }
        }

        try
        {
            KillProcesses();
            RemoveCertificate();
            RemoveProtocol();
            RemoveShortcuts();
            RemoveUninstallEntry();

            if (Directory.Exists(installPath))
            {
                Log($"Удаление папки: {installPath}");
                try
                {
                    Directory.Delete(installPath, true);
                }
                catch (IOException)
                {
                    // Some files locked (e.g. self). Delete what we can.
                    foreach (var file in Directory.GetFiles(installPath, "*", SearchOption.AllDirectories))
                    {
                        try { File.Delete(file); } catch { }
                    }
                    try { Directory.Delete(installPath, false); } catch { }
                }
            }

            if (cleanData)
            {
                var appData = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "VKVideoDesktop");
                if (Directory.Exists(appData))
                {
                    Log($"Удаление данных: {appData}");
                    Directory.Delete(appData, true);
                }
            }

            Log("=== Удаление завершено ===");
            return 0;
        }
        catch (Exception ex)
        {
            Log($"ОШИБКА: {ex}");
            return 1;
        }
    }

    private static string GetInstallPathFromRegistry()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Uninstall\VKVideoDesktop");
            return key?.GetValue("InstallLocation")?.ToString() ?? "";
        }
        catch { return ""; }
    }

    private static void KillProcesses()
    {
        Log("Остановка процессов...");
        foreach (var proc in Process.GetProcessesByName("VKVideoDesktop.App"))
        {
            try
            {
                Log($"Остановка PID {proc.Id}");
                proc.Kill();
                proc.WaitForExit(3000);
            }
            catch (Exception ex) { Log($"Ошибка остановки PID {proc.Id}: {ex.Message}"); }
        }
    }

    private static void RemoveCertificate()
    {
        try
        {
            var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            var certs = store.Certificates.Find(X509FindType.FindBySubjectName, "VKVideoDesktop", false);
            foreach (var cert in certs)
            {
                Log($"Удаление сертификата: {cert.Subject}");
                store.Remove(cert);
            }
            store.Close();
        }
        catch (Exception ex) { Log($"Ошибка удаления сертификата: {ex.Message}"); }
    }

    private static void RemoveProtocol()
    {
        try
        {
            Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\vkvideo", false);
            Log("Протокол vkvideo:// удалён");
        }
        catch (Exception ex) { Log($"Ошибка удаления протокола: {ex.Message}"); }
    }

    private static void RemoveShortcuts()
    {
        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var desktopShortcut = Path.Combine(desktop, "VK Video Desktop.lnk");
            if (File.Exists(desktopShortcut))
            {
                File.Delete(desktopShortcut);
                Log("Ярлык на рабочем столе удалён");
            }

            var startMenu = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
            var startMenuDir = Path.Combine(startMenu, "Programs", "VK Video Desktop");
            if (Directory.Exists(startMenuDir))
            {
                Directory.Delete(startMenuDir, true);
                Log("Ярлыки в меню Пуск удалены");
            }
        }
        catch (Exception ex) { Log($"Ошибка удаления ярлыков: {ex.Message}"); }
    }

    private static void RemoveUninstallEntry()
    {
        try
        {
            Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(
                @"Software\Microsoft\Windows\CurrentVersion\Uninstall\VKVideoDesktop", false);
            Log("Запись удаления удалена из реестра");
        }
        catch (Exception ex) { Log($"Ошибка удаления записи из реестра: {ex.Message}"); }
    }

    private static void Log(string message)
    {
        try
        {
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            File.AppendAllText(LogPath, line + Environment.NewLine);
            Console.WriteLine(line);
        }
        catch { }
    }
}
