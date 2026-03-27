using FluentAssertions;
using WindowManagement.DependencyInjection;
using Xunit;

namespace WindowManagement.Tests;

public class WindowManagementFactoryTests
{
    [Fact]
    public void Create__ReturnsWindowManager()
    {
        var options = new WindowManagementOptions { EnforceDpiAwareness = false };

        var manager = WindowManagementFactory.Create(options);

        manager.Should().NotBeNull();
        manager.Should().BeAssignableTo<IWindowManager>();
    }
}
