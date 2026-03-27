namespace WindowManagement;

public class WindowMovedEventArgs : WindowEventArgs
{
    public required WindowRect OldBounds { get; init; }
    public required WindowRect NewBounds { get; init; }
}
