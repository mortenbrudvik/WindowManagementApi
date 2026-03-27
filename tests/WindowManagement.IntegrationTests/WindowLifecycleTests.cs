using FluentAssertions;
using WindowManagement.DependencyInjection;
using WindowManagement.IntegrationTests.Helpers;
using Xunit;

namespace WindowManagement.IntegrationTests;

public class WindowLifecycleTests : IAsyncDisposable
{
    private readonly IWindowManager _manager;

    public WindowLifecycleTests()
    {
        _manager = WindowManagementFactory.Create(new WindowManagementOptions
        {
            EnforceDpiAwareness = false
        });
    }

    [Fact]
    public void Create__WindowAppearsInGetAll()
    {
        using var window = TestWindow.Create();

        var windows = _manager.GetAll(f => f.Unfiltered());

        windows.Should().Contain(w => w.Handle == window.Handle);
    }

    [Fact]
    public void Close__WindowDisappearsFromGetAll()
    {
        var window = TestWindow.Create();
        var handle = window.Handle;

        window.Dispose();
        Thread.Sleep(200); // allow Win32 to process destruction

        var windows = _manager.GetAll(f => f.Unfiltered());
        windows.Should().NotContain(w => w.Handle == handle);
    }

    public async ValueTask DisposeAsync()
    {
        await _manager.DisposeAsync();
    }
}
