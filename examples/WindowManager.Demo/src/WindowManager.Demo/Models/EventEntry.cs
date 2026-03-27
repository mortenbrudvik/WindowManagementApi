namespace WindowManager.Demo.Models;

public class EventEntry
{
    public DateTime Timestamp { get; init; }
    public string TimestampText => Timestamp.ToString("HH:mm:ss.fff");
    public string EventType { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
}
