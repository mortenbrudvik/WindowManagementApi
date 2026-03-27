using FluentAssertions;
using NSubstitute;
using WindowManagement.Exceptions;
using WindowManagement.Internal;
using WindowManagement.LowLevel;
using Xunit;

namespace WindowManagement.Tests;

public class MonitorServiceTests
{
    private readonly IDisplayApi _displayApi = Substitute.For<IDisplayApi>();
    private readonly MonitorService _service;

    public MonitorServiceTests()
    {
        var primary = new DisplayInfo(1, @"\\.\DISPLAY1", "Monitor 1", true,
            new WindowRect(0, 0, 1920, 1080), new WindowRect(0, 0, 1920, 1040), 96, 1.0);
        var secondary = new DisplayInfo(2, @"\\.\DISPLAY2", "Monitor 2", false,
            new WindowRect(1920, 0, 2560, 1440), new WindowRect(1920, 0, 2560, 1400), 144, 1.5);

        _displayApi.GetAll().Returns([primary, secondary]);
        _displayApi.GetPrimary().Returns(primary);

        _service = new MonitorService(_displayApi);
    }

    [Fact]
    public void All__ReturnsBothMonitors()
    {
        _service.All.Should().HaveCount(2);
    }

    [Fact]
    public void Primary__ReturnsPrimaryMonitor()
    {
        _service.Primary.IsPrimary.Should().BeTrue();
        _service.Primary.DeviceName.Should().Be(@"\\.\DISPLAY1");
    }

    [Fact]
    public void GetAt__CenterOfPrimary_ReturnsPrimaryMonitor()
    {
        _displayApi.GetAtPoint(960, 540).Returns(
            new DisplayInfo(1, @"\\.\DISPLAY1", "Monitor 1", true,
                new WindowRect(0, 0, 1920, 1080), new WindowRect(0, 0, 1920, 1040), 96, 1.0));

        var result = _service.GetAt(960, 540);

        result.Should().NotBeNull();
        result!.DeviceName.Should().Be(@"\\.\DISPLAY1");
    }

    [Fact]
    public void GetAt__PointOutsideAllMonitors_ReturnsNull()
    {
        _displayApi.GetAtPoint(-9999, -9999).Returns((DisplayInfo?)null);

        var result = _service.GetAt(-9999, -9999);

        result.Should().BeNull();
    }

    [Fact]
    public void Primary__NoPrimaryMonitor_ThrowsWindowManagementException()
    {
        var displayApi = Substitute.For<IDisplayApi>();
        var nonPrimary = new DisplayInfo(1, @"\\.\DISPLAY1", "Monitor 1", false,
            new WindowRect(0, 0, 1920, 1080), new WindowRect(0, 0, 1920, 1040), 96, 1.0);
        displayApi.GetAll().Returns([nonPrimary]);

        var service = new MonitorService(displayApi);

        var act = () => service.Primary;

        act.Should().Throw<WindowManagementException>();
    }
}
