namespace DigitalKeyLab;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var demoScenario = args
            .FirstOrDefault(arg => arg.StartsWith("--demo=", StringComparison.OrdinalIgnoreCase))?
            .Split('=', 2)[1];

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm(demoScenario));
    }
}
