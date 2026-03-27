using FluentAssertions;
using WindowManagement.Filtering;
using Xunit;

namespace WindowManagement.Tests.Filtering;

public class WindowFilterBuilderTests
{
    [Fact]
    public void Default__UseAltTabFiltering()
    {
        var builder = new WindowFilterBuilder();
        var filter = builder.Build();

        filter.AltTabOnly.Should().BeTrue();
    }

    [Fact]
    public void Unfiltered__DisablesAltTabFiltering()
    {
        var builder = new WindowFilterBuilder();
        builder.Unfiltered();
        var filter = builder.Build();

        filter.AltTabOnly.Should().BeFalse();
    }

    [Fact]
    public void WithProcess__SetsProcessNameFilter()
    {
        var builder = new WindowFilterBuilder();
        builder.WithProcess("notepad");
        var filter = builder.Build();

        filter.ProcessName.Should().Be("notepad");
    }

    [Fact]
    public void WithTitle__SetsTitlePattern()
    {
        var builder = new WindowFilterBuilder();
        builder.WithTitle("*.txt");
        var filter = builder.Build();

        filter.TitlePattern.Should().Be("*.txt");
    }

    [Fact]
    public void ExcludeMinimized__SetsFlag()
    {
        var builder = new WindowFilterBuilder();
        builder.ExcludeMinimized();
        var filter = builder.Build();

        filter.IncludeMinimized.Should().BeFalse();
    }

    [Fact]
    public void Where__AddsPredicate()
    {
        var builder = new WindowFilterBuilder();
        builder.Where(w => w.Bounds.Width > 500);
        var filter = builder.Build();

        filter.Predicate.Should().NotBeNull();
    }

    [Fact]
    public void FluentChaining__AllFiltersApplied()
    {
        var builder = new WindowFilterBuilder();
        builder
            .WithProcess("notepad")
            .WithTitle("*.txt")
            .ExcludeMinimized();
        var filter = builder.Build();

        filter.ProcessName.Should().Be("notepad");
        filter.TitlePattern.Should().Be("*.txt");
        filter.IncludeMinimized.Should().BeFalse();
        filter.AltTabOnly.Should().BeTrue();
    }
}
