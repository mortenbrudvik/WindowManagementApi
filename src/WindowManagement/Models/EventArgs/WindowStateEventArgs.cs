namespace WindowManagement;

public class WindowStateEventArgs : WindowEventArgs
{
    public required WindowState OldState { get; init; }
    public required WindowState NewState { get; init; }
}
