using FluentAssertions;
using NSubstitute;
using WindowManagement.Exceptions;
using WindowManagement.Internal;
using WindowManagement.LowLevel;
using Xunit;

namespace WindowManagement.Tests;

public class WindowManagerSnapTests
{
    [Theory]
    [InlineData(SnapPosition.Fill, 0, 0, 1920, 1040)]
    [InlineData(SnapPosition.Left, 0, 0, 960, 1040)]
    [InlineData(SnapPosition.Right, 960, 0, 960, 1040)]
    [InlineData(SnapPosition.Top, 0, 0, 1920, 520)]
    [InlineData(SnapPosition.Bottom, 0, 520, 1920, 520)]
    [InlineData(SnapPosition.TopLeft, 0, 0, 960, 520)]
    [InlineData(SnapPosition.TopRight, 960, 0, 960, 520)]
    [InlineData(SnapPosition.BottomLeft, 0, 520, 960, 520)]
    [InlineData(SnapPosition.BottomRight, 960, 520, 960, 520)]
    public void CalculateSnapBounds__ReturnsCorrectBounds(SnapPosition position, int x, int y, int w, int h)
    {
        var workArea = new WindowRect(0, 0, 1920, 1040);

        var result = SnapCalculator.Calculate(workArea, position);

        result.Should().Be(new WindowRect(x, y, w, h));
    }

    [Theory]
    [InlineData(SnapPosition.Left, 1920, 0, 1280, 1400)]
    [InlineData(SnapPosition.Right, 3200, 0, 1280, 1400)]
    public void CalculateSnapBounds__SecondMonitor_CorrectOffset(SnapPosition position, int x, int y, int w, int h)
    {
        var workArea = new WindowRect(1920, 0, 2560, 1400);

        var result = SnapCalculator.Calculate(workArea, position);

        result.Should().Be(new WindowRect(x, y, w, h));
    }

