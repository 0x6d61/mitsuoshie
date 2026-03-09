using Mitsuoshie.App;

namespace Mitsuoshie.App.Tests;

public class StartupMenuTests
{
    [Fact]
    public void CreateStartupMenuItem_WhenRegistered_ShowsChecked()
    {
        var fake = new FakeStartupManager(isRegistered: true);
        var item = StartupMenuHelper.CreateStartupMenuItem(fake);

        Assert.True(((ToolStripMenuItem)item).Checked);
        Assert.Equal("スタートアップに登録済み", item.Text);
    }

    [Fact]
    public void CreateStartupMenuItem_WhenNotRegistered_ShowsUnchecked()
    {
        var fake = new FakeStartupManager(isRegistered: false);
        var item = StartupMenuHelper.CreateStartupMenuItem(fake);

        Assert.False(((ToolStripMenuItem)item).Checked);
        Assert.Equal("スタートアップに登録", item.Text);
    }

    [Fact]
    public void ClickMenuItem_WhenNotRegistered_CallsRegister()
    {
        var fake = new FakeStartupManager(isRegistered: false);
        var item = (ToolStripMenuItem)StartupMenuHelper.CreateStartupMenuItem(fake);

        item.PerformClick();

        Assert.True(fake.RegisterCalled);
        Assert.False(fake.UnregisterCalled);
    }

    [Fact]
    public void ClickMenuItem_WhenRegistered_CallsUnregister()
    {
        var fake = new FakeStartupManager(isRegistered: true);
        var item = (ToolStripMenuItem)StartupMenuHelper.CreateStartupMenuItem(fake);

        item.PerformClick();

        Assert.True(fake.UnregisterCalled);
        Assert.False(fake.RegisterCalled);
    }

    [Fact]
    public void ClickMenuItem_TogglesCheckedState()
    {
        var fake = new FakeStartupManager(isRegistered: false);
        var item = (ToolStripMenuItem)StartupMenuHelper.CreateStartupMenuItem(fake);

        Assert.False(item.Checked);

        item.PerformClick();

        // After register, IsRegistered returns true → Checked should update
        Assert.True(item.Checked);
        Assert.Equal("スタートアップに登録済み", item.Text);
    }

    private class FakeStartupManager : IStartupManager
    {
        private bool _isRegistered;
        public bool RegisterCalled { get; private set; }
        public bool UnregisterCalled { get; private set; }

        public FakeStartupManager(bool isRegistered) => _isRegistered = isRegistered;

        public void Register()
        {
            RegisterCalled = true;
            _isRegistered = true;
        }

        public void Unregister()
        {
            UnregisterCalled = true;
            _isRegistered = false;
        }

        public bool IsRegistered() => _isRegistered;
    }
}
