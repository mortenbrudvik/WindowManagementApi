using FluentAssertions;
using NSubstitute;
using R3;
using WindowManagement;
using WindowManager.Demo.ViewModels;

namespace WindowManager.Demo.Tests.ViewModels;

public class WindowsViewModelTests : IDisposable
{
    private readonly IWindowManager _windowManager;
    private readonly WindowsViewModel _sut;

    public WindowsViewModelTests()
    {
        _windowManager = Substitute.For<IWindowManager>();
        var monitorService = Substitute.For<IMonitorService>();

        var emptyCreated = Observable.Empty<WindowEventArgs>();
        var emptyDestroyed = Observable.Empty<WindowEventArgs>();

        _windowManager.Monitors.Returns(monitorService);
        _windowManager.Created.Returns(emptyCreated);
        _windowManager.Destroyed.Returns(emptyDestroyed);
        SetupGetAll();

        _sut = new WindowsViewModel(_windowManager);
    }

    public void Dispose() => _sut.Dispose();

    [Fact]
    public void RefreshWindows__PopulatesWindowsCollection()
    {
        SetupGetAll(CreateWindow("Notepad"), CreateWindow("Explorer"));

        _sut.RefreshWindowsCommand.Execute(null);

        _sut.Windows.Should().HaveCount(2);
    }

    [Fact]
    public void RefreshWindows__SearchText_FiltersWindowsByTitle()
    {
        SetupGetAll(CreateWindow("Notepad"), CreateWindow("Explorer"), CreateWindow("Note Editor"));

        _sut.RefreshWindowsCommand.Execute(null);
        _sut.SearchText = "Note";

        _sut.Windows.Should().HaveCount(2);
        _sut.Windows.Should().Contain(w => w.Title == "Notepad");
        _sut.Windows.Should().Contain(w => w.Title == "Note Editor");
    }

    [Fact]
    public void RefreshWindows__SearchText_FiltersWindowsByProcessName()
    {
        SetupGetAll(CreateWindow("Window 1", processName: "notepad"), CreateWindow("Window 2", processName: "explorer"));

        _sut.RefreshWindowsCommand.Execute(null);
        _sut.SearchText = "notepad";

        _sut.Windows.Should().HaveCount(1);
        _sut.Windows[0].Title.Should().Be("Window 1");
    }

    [Fact]
    public void RefreshWindows__SearchText_IsCaseInsensitive()
    {
        SetupGetAll(CreateWindow("NOTEPAD"));

        _sut.RefreshWindowsCommand.Execute(null);
        _sut.SearchText = "notepad";

        _sut.Windows.Should().HaveCount(1);
    }

    [Fact]
    public void RefreshWindows__EmptySearch_ShowsAllWindows()
    {
        SetupGetAll(CreateWindow("Notepad"), CreateWindow("Explorer"));

        _sut.SearchText = "Note";
        _sut.SearchText = "";

        _sut.Windows.Should().HaveCount(2);
    }

    [Fact]
    public void RefreshWindows__PreservesSelectionByHandle()
    {
        SetupGetAll(CreateWindow("Notepad", handle: 42));

        _sut.RefreshWindowsCommand.Execute(null);
        _sut.SelectedWindow = _sut.Windows[0];

        _sut.RefreshWindowsCommand.Execute(null);

        _sut.SelectedWindow.Should().NotBeNull();
        _sut.SelectedWindow!.Handle.Should().Be(42);
    }

    private void SetupGetAll(params IWindow[] windows)
    {
        IReadOnlyList<IWindow> result = windows;
        _windowManager.GetAll(Arg.Any<WindowFilter?>()).Returns(result);
    }

    private static IWindow CreateWindow(string title, string processName = "test.exe", nint handle = 0)
    {
        var monitor = Substitute.For<IMonitor>();
        monitor.DeviceName.Returns("\\\\.\\DISPLAY1");
        monitor.Bounds.Returns(new WindowRect(0, 0, 1920, 1080));
        monitor.Dpi.Returns(96);
        monitor.ScaleFactor.Returns(1.0);

        var window = Substitute.For<IWindow>();
        window.Title.Returns(title);
        window.ProcessName.Returns(processName);
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
