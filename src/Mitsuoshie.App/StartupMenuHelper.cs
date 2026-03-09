namespace Mitsuoshie.App;

/// <summary>
/// スタートアップ登録/解除のメニュー項目を生成するヘルパー。
/// </summary>
public static class StartupMenuHelper
{
    public static ToolStripItem CreateStartupMenuItem(
        IStartupManager startupManager,
        Action<string>? onError = null)
    {
        var registered = startupManager.IsRegistered();
        var item = new ToolStripMenuItem
        {
            Text = registered ? "スタートアップに登録済み" : "スタートアップに登録",
            Checked = registered
        };

        item.Click += (_, _) =>
        {
            try
            {
                if (startupManager.IsRegistered())
                {
                    startupManager.Unregister();
                }
                else
                {
                    startupManager.Register();
                }

                var nowRegistered = startupManager.IsRegistered();
                item.Checked = nowRegistered;
                item.Text = nowRegistered ? "スタートアップに登録済み" : "スタートアップに登録";
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex.Message);
            }
        };

        return item;
    }
}
