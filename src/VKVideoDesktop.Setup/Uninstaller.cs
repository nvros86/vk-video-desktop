using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace VKVideoDesktop.Setup;

public static class Uninstaller
{
    public static void Run()
    {
        SetupLogger.Info("=== Удаление VK Video Desktop ===");

        var args = Environment.GetCommandLineArgs();
        bool silent = args.Contains("--silent");

        if (!silent)
        {
            var result = MessageBox.Show(
                "Вы уверены, что хотите удалить VK Video Desktop?",
                "Удаление VK Video Desktop",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
            {
                SetupLogger.Info("Удаление отменено пользователем");
                return;
            }
        }

        try
        {
            var installPath = GetInstallPath();
            if (string.IsNullOrEmpty(installPath))
            {
                SetupLogger.Warn("Путь установки не найден в реестре");
                if (!silent) MessageBox.Show("Приложение не найдено.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SetupLogger.Info($"Путь установки: {installPath}");

            SetupLogger.Info("Остановка процессов...");
            foreach (var proc in Process.GetProcessesByName("VKVideoDesktop.App"))
            {
                try
                {
                    SetupLogger.Info($"Остановка процесса: {proc.Id}");
                    proc.Kill();
                    proc.WaitForExit(3000);
                }
                catch (Exception ex)
                {
                    SetupLogger.Error($"Ошибка остановки процесса {proc.Id}", ex);
                }
            }

            SetupLogger.Info("Удаление сертификата...");
            RemoveCertificate();

            SetupLogger.Info("Удаление протокола...");
            RemoveProtocol();

            SetupLogger.Info("Удаление ярлыков...");
            RemoveShortcuts();

            SetupLogger.Info("Удаление записи из реестра...");
            RemoveUninstallEntry();

            if (Directory.Exists(installPath))
            {
                SetupLogger.Info($"Удаление папки: {installPath}");
                Directory.Delete(installPath, true);
            }

            SetupLogger.Info("=== Удаление завершено ===");
            if (!silent)
                MessageBox.Show("VK Video Desktop удалён.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            SetupLogger.Fatal("Ошибка удаления", ex);
            if (!silent)
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string GetInstallPath()
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Uninstall\VKVideoDesktop");
        return key?.GetValue("InstallLocation")?.ToString() ?? "";
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
                SetupLogger.Info($"Удаление сертификата: {cert.Subject}");
                store.Remove(cert);
            }
            store.Close();
        }
        catch (Exception ex)
        {
            SetupLogger.Error("Ошибка удаления сертификата", ex);
        }
    }

    private static void RemoveProtocol()
    {
        try
        {
            Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\vkvideo", false);
            SetupLogger.Info("Протокол vkvideo:// удалён");
        }
        catch (Exception ex)
        {
            SetupLogger.Error("Ошибка удаления протокола", ex);
        }
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
                SetupLogger.Info("Ярлык на рабочем столе удалён");
            }

            var startMenu = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
            var startMenuDir = Path.Combine(startMenu, "Programs", "VK Video Desktop");
            if (Directory.Exists(startMenuDir))
            {
                Directory.Delete(startMenuDir, true);
                SetupLogger.Info("Ярлык в меню Пуск удалён");
            }
        }
        catch (Exception ex)
        {
            SetupLogger.Error("Ошибка удаления ярлыков", ex);
        }
    }

    private static void RemoveUninstallEntry()
    {
        try
        {
            Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(
                @"Software\Microsoft\Windows\CurrentVersion\Uninstall\VKVideoDesktop", false);
            SetupLogger.Info("Запись удаления удалена из реестра");
        }
        catch (Exception ex)
        {
            SetupLogger.Error("Ошибка удаления записи из реестра", ex);
        }
    }
}
