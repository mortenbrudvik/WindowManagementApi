using FluentAssertions;
using Xunit;

namespace WindowManagement.IntegrationTests;

public class WindowManagerIntegrationTests : IAsyncDisposable
{
    private readonly IWindowManager _manager;

    public WindowManagerIntegrationTests()
    {
        _manager = WindowManagementFactory.Create();
    }

    [Fact]
    public void GetAll__ReturnsVisibleWindows()
    {
        var windows = _manager.GetAll();

        windows.Should().NotBeEmpty();
    }

    [Fact]
    public void GetAll__AllWindowsHaveTitles()
    {
        var windows = _manager.GetAll();

        foreach (var w in windows)
            w.Title.Should().NotBeEmpty();
    }

    [Fact]
    public void GetAll__AllWindowsHaveMonitors()
    {
        var windows = _manager.GetAll();

        foreach (var w in windows)
            w.Monitor.Should().NotBeNull();
    }

    [Fact]
    public void GetForeground__ReturnsAWindow()
    {
        var window = _manager.GetForeground();

        window.Should().NotBeNull();
        window!.Title.Should().NotBeEmpty();
    }

    [Fact]
    public void Monitors_All__ReturnsMonitors()
    {
        var monitors = _manager.Monitors.All;

        monitors.Should().NotBeEmpty();
    }

    [Fact]
    public void Monitors_Primary__ReturnsPrimary()
    {
        var primary = _manager.Monitors.Primary;

        primary.IsPrimary.Should().BeTrue();
        primary.Dpi.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetAll__Unfiltered_ReturnsMoreWindows()
    {
        var altTab = _manager.GetAll();
        var all = _manager.GetAll(f => f.Unfiltered());

        all.Count.Should().BeGreaterThanOrEqualTo(altTab.Count);
    }

    [Fact]
    public void GetAll__WithProcessFilter_FiltersCorrectly()
    {
        var foreground = _manager.GetForeground();
        if (foreground == null) return;

        var filtered = _manager.GetAll(f => f.WithProcess(foreground.ProcessName));

        filtered.Should().Contain(w => w.ProcessName == foreground.ProcessName);
    }

    public async ValueTask DisposeAsync()
    {
        await _manager.DisposeAsync();
    }
}
