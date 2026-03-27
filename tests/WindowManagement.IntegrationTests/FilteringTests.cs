using System.Diagnostics;
using FluentAssertions;
using WindowManagement.DependencyInjection;
using WindowManagement.IntegrationTests.Helpers;
using Xunit;

namespace WindowManagement.IntegrationTests;

public class FilteringTests : IAsyncDisposable
{
    private readonly IWindowManager _manager;

    public FilteringTests()
    {
        _manager = WindowManagementFactory.Create(new WindowManagementOptions
        {
            EnforceDpiAwareness = false
        });
    }

    [Fact]
    public void GetAll__WithProcessFilter_FindsTestWindow()
    {
        using var window = TestWindow.Create();
        var processName = Process.GetCurrentProcess().ProcessName;

        var filtered = _manager.GetAll(f => f.Unfiltered().WithProcess(processName));

        filtered.Should().Contain(w => w.Handle == window.Handle);
    }

    [Fact]
    public void GetAll__WithTitleFilter_FindsTestWindow()
    {
        var uniqueTitle = $"IntegrationTest_{Guid.NewGuid():N}";
        using var window = TestWindow.Create(o => o.WithTitle(uniqueTitle));

        var filtered = _manager.GetAll(f => f.Unfiltered().WithTitle($"*{uniqueTitle}*"));

        filtered.Should().ContainSingle(w => w.Handle == window.Handle);
    }

    public async ValueTask DisposeAsync()
    {
        await _manager.DisposeAsync();
    }
}
