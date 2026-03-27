using FluentAssertions;
using WindowManagement.IntegrationTests.Helpers;
using Xunit;

namespace WindowManagement.IntegrationTests;

public class SnapPositionTests : IAsyncDisposable
{
    private const int Tolerance = 20;
    private readonly IWindowManager _manager;

    public SnapPositionTests()
    {
        _manager = WindowManagementFactory.Create();
    }

    [Theory]
    [InlineData(SnapPosition.Left, 0.5, 1.0)]
    [InlineData(SnapPosition.Right, 0.5, 1.0)]
    [InlineData(SnapPosition.Top, 1.0, 0.5)]
    [InlineData(SnapPosition.Bottom, 1.0, 0.5)]
    [InlineData(SnapPosition.TopLeft, 0.5, 0.5)]
    [InlineData(SnapPosition.TopRight, 0.5, 0.5)]
    [InlineData(SnapPosition.BottomLeft, 0.5, 0.5)]
    [InlineData(SnapPosition.BottomRight, 0.5, 0.5)]
    [InlineData(SnapPosition.Fill, 1.0, 1.0)]
    public async Task SnapAsync__AllPositions_WindowFillsExpectedArea(
        SnapPosition position, double expectedWidthRatio, double expectedHeightRatio)
    {
        using var window = TestWindow.Create();
        var iWindow = FindWindow(window.Handle);
        var monitor = _manager.Monitors.Primary;

        await _manager.SnapAsync(iWindow, monitor, position);
        Thread.Sleep(100);

        var snapped = FindWindow(window.Handle);
        var workArea = monitor.WorkArea;

        // Window should be within the monitor's work area (with tolerance for invisible borders)
        snapped.Bounds.X.Should().BeGreaterThanOrEqualTo(workArea.X - Tolerance);
        snapped.Bounds.Y.Should().BeGreaterThanOrEqualTo(workArea.Y - Tolerance);
        snapped.Bounds.Right.Should().BeLessThanOrEqualTo(workArea.Right + Tolerance);
        snapped.Bounds.Bottom.Should().BeLessThanOrEqualTo(workArea.Bottom + Tolerance);

        // Verify proportions
        var expectedWidth = (int)(workArea.Width * expectedWidthRatio);
        var expectedHeight = (int)(workArea.Height * expectedHeightRatio);

        snapped.Bounds.Width.Should().BeCloseTo(expectedWidth, (uint)Tolerance);
        snapped.Bounds.Height.Should().BeCloseTo(expectedHeight, (uint)Tolerance);
    }

    [Fact]
    public async Task SnapAsync__NonResizableWindow_CentersInSnapZone()
    {
        using var window = TestWindow.Create(o => o.NonResizable().WithSize(300, 200));
        var iWindow = FindWindow(window.Handle);
        var monitor = _manager.Monitors.Primary;

        await _manager.SnapAsync(iWindow, monitor, SnapPosition.Left);
        Thread.Sleep(100);

        var snapped = FindWindow(window.Handle);
        var workArea = monitor.WorkArea;
        var snapZoneWidth = workArea.Width / 2;

        // Size should be unchanged (non-resizable)
        snapped.Bounds.Width.Should().BeCloseTo(300, (uint)Tolerance);
        snapped.Bounds.Height.Should().BeCloseTo(200, (uint)Tolerance);

        // Should be centered in the left snap zone
        var expectedCenterX = workArea.X + snapZoneWidth / 2;
        var actualCenterX = snapped.Bounds.X + snapped.Bounds.Width / 2;
        actualCenterX.Should().BeCloseTo(expectedCenterX, (uint)Tolerance);
    }

    private IWindow FindWindow(nint handle) =>
        _manager.GetAll(f => f.Unfiltered()).First(w => w.Handle == handle);

    public async ValueTask DisposeAsync()
    {
        await _manager.DisposeAsync();
    }
}
