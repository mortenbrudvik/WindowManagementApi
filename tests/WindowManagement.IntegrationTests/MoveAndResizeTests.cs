using FluentAssertions;
using WindowManagement.IntegrationTests.Helpers;
using Xunit;

namespace WindowManagement.IntegrationTests;

public class MoveAndResizeTests : IAsyncDisposable
{
    private const int Tolerance = 10;
    private readonly IWindowManager _manager;

    public MoveAndResizeTests()
    {
        _manager = WindowManagementFactory.Create();
    }

    [Fact]
    public async Task MoveAsync__WindowMovesToNewPosition()
    {
        using var window = TestWindow.Create();
        var iWindow = FindWindow(window.Handle);

        await _manager.MoveAsync(iWindow, 200, 200);
        Thread.Sleep(100);

        var moved = FindWindow(window.Handle);
        moved.Bounds.X.Should().BeCloseTo(200, (uint)Tolerance);
        moved.Bounds.Y.Should().BeCloseTo(200, (uint)Tolerance);
    }

    [Fact]
    public async Task ResizeAsync__WindowChangesSize()
    {
        using var window = TestWindow.Create();
        var iWindow = FindWindow(window.Handle);

        await _manager.ResizeAsync(iWindow, 600, 400);
        Thread.Sleep(100);

        var resized = FindWindow(window.Handle);
        resized.Bounds.Width.Should().BeCloseTo(600, (uint)Tolerance);
        resized.Bounds.Height.Should().BeCloseTo(400, (uint)Tolerance);
    }

    [Fact]
    public async Task SetBoundsAsync__MovesAndResizesInOneCall()
    {
        using var window = TestWindow.Create();
        var iWindow = FindWindow(window.Handle);
        var target = new WindowRect(300, 300, 500, 350);

        await _manager.SetBoundsAsync(iWindow, target);
        Thread.Sleep(100);

        var updated = FindWindow(window.Handle);
        updated.Bounds.X.Should().BeCloseTo(target.X, (uint)Tolerance);
        updated.Bounds.Y.Should().BeCloseTo(target.Y, (uint)Tolerance);
        updated.Bounds.Width.Should().BeCloseTo(target.Width, (uint)Tolerance);
        updated.Bounds.Height.Should().BeCloseTo(target.Height, (uint)Tolerance);
    }

    private IWindow FindWindow(nint handle) =>
        _manager.GetAll(f => f.Unfiltered()).First(w => w.Handle == handle);

    public async ValueTask DisposeAsync()
    {
        await _manager.DisposeAsync();
    }
}
