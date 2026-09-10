using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace VKVideoDesktop.Setup;

public static class Uninstaller
{
    public static void Run()
    {
        var args = Environment.GetCommandLineArgs();
        bool silent = args.Contains("--silent");

        if (!silent)
        {
            var result = MessageBox.Show(
                "Вы уверены, что хотите удалить VK Video Desktop?",
                "Удаление VK Video Desktop",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;
        }

        try
        {
            var installPath = GetInstallPath();
            if (string.IsNullOrEmpty(installPath))
            {
                if (!silent) MessageBox.Show("Приложение не найдено.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            foreach (var proc in Process.GetProcessesByName("VKVideoDesktop"))
            {
                try { proc.Kill(); proc.WaitForExit(3000); } catch { }
            }

            RemoveCertificate();
            RemoveProtocol();
            RemoveShortcuts();
            RemoveUninstallEntry();

            if (Directory.Exists(installPath))
                Directory.Delete(installPath, true);

            if (!silent)
                MessageBox.Show("VK Video Desktop удалён.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
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
                store.Remove(cert);
            store.Close();
        }
        catch { }
    }

    private static void RemoveProtocol()
    {
        try { Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\vkvideo", false); } catch { }
    }

    private static void RemoveShortcuts()
    {
        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var desktopShortcut = Path.Combine(desktop, "VK Video Desktop.lnk");
            if (File.Exists(desktopShortcut)) File.Delete(desktopShortcut);

            var startMenu = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
            var startMenuDir = Path.Combine(startMenu, "Programs", "VK Video Desktop");
            if (Directory.Exists(startMenuDir)) Directory.Delete(startMenuDir, true);
        }
        catch { }
    }

    private static void RemoveUninstallEntry()
    {
        try { Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(
            @"Software\Microsoft\Windows\CurrentVersion\Uninstall\VKVideoDesktop", false); } catch { }
    }
}