    [Fact]
    public void CalculateSnapBounds__InvalidPosition_ThrowsArgumentOutOfRangeException()
    {
        var workArea = new WindowRect(0, 0, 1920, 1040);

        var act = () => SnapCalculator.Calculate(workArea, (SnapPosition)999);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task SnapAsync__MinimizedWindow_CallsRestoreMinimized()
    {
        var (manager, windowApi, _) = CreateSnapTestFixture();
        var window = CreateSnapWindow(windowApi);
        var monitor = CreateSnapMonitor();

        windowApi.GetState(window.Handle).Returns(WindowState.Minimized);
        windowApi.RestoreMinimized(window.Handle).Returns(true);
        windowApi.IsResizable(window.Handle).Returns(true);
        windowApi.GetInvisibleBorders(window.Handle).Returns((0, 0, 0, 0));
        windowApi.GetDpi(window.Handle).Returns(96u);

        await manager.SnapAsync(window, monitor, SnapPosition.Left);

        windowApi.Received(1).RestoreMinimized(window.Handle);
    }

    [Fact]
    public async Task SnapAsync__MaximizedWindow_CallsSetStateNormal()
    {
        var (manager, windowApi, _) = CreateSnapTestFixture();
        var window = CreateSnapWindow(windowApi);
        var monitor = CreateSnapMonitor();

        windowApi.GetState(window.Handle).Returns(WindowState.Maximized);
        windowApi.IsResizable(window.Handle).Returns(true);
        windowApi.GetInvisibleBorders(window.Handle).Returns((0, 0, 0, 0));
        windowApi.GetDpi(window.Handle).Returns(96u);

        await manager.SnapAsync(window, monitor, SnapPosition.Left);

        windowApi.Received(1).SetState(window.Handle, WindowState.Normal);
        windowApi.DidNotReceive().RestoreMinimized(window.Handle);
    }

    [Fact]
    public async Task SnapAsync__MinimizedRestoreFails_ThrowsWindowManagementException()
    {
        var (manager, windowApi, _) = CreateSnapTestFixture();
        var window = CreateSnapWindow(windowApi);
        var monitor = CreateSnapMonitor();

        windowApi.GetState(window.Handle).Returns(WindowState.Minimized);
        windowApi.RestoreMinimized(window.Handle).Returns(false);

        var act = () => manager.SnapAsync(window, monitor, SnapPosition.Left);

        await act.Should().ThrowAsync<WindowManagementException>();
    }

    [Fact]
    public async Task SnapAsync__CrossDpi_AdjustsWidthAndHeight()
    {
        var (manager, windowApi, _) = CreateSnapTestFixture();
        var window = CreateSnapWindow(windowApi);
        var monitor = CreateSnapMonitor(dpi: 144);

        windowApi.GetState(window.Handle).Returns(WindowState.Normal);
        windowApi.IsResizable(window.Handle).Returns(true);
        windowApi.GetInvisibleBorders(window.Handle).Returns((0, 0, 0, 0));
        windowApi.GetDpi(window.Handle).Returns(96u);

        await manager.SnapAsync(window, monitor, SnapPosition.Fill);

        // DPI ratio = 96/144 = 0.667, applied to width/height only
        windowApi.Received(1).SetBounds(window.Handle, Arg.Is<WindowRect>(r =>
            r.X == 0 && r.Y == 0 &&
            r.Width == (int)Math.Round(1920 * (96.0 / 144)) &&
            r.Height == (int)Math.Round(1040 * (96.0 / 144))));
    }

    [Fact]
    public async Task SnapAsync__SameDpi_NoAdjustment()
    {
        var (manager, windowApi, _) = CreateSnapTestFixture();
        var window = CreateSnapWindow(windowApi);
        var monitor = CreateSnapMonitor(dpi: 96);

        windowApi.GetState(window.Handle).Returns(WindowState.Normal);
        windowApi.IsResizable(window.Handle).Returns(true);
        windowApi.GetInvisibleBorders(window.Handle).Returns((0, 0, 0, 0));
        windowApi.GetDpi(window.Handle).Returns(96u);

        await manager.SnapAsync(window, monitor, SnapPosition.Fill);

        windowApi.Received(1).SetBounds(window.Handle, Arg.Is<WindowRect>(r =>
            r.X == 0 && r.Y == 0 && r.Width == 1920 && r.Height == 1040));
    }

    private static (WindowManager manager, IWindowApi windowApi, IDisplayApi displayApi) CreateSnapTestFixture()
    {
        var windowApi = Substitute.For<IWindowApi>();
        var displayApi = Substitute.For<IDisplayApi>();

        var primaryDisplay = new DisplayInfo(1, @"\\.\DISPLAY1", "Monitor 1", true,
            new WindowRect(0, 0, 1920, 1080), new WindowRect(0, 0, 1920, 1040), 96, 1.0);

        displayApi.GetAll().Returns([primaryDisplay]);
        displayApi.GetPrimary().Returns(primaryDisplay);
        displayApi.GetForWindow(Arg.Any<nint>()).Returns(primaryDisplay);
        displayApi.IsPerMonitorV2Aware().Returns(true);

        var manager = new WindowManager(windowApi, displayApi);
        return (manager, windowApi, displayApi);
    }

    private static IWindow CreateSnapWindow(IWindowApi windowApi, nint handle = 1)
    {
        windowApi.GetTitle(handle).Returns("Test");
        windowApi.GetProcessId(handle).Returns(1000);
        windowApi.GetBounds(handle).Returns(new WindowRect(0, 0, 800, 600));

        return new WindowInfo
        {
            Handle = handle,
            Title = "Test",
            ProcessName = "test",
            ProcessId = 1000,
            ClassName = "TestClass",
            Bounds = new WindowRect(0, 0, 800, 600),
            State = WindowState.Normal,
            Monitor = CreateSnapMonitor(),
            IsTopmost = false
        };
    }

    private static IMonitor CreateSnapMonitor(int dpi = 96) => new MonitorInfo
    {
        Handle = 1, DeviceName = @"\\.\DISPLAY1", DisplayName = "Monitor 1",
        IsPrimary = true, Bounds = new WindowRect(0, 0, 1920, 1080),
        WorkArea = new WindowRect(0, 0, 1920, 1040), Dpi = dpi, ScaleFactor = dpi / 96.0
    };
}
