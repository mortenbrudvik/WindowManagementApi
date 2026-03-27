using FluentAssertions;
using WindowManagement.DependencyInjection;
using WindowManagement.IntegrationTests.Helpers;
using Xunit;

namespace WindowManagement.IntegrationTests;

public class WindowStateTests : IAsyncDisposable
{
    private readonly IWindowManager _manager;

    public WindowStateTests()
    {
        _manager = WindowManagementFactory.Create(new WindowManagementOptions
        {
            EnforceDpiAwareness = false
        });
    }

    [Fact]
    public async Task SetStateAsync__Minimize_WindowIsMinimized()
    {
        using var window = TestWindow.Create();
        var iWindow = FindWindow(window.Handle);

        await _manager.SetStateAsync(iWindow, WindowState.Minimized);
        Thread.Sleep(200);

        var updated = FindWindow(window.Handle);
        updated.State.Should().Be(WindowState.Minimized);
    }

    [Fact]
    public async Task SetStateAsync__Maximize_WindowIsMaximized()
    {
        using var window = TestWindow.Create();
        var iWindow = FindWindow(window.Handle);

        await _manager.SetStateAsync(iWindow, WindowState.Maximized);
        Thread.Sleep(200);

        var updated = FindWindow(window.Handle);
        updated.State.Should().Be(WindowState.Maximized);
    }

    [Fact]
    public async Task SetStateAsync__Restore_WindowReturnsToNormal()
    {
        using var window = TestWindow.Create();
        var iWindow = FindWindow(window.Handle);

        await _manager.SetStateAsync(iWindow, WindowState.Maximized);
        Thread.Sleep(200);

        await _manager.SetStateAsync(FindWindow(window.Handle), WindowState.Normal);
        Thread.Sleep(200);

        var updated = FindWindow(window.Handle);
        updated.State.Should().Be(WindowState.Normal);
    }

    [Fact]
    public async Task FocusAsync__BringsWindowToForeground()
    {
        using var window1 = TestWindow.Create(o => o.WithTitle("FocusTest_First"));
        using var window2 = TestWindow.Create(o => o.WithTitle("FocusTest_Second"));
        Thread.Sleep(100);

        // window2 is in front (created last). Focus window1.
        var iWindow1 = FindWindow(window1.Handle);
        await _manager.FocusAsync(iWindow1);
        Thread.Sleep(200);

        var foreground = _manager.GetForeground();
        foreground.Should().NotBeNull();
        foreground!.Handle.Should().Be(window1.Handle);
    }

    private IWindow FindWindow(nint handle) =>
        _manager.GetAll(f => f.Unfiltered()).First(w => w.Handle == handle);

    public async ValueTask DisposeAsync()
    {
        await _manager.DisposeAsync();
    }
}
