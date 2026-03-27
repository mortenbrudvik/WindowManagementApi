using FluentAssertions;
using WindowManagement.IntegrationTests.Helpers;
using Xunit;

namespace WindowManagement.IntegrationTests;

public class CrossMonitorDpiTests : IAsyncDisposable
{
    private const int Tolerance = 20;
    private readonly IWindowManager _manager;

    public CrossMonitorDpiTests()
    {
        _manager = WindowManagementFactory.Create();
    }

    [RequiresMixedDpiFact]
    public async Task MoveToMonitorAsync__DifferentDpi_BoundsCorrectRelativeToTarget()
    {
        using var window = TestWindow.Create();
        var iWindow = FindWindow(window.Handle);

        // Find a monitor with a different DPI than the current one
        var currentMonitor = iWindow.Monitor;
        var targetMonitor = _manager.Monitors.All.First(m => m.Dpi != currentMonitor.Dpi);

        await _manager.MoveToMonitorAsync(iWindow, targetMonitor);
        Thread.Sleep(200);

        var moved = FindWindow(window.Handle);
        var workArea = targetMonitor.WorkArea;

        // Window should be positioned within the target monitor's work area
        moved.Bounds.X.Should().BeGreaterThanOrEqualTo(workArea.X - Tolerance);
        moved.Bounds.Y.Should().BeGreaterThanOrEqualTo(workArea.Y - Tolerance);
        moved.Bounds.Right.Should().BeLessThanOrEqualTo(workArea.Right + Tolerance);
        moved.Bounds.Bottom.Should().BeLessThanOrEqualTo(workArea.Bottom + Tolerance);
    }

    [RequiresMixedDpiFact]
    public async Task SnapAsync__HighDpiMonitor_FillsExpectedProportion()
    {
        using var window = TestWindow.Create();
        var iWindow = FindWindow(window.Handle);

        // Find the monitor with the highest DPI
        var highDpiMonitor = _manager.Monitors.All.OrderByDescending(m => m.Dpi).First();

        // Move to target monitor first
        await _manager.MoveToMonitorAsync(iWindow, highDpiMonitor);
        Thread.Sleep(200);

        // Snap Left
        await _manager.SnapAsync(FindWindow(window.Handle), highDpiMonitor, SnapPosition.Left);
        Thread.Sleep(100);

        var snapped = FindWindow(window.Handle);
        var workArea = highDpiMonitor.WorkArea;
        var expectedWidth = workArea.Width / 2;

        // Width should be approximately half the work area despite DPI differences
        snapped.Bounds.Width.Should().BeCloseTo(expectedWidth, (uint)Tolerance);
        snapped.Bounds.Height.Should().BeCloseTo(workArea.Height, (uint)Tolerance);
    }

    private IWindow FindWindow(nint handle) =>
        _manager.GetAll(f => f.Unfiltered()).First(w => w.Handle == handle);

    public async ValueTask DisposeAsync()
    {
        await _manager.DisposeAsync();
    }
}
