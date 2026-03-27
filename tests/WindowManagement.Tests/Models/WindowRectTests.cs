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

    [Fact]
    public void Constructor__NegativeWidth_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new WindowRect(0, 0, -1, 100);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor__NegativeHeight_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new WindowRect(0, 0, 100, -1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor__ZeroDimensions_Allowed()
    {
        var rect = new WindowRect(0, 0, 0, 0);

        rect.Width.Should().Be(0);
        rect.Height.Should().Be(0);
    }

    [Fact]
    public void WithExpression__NegativeWidth_ThrowsArgumentOutOfRangeException()
    {
        var rect = new WindowRect(0, 0, 100, 100);
        var act = () => rect with { Width = -1 };

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WithExpression__NegativeHeight_ThrowsArgumentOutOfRangeException()
    {
        var rect = new WindowRect(0, 0, 100, 100);
        var act = () => rect with { Height = -1 };

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WithExpression__ValidValues_CreatesNewRect()
    {
        var rect = new WindowRect(10, 20, 100, 200);
        var modified = rect with { Width = 50, Height = 80 };

        modified.X.Should().Be(10);
        modified.Y.Should().Be(20);
        modified.Width.Should().Be(50);
        modified.Height.Should().Be(80);
    }

    [Fact]
    public void Deconstruct__ReturnsAllFourComponents()
    {
        var rect = new WindowRect(10, 20, 800, 600);
        var (x, y, width, height) = rect;

        x.Should().Be(10);
        y.Should().Be(20);
        width.Should().Be(800);
        height.Should().Be(600);
    }
}
