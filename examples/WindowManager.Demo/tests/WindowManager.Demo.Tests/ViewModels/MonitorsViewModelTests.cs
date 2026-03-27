using FluentAssertions;
using NSubstitute;
using R3;
using WindowManagement;
using WindowManager.Demo.ViewModels;

namespace WindowManager.Demo.Tests.ViewModels;

public class MonitorsViewModelTests : IDisposable
{
    private readonly IMonitorService _monitorService = Substitute.For<IMonitorService>();
    private readonly MonitorsViewModel _sut;

    public MonitorsViewModelTests()
    {
        _monitorService.Connected.Returns(Observable.Empty<MonitorEventArgs>());
        _monitorService.Disconnected.Returns(Observable.Empty<MonitorEventArgs>());
        _monitorService.SettingsChanged.Returns(Observable.Empty<MonitorEventArgs>());
        _monitorService.All.Returns([]);

        _sut = new MonitorsViewModel(_monitorService);
    }

    public void Dispose() => _sut.Dispose();

    [Fact]
    public void RefreshMonitors__SingleMonitor_PopulatesCollection()
    {
        var monitor = CreateMonitor(0, 0, 1920, 1080, isPrimary: true);
        _monitorService.All.Returns(new[] { monitor });

        _sut.RefreshMonitorsCommand.Execute(null);

        _sut.Monitors.Should().HaveCount(1);
        _sut.SelectedMonitor.Should().Be(monitor);
    }

    [Fact]
    public void RefreshMonitors__MultipleMonitors_CalculatesCorrectScale()
    {
        var left = CreateMonitor(0, 0, 1920, 1080, isPrimary: true);
        var right = CreateMonitor(1920, 0, 2560, 1440);
        _monitorService.All.Returns(new[] { left, right });

        _sut.RefreshMonitorsCommand.Execute(null);

        _sut.Monitors.Should().HaveCount(2);
        // Total width = 1920 + 2560 = 4480, so scale = 500.0 / 4480
        double expectedScale = 500.0 / 4480;
        _sut.CanvasScale.Should().BeApproximately(expectedScale, 0.001);
        _sut.OffsetX.Should().Be(0);
        _sut.OffsetY.Should().Be(0);
    }

    [Fact]
    public void RefreshMonitors__NegativeCoordinates_CalculatesCorrectOffset()
    {
        var left = CreateMonitor(-1920, 0, 1920, 1080);
        var right = CreateMonitor(0, 0, 1920, 1080, isPrimary: true);
        _monitorService.All.Returns(new[] { left, right });

        _sut.RefreshMonitorsCommand.Execute(null);

        _sut.OffsetX.Should().Be(1920);
        _sut.OffsetY.Should().Be(0);
    }

    [Fact]
    public void RefreshMonitors__SelectsPrimaryMonitorByDefault()
    {
        var secondary = CreateMonitor(0, 0, 1920, 1080);
        var primary = CreateMonitor(1920, 0, 2560, 1440, isPrimary: true);
        _monitorService.All.Returns(new[] { secondary, primary });

        _sut.RefreshMonitorsCommand.Execute(null);

        _sut.SelectedMonitor.Should().Be(primary);
    }

    [Fact]
    public void RefreshMonitors__HeightConstrained_UsesHeightScale()
    {
        // Very tall monitor setup: total 1920 wide x 4320 tall
        // Width scale = 500/1920 ≈ 0.26, Height scale = 350/4320 ≈ 0.081
        // Should use the smaller (height) scale
        var top = CreateMonitor(0, 0, 1920, 2160, isPrimary: true);
        var bottom = CreateMonitor(0, 2160, 1920, 2160);
        _monitorService.All.Returns(new[] { top, bottom });

        _sut.RefreshMonitorsCommand.Execute(null);

        double expectedScale = 350.0 / 4320;
        _sut.CanvasScale.Should().BeApproximately(expectedScale, 0.001);
    }

    [Fact]
    public void RefreshMonitors__PreservesExistingSelection()
    {
        var monitor = CreateMonitor(0, 0, 1920, 1080, isPrimary: true, handle: 1);
        _monitorService.All.Returns(new[] { monitor });

        _sut.RefreshMonitorsCommand.Execute(null);
        _sut.SelectedMonitor.Should().Be(monitor);

        // Refresh again — should preserve selection via handle match
        _sut.RefreshMonitorsCommand.Execute(null);
        _sut.SelectedMonitor.Should().NotBeNull();
    }

    private static IMonitor CreateMonitor(int x, int y, int width, int height, bool isPrimary = false, nint handle = 0)
    {
        var monitor = Substitute.For<IMonitor>();
        monitor.Bounds.Returns(new WindowRect(x, y, width, height));
        monitor.WorkArea.Returns(new WindowRect(x, y, width, height - 48));
        monitor.IsPrimary.Returns(isPrimary);
        monitor.Handle.Returns(handle == 0 ? (nint)(x + width) : handle);
        monitor.DeviceName.Returns($"\\\\.\\DISPLAY{(isPrimary ? 1 : 2)}");
        monitor.Dpi.Returns(96);
        monitor.ScaleFactor.Returns(1.0);
        return monitor;
    }
}
