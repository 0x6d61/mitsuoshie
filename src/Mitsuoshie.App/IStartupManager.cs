namespace Mitsuoshie.App;

/// <summary>
/// Windows スタートアップ登録の抽象化。
/// </summary>
public interface IStartupManager
{
    void Register();
    void Unregister();
    bool IsRegistered();
}
