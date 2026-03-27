using FluentAssertions;
using WindowManagement.Internal;
using Xunit;

namespace WindowManagement.Tests;

public class WindowManagerSnapTests
{
    [Theory]
    [InlineData(SnapPosition.Fill, 0, 0, 1920, 1040)]
    [InlineData(SnapPosition.Left, 0, 0, 960, 1040)]
    [InlineData(SnapPosition.Right, 960, 0, 960, 1040)]
    [InlineData(SnapPosition.Top, 0, 0, 1920, 520)]
    [InlineData(SnapPosition.Bottom, 0, 520, 1920, 520)]
    [InlineData(SnapPosition.TopLeft, 0, 0, 960, 520)]
    [InlineData(SnapPosition.TopRight, 960, 0, 960, 520)]
    [InlineData(SnapPosition.BottomLeft, 0, 520, 960, 520)]
    [InlineData(SnapPosition.BottomRight, 960, 520, 960, 520)]
    public void CalculateSnapBounds__ReturnsCorrectBounds(SnapPosition position, int x, int y, int w, int h)
    {
        var workArea = new WindowRect(0, 0, 1920, 1040);

        var result = SnapCalculator.Calculate(workArea, position);

        result.Should().Be(new WindowRect(x, y, w, h));
    }

    [Theory]
    [InlineData(SnapPosition.Left, 1920, 0, 1280, 1400)]
    [InlineData(SnapPosition.Right, 3200, 0, 1280, 1400)]
    public void CalculateSnapBounds__SecondMonitor_CorrectOffset(SnapPosition position, int x, int y, int w, int h)
    {
        var workArea = new WindowRect(1920, 0, 2560, 1400);

        var result = SnapCalculator.Calculate(workArea, position);

        result.Should().Be(new WindowRect(x, y, w, h));
    }
}
