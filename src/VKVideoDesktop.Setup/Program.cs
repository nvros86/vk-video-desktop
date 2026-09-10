namespace VKVideoDesktop.Setup;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--uninstall")
        {
            Uninstaller.Run();
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new InstallerForm());
    }
}
