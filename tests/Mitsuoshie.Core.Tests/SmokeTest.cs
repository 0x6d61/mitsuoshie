namespace Mitsuoshie.Core.Tests;

public class SmokeTest
{
    [Fact]
    public void CoreAssembly_CanBeLoaded()
    {
        var assembly = typeof(Mitsuoshie.Core.MitsuoshieInfo).Assembly;
        Assert.NotNull(assembly);
        Assert.Equal("Mitsuoshie.Core", assembly.GetName().Name);
    }
}
