using Mitsuoshie.Core;

namespace Mitsuoshie.App;

public static class MitsuoshieAppConfig
{
    public static MitsuoshieServiceConfig CreateDefault()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var mitsuoshieDir = Path.Combine(localAppData, "Mitsuoshie");

        return new MitsuoshieServiceConfig
        {
            UserProfileDir = userProfile,
            SettingsPath = Path.Combine(mitsuoshieDir, "settings.json"),
            SysmonLogPath = Path.Combine(mitsuoshieDir, "logs", "mitsuoshie_sysmon.jsonl"),
            SafeProcessNames = [
                "MsMpEng.exe",
                "SearchIndexer.exe",
                "SearchProtocolHost.exe",
                "TiWorker.exe",
                "consent.exe"
            ],
            IntegrityCheckIntervalMinutes = 30,
            SuppressDuplicateMinutes = 5,
            EnableWindowsEventLog = true
        };
    }
}
