using FluentAssertions;
using WindowManagement.IntegrationTests.Helpers;
using Xunit;

namespace WindowManagement.IntegrationTests;

public class CrossMonitorTests : IAsyncDisposable
{
    private const int Tolerance = 20;
    private readonly IWindowManager _manager;

    public CrossMonitorTests()
    {
        _manager = WindowManagementFactory.Create();
    }

    [RequiresMultipleMonitorsFact]
    public async Task MoveToMonitorAsync__WindowMovesToTargetMonitor()
    {
        using var window = TestWindow.Create();
        var iWindow = FindWindow(window.Handle);
        var targetMonitor = _manager.Monitors.All.First(m => !m.IsPrimary);

        await _manager.MoveToMonitorAsync(iWindow, targetMonitor);
        Thread.Sleep(200);

        var moved = FindWindow(window.Handle);
        moved.Monitor.DeviceName.Should().Be(targetMonitor.DeviceName);
    }

    [RequiresMultipleMonitorsFact]
    public async Task MoveAsync__CrossMonitor_WindowLandsOnCorrectMonitor()
    {
        using var window = TestWindow.Create();
        var iWindow = FindWindow(window.Handle);
        var targetMonitor = _manager.Monitors.All.First(m => !m.IsPrimary);
        var targetCenter = targetMonitor.WorkArea;

        // Move to a point well within the target monitor's work area
        var targetX = targetCenter.X + targetCenter.Width / 4;
        var targetY = targetCenter.Y + targetCenter.Height / 4;

        await _manager.MoveAsync(iWindow, targetX, targetY);
        Thread.Sleep(200);

        var moved = FindWindow(window.Handle);
        moved.Monitor.DeviceName.Should().Be(targetMonitor.DeviceName);
    }

    [RequiresMultipleMonitorsFact]
    public async Task SnapAsync__OnSecondaryMonitor_SnapsToCorrectWorkArea()
    {
        using var window = TestWindow.Create();
        var iWindow = FindWindow(window.Handle);
        var targetMonitor = _manager.Monitors.All.First(m => !m.IsPrimary);

        // Move to secondary first
        await _manager.MoveToMonitorAsync(iWindow, targetMonitor);
        Thread.Sleep(200);

        // Snap Left on secondary
        await _manager.SnapAsync(FindWindow(window.Handle), targetMonitor, SnapPosition.Left);
        Thread.Sleep(100);

        var snapped = FindWindow(window.Handle);
        var workArea = targetMonitor.WorkArea;

        snapped.Bounds.X.Should().BeGreaterThanOrEqualTo(workArea.X - Tolerance);
        snapped.Bounds.Y.Should().BeGreaterThanOrEqualTo(workArea.Y - Tolerance);
        snapped.Bounds.Right.Should().BeLessThanOrEqualTo(workArea.X + workArea.Width / 2 + Tolerance);
        snapped.Bounds.Width.Should().BeCloseTo(workArea.Width / 2, (uint)Tolerance);
    }

    private IWindow FindWindow(nint handle) =>
        _manager.GetAll(f => f.Unfiltered()).First(w => w.Handle == handle);

    public async ValueTask DisposeAsync()
    {
        await _manager.DisposeAsync();
    }
}
