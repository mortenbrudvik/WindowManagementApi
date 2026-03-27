using FluentAssertions;
using WindowManagement.Exceptions;
using Xunit;

namespace WindowManagement.Tests.Exceptions;

public class ExceptionTests
{
    [Fact]
    public void WindowManagementException__IsException()
    {
        var ex = new WindowManagementException("test");
        ex.Should().BeAssignableTo<Exception>();
        ex.Message.Should().Be("test");
    }

    [Fact]
    public void WindowNotFoundException__IsWindowManagementException()
    {
        var ex = new WindowNotFoundException(0x1234);
        ex.Should().BeAssignableTo<WindowManagementException>();
        ex.Handle.Should().Be(0x1234);
    }

    [Fact]
    public void DpiAwarenessException__IsWindowManagementException()
    {
        var ex = new DpiAwarenessException();
        ex.Should().BeAssignableTo<WindowManagementException>();
    }
}
