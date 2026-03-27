using Autofac;
using FluentAssertions;
using WindowManagement.DependencyInjection;
using WindowManagement.LowLevel;
using Xunit;

namespace WindowManagement.Tests.DependencyInjection;

public class WindowManagementModuleTests
{
    [Fact]
    public void RegisterModule__RegistersAllInterfaces()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule(new WindowManagementModule(opts => opts.EnforceDpiAwareness = false));
        var container = builder.Build();

        container.Resolve<IWindowApi>().Should().NotBeNull();
        container.Resolve<IDisplayApi>().Should().NotBeNull();
        container.Resolve<IWindowManager>().Should().NotBeNull();
        container.Resolve<IMonitorService>().Should().NotBeNull();
    }
}
