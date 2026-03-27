using FluentAssertions;
using Xunit;

namespace WindowManagement.Tests.Models;

public class WindowRectTests
{
    [Fact]
    public void Constructor__SetsAllProperties()
    {
        var rect = new WindowRect(10, 20, 800, 600);

        rect.X.Should().Be(10);
        rect.Y.Should().Be(20);
        rect.Width.Should().Be(800);
        rect.Height.Should().Be(600);
    }

    [Fact]
    public void Right__ReturnsXPlusWidth()
    {
        var rect = new WindowRect(10, 20, 800, 600);

        rect.Right.Should().Be(810);
    }

    [Fact]
    public void Bottom__ReturnsYPlusHeight()
    {
        var rect = new WindowRect(10, 20, 800, 600);

        rect.Bottom.Should().Be(620);
    }

    [Fact]
    public void Equality__TwoRectsWithSameValues_AreEqual()
    {
        var a = new WindowRect(10, 20, 800, 600);
        var b = new WindowRect(10, 20, 800, 600);

        a.Should().Be(b);
    }
}
