using FluentAssertions;
using WindowManagement.IntegrationTests.Helpers;
using Xunit;

namespace WindowManagement.IntegrationTests;

public class CrossMonitorResolutionTests : IAsyncDisposable
{
    private const int Tolerance = 20;
    private readonly IWindowManager _manager;

    public CrossMonitorResolutionTests()
    {
        _manager = WindowManagementFactory.Create();
    }

    [RequiresMixedResolutionFact]
    public async Task MoveToMonitorAsync__DifferentResolution_BoundsWithinWorkArea()
    {
        using var window = TestWindow.Create(o => o.WithSize(400, 300));
        var iWindow = FindWindow(window.Handle);

        // Find a monitor with a different resolution
        var currentMonitor = iWindow.Monitor;
        var targetMonitor = _manager.Monitors.All.First(m =>
            m.Bounds.Width != currentMonitor.Bounds.Width ||
            m.Bounds.Height != currentMonitor.Bounds.Height);

        await _manager.MoveToMonitorAsync(iWindow, targetMonitor);
        Thread.Sleep(200);

        var moved = FindWindow(window.Handle);
        var workArea = targetMonitor.WorkArea;

        // Window should fit within target monitor's work area
        moved.Bounds.X.Should().BeGreaterThanOrEqualTo(workArea.X - Tolerance);
        moved.Bounds.Y.Should().BeGreaterThanOrEqualTo(workArea.Y - Tolerance);
        moved.Bounds.Right.Should().BeLessThanOrEqualTo(workArea.Right + Tolerance);
        moved.Bounds.Bottom.Should().BeLessThanOrEqualTo(workArea.Bottom + Tolerance);
    }

    [RequiresMixedResolutionFact]
    public async Task SnapAsync__DifferentResolution_FillsCorrectProportion()
    {
        using var window = TestWindow.Create();

        // Test snap on each monitor — proportions should be correct relative to that monitor's work area
        foreach (var monitor in _manager.Monitors.All)
        {
            var iWindow = FindWindow(window.Handle);
            await _manager.MoveToMonitorAsync(iWindow, monitor);
            Thread.Sleep(200);

            await _manager.SnapAsync(FindWindow(window.Handle), monitor, SnapPosition.Left);
            Thread.Sleep(100);

            var snapped = FindWindow(window.Handle);
            var workArea = monitor.WorkArea;
            var expectedWidth = workArea.Width / 2;

            snapped.Bounds.Width.Should().BeCloseTo(expectedWidth, (uint)Tolerance,
                $"on monitor {monitor.DeviceName} ({workArea.Width}x{workArea.Height})");
            snapped.Bounds.Height.Should().BeCloseTo(workArea.Height, (uint)Tolerance,
                $"on monitor {monitor.DeviceName} ({workArea.Width}x{workArea.Height})");
        }
    }

    private IWindow FindWindow(nint handle) =>
        _manager.GetAll(f => f.Unfiltered()).First(w => w.Handle == handle);

    public async ValueTask DisposeAsync()
    {
        await _manager.DisposeAsync();
    }
}
