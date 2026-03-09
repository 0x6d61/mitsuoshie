namespace Mitsuoshie.Core.Tests.Detection;

using Mitsuoshie.Core.Detection;
using Mitsuoshie.Core.Models;

public class AlertGeneratorTests
{
    [Fact]
    public void GenerateAlertId_ReturnsExpectedFormat()
    {
        var generator = new AlertGenerator();
        var id = generator.GenerateAlertId();

        // MITSUOSHIE-2026-0001 形式
        Assert.Matches(@"^MITSUOSHIE-\d{4}-\d{4}$", id);
    }

    [Fact]
    public void GenerateAlertId_Increments()
    {
        var generator = new AlertGenerator();
        var id1 = generator.GenerateAlertId();
        var id2 = generator.GenerateAlertId();

        Assert.NotEqual(id1, id2);
        // 連番が増えているか
        var num1 = int.Parse(id1.Split('-')[2]);
        var num2 = int.Parse(id2.Split('-')[2]);
        Assert.Equal(num1 + 1, num2);
    }

    [Fact]
    public void ShouldSuppress_FirstAlert_ReturnsFalse()
    {
        var generator = new AlertGenerator(suppressDuration: TimeSpan.FromMinutes(5));
        var alert = CreateAlert("malware.exe", 1234, @"C:\test\file.txt");

        Assert.False(generator.ShouldSuppress(alert));
    }

    [Fact]
    public void ShouldSuppress_SameProcessSameFile_WithinWindow_ReturnsTrue()
    {
        var generator = new AlertGenerator(suppressDuration: TimeSpan.FromMinutes(5));
        var alert1 = CreateAlert("malware.exe", 1234, @"C:\test\file.txt");
        var alert2 = CreateAlert("malware.exe", 1234, @"C:\test\file.txt");

        generator.ShouldSuppress(alert1); // 記録
        Assert.True(generator.ShouldSuppress(alert2)); // 抑制される
    }

    [Fact]
    public void ShouldSuppress_DifferentProcess_ReturnsFalse()
    {
        var generator = new AlertGenerator(suppressDuration: TimeSpan.FromMinutes(5));
        var alert1 = CreateAlert("malware.exe", 1234, @"C:\test\file.txt");
        var alert2 = CreateAlert("stealer.exe", 5678, @"C:\test\file.txt");

        generator.ShouldSuppress(alert1);
        Assert.False(generator.ShouldSuppress(alert2));
    }

    [Fact]
    public void ShouldSuppress_SameProcessDifferentFile_ReturnsFalse()
    {
        var generator = new AlertGenerator(suppressDuration: TimeSpan.FromMinutes(5));
        var alert1 = CreateAlert("malware.exe", 1234, @"C:\test\file1.txt");
        var alert2 = CreateAlert("malware.exe", 1234, @"C:\test\file2.txt");

        generator.ShouldSuppress(alert1);
        Assert.False(generator.ShouldSuppress(alert2));
    }

    [Fact]
    public void ShouldSuppress_AfterWindowExpires_ReturnsFalse()
    {
        // 抑制期間を0秒に設定（即座に期限切れ）
        var generator = new AlertGenerator(suppressDuration: TimeSpan.Zero);
        var alert1 = CreateAlert("malware.exe", 1234, @"C:\test\file.txt");
        var alert2 = CreateAlert("malware.exe", 1234, @"C:\test\file.txt");

        generator.ShouldSuppress(alert1);
        Assert.False(generator.ShouldSuppress(alert2)); // 期限切れなので抑制されない
    }

    [Fact]
    public void ShouldSuppress_SameProcessSameFile_DifferentEventType_ReturnsFalse()
    {
        var generator = new AlertGenerator(suppressDuration: TimeSpan.FromMinutes(5));
        var readAlert = CreateAlert("malware.exe", 1234, @"C:\test\file.txt", "ReadData");
        var deleteAlert = CreateAlert("malware.exe", 1234, @"C:\test\file.txt", "Delete");

        generator.ShouldSuppress(readAlert);
        // 同一プロセス・同一ファイルでもアクセス種別が異なれば抑制されない
        Assert.False(generator.ShouldSuppress(deleteAlert));
    }

    private static MitsuoshieAlert CreateAlert(string processName, int processId, string honeyFile, string eventType = "ReadData")
    {
        return new MitsuoshieAlert
        {
            Timestamp = DateTime.UtcNow,
            HoneyFile = honeyFile,
            HoneyType = HoneyTokenType.AwsCredential,
            EventType = eventType,
            Tampered = false,
            OriginalHash = "hash",
            CurrentHash = "hash",
            AccessMask = "0x1",
            ProcessId = processId,
            ProcessName = processName,
            ProcessPath = $@"C:\{processName}",
            User = "user",
            Severity = AlertSeverity.Critical
        };
    }
}
