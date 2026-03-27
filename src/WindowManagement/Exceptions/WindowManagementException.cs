namespace WindowManagement.Exceptions;

public class WindowManagementException : Exception
{
    public WindowManagementException(string message) : base(message) { }
    public WindowManagementException(string message, Exception inner) : base(message, inner) { }
}
