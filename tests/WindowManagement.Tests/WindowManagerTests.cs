using FluentAssertions;
using NSubstitute;
using WindowManagement.Exceptions;
using WindowManagement.Filtering;
using WindowManagement.Internal;
using WindowManagement.LowLevel;
using Xunit;

namespace WindowManagement.Tests;

public class WindowManagerTests
{
    private readonly IWindowApi _windowApi = Substitute.For<IWindowApi>();
    private readonly IDisplayApi _displayApi = Substitute.For<IDisplayApi>();
    private readonly WindowManager _manager;

    public WindowManagerTests()
    {
        var primaryDisplay = new DisplayInfo(1, @"\\.\DISPLAY1", "Monitor 1", true,
            new WindowRect(0, 0, 1920, 1080), new WindowRect(0, 0, 1920, 1040), 96, 1.0);

        _displayApi.GetAll().Returns([primaryDisplay]);
        _displayApi.GetPrimary().Returns(primaryDisplay);
        _displayApi.GetForWindow(Arg.Any<nint>()).Returns(primaryDisplay);
        _displayApi.IsPerMonitorV2Aware().Returns(true);

        _manager = new WindowManager(_windowApi, _displayApi);
    }

    [Fact]
    public void Constructor__NotDpiAware_ThrowsDpiAwarenessException()
    {
        _displayApi.IsPerMonitorV2Aware().Returns(false);

        var act = () => new WindowManager(_windowApi, _displayApi, enforceDpiAwareness: true);

        act.Should().Throw<DpiAwarenessException>();
    }

    [Fact]
    public void Constructor__DpiAwarenessFalse_DoesNotThrow()
    {
        _displayApi.IsPerMonitorV2Aware().Returns(false);

        var act = () => new WindowManager(_windowApi, _displayApi, enforceDpiAwareness: false);

        act.Should().NotThrow();
    }

    [Fact]
    public void GetAll__ReturnsWindowsFromApi()
    {
        _windowApi.Enumerate(true).Returns([1, 2]);
        SetupWindowHandle(1, "Notepad", "notepad", "Notepad", WindowState.Normal);
        SetupWindowHandle(2, "Calculator", "calc", "CalcFrame", WindowState.Normal);

        var windows = _manager.GetAll();

        windows.Should().HaveCount(2);
    }

    [Fact]
    public void GetAll__WithProcessFilter_FiltersCorrectly()
    {
        _windowApi.Enumerate(true).Returns([1, 2]);
        SetupWindowHandle(1, "Notepad", "notepad", "Notepad", WindowState.Normal);
        SetupWindowHandle(2, "Calculator", "calc", "CalcFrame", WindowState.Normal);

        var windows = _manager.GetAll(f => f.WithProcess("notepad"));

        windows.Should().HaveCount(1);
        windows[0].ProcessName.Should().Be("notepad");
    }

    [Fact]
    public void GetAll__WithTitleWildcard_FiltersCorrectly()
    {
        _windowApi.Enumerate(true).Returns([1, 2]);
        SetupWindowHandle(1, "readme.txt - Notepad", "notepad", "Notepad", WindowState.Normal);
        SetupWindowHandle(2, "Calculator", "calc", "CalcFrame", WindowState.Normal);

        var windows = _manager.GetAll(f => f.WithTitle("*.txt*"));

        windows.Should().HaveCount(1);
        windows[0].Title.Should().Contain(".txt");
    }

    [Fact]
    public void GetAll__ExcludeMinimized_FiltersCorrectly()
    {
        _windowApi.Enumerate(true).Returns([1, 2]);
        SetupWindowHandle(1, "Notepad", "notepad", "Notepad", WindowState.Normal);
        SetupWindowHandle(2, "Calculator", "calc", "CalcFrame", WindowState.Minimized);

        var windows = _manager.GetAll(f => f.ExcludeMinimized());

        windows.Should().HaveCount(1);
    }

    [Fact]
    public async Task MoveAsync__ValidWindow_CallsWindowApi()
    {
        _windowApi.IsValid(1).Returns(true);
        var window = CreateWindow(1);

        await _manager.MoveAsync(window, 100, 200);

        _windowApi.Received(1).Move(1, 100, 200);
    }

    [Fact]
    public async Task MoveAsync__InvalidWindow_ThrowsWindowNotFoundException()
    {
        _windowApi.IsValid(1).Returns(false);
        _windowApi.When(x => x.Move(1, Arg.Any<int>(), Arg.Any<int>()))
            .Do(_ => throw new WindowNotFoundException(1));
        var window = CreateWindow(1);

        var act = () => _manager.MoveAsync(window, 100, 200);

        await act.Should().ThrowAsync<WindowNotFoundException>();
    }

    private void SetupWindowHandle(nint hwnd, string title, string processName, string className, WindowState state)
    {
        _windowApi.GetTitle(hwnd).Returns(title);
        _windowApi.GetClassName(hwnd).Returns(className);
        _windowApi.GetProcessId(hwnd).Returns((int)hwnd * 1000);
        _windowApi.GetBounds(hwnd).Returns(new WindowRect(0, 0, 800, 600));
        _windowApi.GetState(hwnd).Returns(state);
        _windowApi.IsTopmost(hwnd).Returns(false);
        _windowApi.IsValid(hwnd).Returns(true);
    }

    private IWindow CreateWindow(nint hwnd) => new WindowInfo
    {
        Handle = hwnd,
        Title = _windowApi.GetTitle(hwnd),
        ProcessName = "notepad",
        ProcessId = _windowApi.GetProcessId(hwnd),
        ClassName = _windowApi.GetClassName(hwnd),
        Bounds = _windowApi.GetBounds(hwnd),
        State = _windowApi.GetState(hwnd),
        Monitor = new MonitorInfo
        {
            Handle = 1, DeviceName = @"\\.\DISPLAY1", DisplayName = "Monitor 1",
            IsPrimary = true, Bounds = new WindowRect(0, 0, 1920, 1080),
            WorkArea = new WindowRect(0, 0, 1920, 1040), Dpi = 96, ScaleFactor = 1.0
        },
        IsTopmost = false
    };
}
