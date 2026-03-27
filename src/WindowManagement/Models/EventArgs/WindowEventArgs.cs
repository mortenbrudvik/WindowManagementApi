namespace WindowManagement;

public class WindowEventArgs : EventArgs
{
    public required nint Handle { get; init; }
    public required string Title { get; init; }
    public required string ProcessName { get; init; }
}
