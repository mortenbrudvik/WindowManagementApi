using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WindowManagement.DependencyInjection;
using WindowManagement.LowLevel;
using Xunit;

namespace WindowManagement.Tests.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddWindowManagement__RegistersAllInterfaces()
    {
        var services = new ServiceCollection();

        services.AddWindowManagement(opts => opts.EnforceDpiAwareness = false);

        var provider = services.BuildServiceProvider();
        provider.GetService<IWindowApi>().Should().NotBeNull();
        provider.GetService<IDisplayApi>().Should().NotBeNull();
        provider.GetService<IWindowManager>().Should().NotBeNull();
        provider.GetService<IMonitorService>().Should().NotBeNull();
    }

    [Fact]
    public void AddWindowManagement__WindowManagerIsSingleton()
    {
        var services = new ServiceCollection();
        services.AddWindowManagement(opts => opts.EnforceDpiAwareness = false);
        var provider = services.BuildServiceProvider();

        var a = provider.GetService<IWindowManager>();
        var b = provider.GetService<IWindowManager>();

        a.Should().BeSameAs(b);
    }
}
