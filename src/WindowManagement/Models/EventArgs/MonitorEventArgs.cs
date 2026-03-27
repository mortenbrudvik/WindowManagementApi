namespace WindowManagement;

public class MonitorEventArgs : EventArgs
{
    public required string DeviceName { get; init; }
    public required WindowRect Bounds { get; init; }
}
