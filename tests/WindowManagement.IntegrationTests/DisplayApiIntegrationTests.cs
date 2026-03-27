using FluentAssertions;
using WindowManagement.LowLevel;
using WindowManagement.LowLevel.Internal;
using Xunit;

namespace WindowManagement.IntegrationTests;

public class DisplayApiIntegrationTests
{
    private readonly DisplayApi _api = new();

    [Fact]
    public void GetAll__ReturnsAtLeastOneMonitor()
    {
        var monitors = _api.GetAll();

        monitors.Should().NotBeEmpty();
    }

    [Fact]
    public void GetAll__AllMonitorsHavePositiveDimensions()
    {
        var monitors = _api.GetAll();

        foreach (var m in monitors)
        {
            m.Bounds.Width.Should().BeGreaterThan(0);
            m.Bounds.Height.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public void GetPrimary__ReturnsPrimaryMonitor()
    {
        var primary = _api.GetPrimary();

        primary.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void GetAll__ExactlyOnePrimaryMonitor()
    {
        var monitors = _api.GetAll();

        monitors.Count(m => m.IsPrimary).Should().Be(1);
    }

    [Fact]
    public void GetDpi__ReturnsReasonableValue()
    {
        var primary = _api.GetPrimary();
        var dpi = _api.GetDpi(primary.Handle);

        dpi.Should().BeInRange(72, 600);
    }

    [Fact]
    public void GetScaleFactor__ReturnsReasonableValue()
    {
        var primary = _api.GetPrimary();
        var scale = _api.GetScaleFactor(primary.Handle);

        scale.Should().BeInRange(0.5, 5.0);
    }

    [Fact]
    public void GetAtPoint__CenterOfPrimary_ReturnsPrimary()
    {
        var primary = _api.GetPrimary();
        var centerX = primary.Bounds.X + primary.Bounds.Width / 2;
        var centerY = primary.Bounds.Y + primary.Bounds.Height / 2;

        var result = _api.GetAtPoint(centerX, centerY);

        result.Should().NotBeNull();
        result!.DeviceName.Should().Be(primary.DeviceName);
    }

    [Fact]
    public void IsPerMonitorV2Aware__ReturnsBoolean()
    {
        // Just verify it doesn't throw — actual value depends on test host config
        var result = _api.IsPerMonitorV2Aware();
        result.Should().Be(result); // no-op assertion, we just want no throw
    }
}
