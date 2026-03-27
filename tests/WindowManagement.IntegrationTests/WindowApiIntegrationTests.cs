using FluentAssertions;
using WindowManagement.LowLevel;
using WindowManagement.LowLevel.Internal;
using Xunit;

namespace WindowManagement.IntegrationTests;

public class WindowApiIntegrationTests
{
    private readonly WindowApi _api = new();

    [Fact]
    public void Enumerate__AltTabOnly_ReturnsWindows()
    {
        var windows = _api.Enumerate(altTabOnly: true);

        windows.Should().NotBeEmpty();
    }

    [Fact]
    public void Enumerate__Unfiltered_ReturnsMoreOrEqualWindows()
    {
        var altTab = _api.Enumerate(altTabOnly: true);
        var all = _api.Enumerate(altTabOnly: false);

        all.Count.Should().BeGreaterThanOrEqualTo(altTab.Count);
    }

    [Fact]
    public void GetForeground__ReturnsNonZeroHandle()
    {
        var hwnd = _api.GetForeground();

        hwnd.Should().NotBe(0);
    }

    [Fact]
    public void GetTitle__ForegroundWindow_ReturnsNonEmptyString()
    {
        var hwnd = _api.GetForeground();
        var title = _api.GetTitle(hwnd);

        title.Should().NotBeEmpty();
    }

    [Fact]
    public void GetBounds__ForegroundWindow_ReturnsNonZeroDimensions()
    {
        var hwnd = _api.GetForeground();
        var bounds = _api.GetBounds(hwnd);

        bounds.Width.Should().NotBe(0);
        bounds.Height.Should().NotBe(0);
    }

    [Fact]
    public void IsValid__ForegroundWindow_ReturnsTrue()
    {
        var hwnd = _api.GetForeground();

        _api.IsValid(hwnd).Should().BeTrue();
    }

    [Fact]
    public void IsValid__InvalidHandle_ReturnsFalse()
    {
        _api.IsValid(0xDEAD).Should().BeFalse();
    }

    [Fact]
    public void GetProcessId__ForegroundWindow_ReturnsNonZero()
    {
        var hwnd = _api.GetForeground();
        var pid = _api.GetProcessId(hwnd);

        pid.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetState__ForegroundWindow_ReturnsNormalOrMaximized()
    {
        var hwnd = _api.GetForeground();
        var state = _api.GetState(hwnd);

        state.Should().BeOneOf(WindowState.Normal, WindowState.Maximized);
    }
}
