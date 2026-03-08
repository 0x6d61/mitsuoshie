namespace Mitsuoshie.App;

static class Program
{
    [STAThread]
    static void Main()
    {
        // 多重起動防止
        using var mutex = new Mutex(true, "Mitsuoshie_SingleInstance", out var isNew);
        if (!isNew)
        {
            MessageBox.Show("Mitsuoshie は既に実行中です。", "Mitsuoshie",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();

        var config = MitsuoshieAppConfig.CreateDefault();
        using var trayApp = new TrayApplicationContext(config);
        Application.Run(trayApp);
    }
}
