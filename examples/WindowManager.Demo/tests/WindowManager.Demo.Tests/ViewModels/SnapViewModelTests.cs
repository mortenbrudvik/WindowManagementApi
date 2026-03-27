using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using WindowManagement;
using WindowManager.Demo.Models;
using WindowManager.Demo.ViewModels;

namespace WindowManager.Demo.Tests.ViewModels;

public class SnapViewModelTests
{
    private readonly IWindowManager _windowManager;
    private readonly IMonitorService _monitorService;
    private readonly SnapViewModel _sut;

    public SnapViewModelTests()
    {
        _windowManager = Substitute.For<IWindowManager>();
        _monitorService = Substitute.For<IMonitorService>();

        _windowManager.Monitors.Returns(_monitorService);
        SetupGetAll();
        _monitorService.All.Returns(Array.Empty<IMonitor>());

        _sut = new SnapViewModel(_windowManager);
    }

    [Fact]
    public async Task SnapToAsync__NoWindowSelected_SetsErrorStatus()
    {
        var monitor = CreateMonitor();
        var request = new SnapRequest(monitor, SnapPosition.Left);

        await _sut.SnapToCommand.ExecuteAsync(request);

        _sut.StatusMessage.Should().Be("No window selected");
        _sut.IsStatusSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task SnapToAsync__Success_SetsSuccessStatus()
    {
        var monitor = CreateMonitor();
        var window = CreateWindow("Notepad");
        SetupGetAll(window);
        _monitorService.All.Returns(new[] { monitor });

        _sut.OnNavigatedTo();
        _sut.SelectedWindow = _sut.Windows[0];

        var request = new SnapRequest(monitor, SnapPosition.TopLeft);
        await _sut.SnapToCommand.ExecuteAsync(request);

        _sut.StatusMessage.Should().Contain("Snapped");
        _sut.StatusMessage.Should().Contain("Notepad");
        _sut.StatusMessage.Should().Contain("TopLeft");
        _sut.IsStatusSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SnapToAsync__Failure_SetsErrorStatusAndRefreshes()
    {
        var monitor = CreateMonitor();
        var window = CreateWindow("Notepad");
        SetupGetAll(window);

        _windowManager.SnapAsync(Arg.Any<IWindow>(), Arg.Any<IMonitor>(), Arg.Any<SnapPosition>())
            .ThrowsAsync(new WindowManagementException("Window not found"));

        _sut.OnNavigatedTo();
        _sut.SelectedWindow = _sut.Windows[0];

        var request = new SnapRequest(monitor, SnapPosition.Fill);
        await _sut.SnapToCommand.ExecuteAsync(request);

        _sut.StatusMessage.Should().Contain("Snap failed");
        _sut.StatusMessage.Should().Contain("Window not found");
        _sut.IsStatusSuccess.Should().BeFalse();
    }

    [Fact]
    public void OnNavigatedTo__PopulatesWindowsAndMonitors()
    {
        var window = CreateWindow("Notepad");
        var monitor = CreateMonitor();
        SetupGetAll(window);
        _monitorService.All.Returns(new[] { monitor });

        _sut.OnNavigatedTo();

        _sut.Windows.Should().HaveCount(1);
        _sut.Monitors.Should().HaveCount(1);
    }

    [Fact]
    public void OnNavigatedTo__PreSelectsForegroundWindow()
    {
        var window = CreateWindow("Notepad", handle: 99);
        SetupGetAll(window);
        _windowManager.GetForeground().Returns(window);
        _monitorService.All.Returns(Array.Empty<IMonitor>());

        _sut.OnNavigatedTo();

        _sut.SelectedWindow.Should().NotBeNull();
        _sut.SelectedWindow!.Handle.Should().Be(99);
    }

    private void SetupGetAll(params IWindow[] windows)
    {
        IReadOnlyList<IWindow> result = windows;
        _windowManager.GetAll(Arg.Any<WindowFilter?>()).Returns(result);
    }

    private static IMonitor CreateMonitor()
    {
        var monitor = Substitute.For<IMonitor>();
        monitor.Bounds.Returns(new WindowRect(0, 0, 1920, 1080));
        monitor.DeviceName.Returns("\\\\.\\DISPLAY1");
        monitor.Handle.Returns((nint)1);
        return monitor;
    }

    private static IWindow CreateWindow(string title, nint handle = 0)
    {
        var monitor = CreateMonitor();
        var window = Substitute.For<IWindow>();
        window.Title.Returns(title);
        window.ProcessName.Returns("test.exe");
        window.ProcessId.Returns(1234);
        window.ClassName.Returns("TestClass");
        window.Handle.Returns(handle == 0 ? (nint)title.GetHashCode() : handle);
        window.Bounds.Returns(new WindowRect(0, 0, 800, 600));
        window.State.Returns(WindowManagement.WindowState.Normal);
        window.Monitor.Returns(monitor);
        window.IsTopmost.Returns(false);
        return window;
    }
}
