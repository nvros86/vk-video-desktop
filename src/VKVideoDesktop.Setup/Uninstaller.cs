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
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;
        }

        try
        {
            InstallerActions.Uninstall.Run("", silent, false, SetupLogger.Info);
            if (!silent)
                MessageBox.Show("VK Video Desktop удалён.", "Готово",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            SetupLogger.Fatal("Ошибка удаления", ex);
            if (!silent)
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
