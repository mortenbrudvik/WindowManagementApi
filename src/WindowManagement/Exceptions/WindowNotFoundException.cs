namespace WindowManagement.Exceptions;

public class WindowNotFoundException : WindowManagementException
{
    public nint Handle { get; }

    public WindowNotFoundException(nint handle)
        : base($"Window handle 0x{handle:X} is no longer valid.")
    {
        Handle = handle;
    }
}
