namespace Mitsuoshie.Core.Tests.Monitoring;

using Mitsuoshie.Core.Monitoring;

public class SafeProcessFilterTests
{
    private readonly SafeProcessFilter _filter;

    public SafeProcessFilterTests()
    {
        _filter = new SafeProcessFilter(
            safeProcessNames: ["MsMpEng.exe", "SearchIndexer.exe", "SearchProtocolHost.exe", "TiWorker.exe", "consent.exe"],
            currentProcessId: 9999
        );
    }

    [Theory]
    [InlineData("MsMpEng.exe", "0x1", true)]       // Windows Defender — 常時除外
    [InlineData("MsMpEng.exe", "0x80", true)]
    [InlineData("SearchIndexer.exe", "0x1", true)]  // Search Indexer — 常時除外
    [InlineData("TiWorker.exe", "0x1", true)]       // Windows Update — 常時除外
    [InlineData("consent.exe", "0x1", true)]        // UAC — 常時除外
    public void IsSafe_AlwaysExcludedProcesses_ReturnsTrue(string processName, string accessMask, bool expected)
    {
        Assert.Equal(expected, _filter.IsSafe(processName, accessMask, processId: 1234));
    }

    [Theory]
    [InlineData("explorer.exe", "0x80", true)]     // ReadAttributes のみ — 除外（フォルダ表示）
    [InlineData("explorer.exe", "0x1", false)]     // ReadData — アラート対象
    [InlineData("explorer.exe", "0x2", false)]     // WriteData — アラート対象
    [InlineData("explorer.exe", "0x10000", false)] // DELETE — アラート対象
    public void IsSafe_Explorer_DependsOnAccessMask(string processName, string accessMask, bool expected)
    {
        Assert.Equal(expected, _filter.IsSafe(processName, accessMask, processId: 1234));
    }

    [Fact]
    public void IsSafe_CurrentProcess_ReturnsTrue()
    {
        // Mitsuoshie 自身のプロセスは除外
        Assert.True(_filter.IsSafe("anything.exe", "0x1", processId: 9999));
    }

    [Theory]
    [InlineData("malware.exe", "0x1", false)]
    [InlineData("stealer.exe", "0x2", false)]
    [InlineData("unknown.exe", "0x10000", false)]
    [InlineData("powershell.exe", "0x1", false)]
    [InlineData("cmd.exe", "0x1", false)]
    public void IsSafe_UnknownProcesses_ReturnsFalse(string processName, string accessMask, bool expected)
    {
        Assert.Equal(expected, _filter.IsSafe(processName, accessMask, processId: 1234));
    }

    [Fact]
    public void IsSafe_CaseInsensitive()
    {
        Assert.True(_filter.IsSafe("MSMPENG.EXE", "0x1", processId: 1234));
        Assert.True(_filter.IsSafe("msmpeng.exe", "0x1", processId: 1234));
        Assert.True(_filter.IsSafe("Explorer.exe", "0x80", processId: 1234));
    }

    [Fact]
    public void IsSafe_FullPath_ExtractsFileName()
    {
        Assert.True(_filter.IsSafe(@"C:\Windows\System32\MsMpEng.exe", "0x1", processId: 1234));
        Assert.True(_filter.IsSafe(@"C:\Windows\explorer.exe", "0x80", processId: 1234));
        Assert.False(_filter.IsSafe(@"C:\temp\malware.exe", "0x1", processId: 1234));
    }

    [Fact]
    public void IsSafe_ReadAttributesOnly_AlwaysExcluded()
    {
        // ReadAttributes (0x80) のみのアクセスはどのプロセスでも除外
        Assert.True(_filter.IsSafe("unknown.exe", "0x80", processId: 1234));
    }
}
