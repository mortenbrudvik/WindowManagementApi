namespace WindowManagement.Internal;

internal class WindowInfo : IWindow
{
    public required nint Handle { get; init; }
    public required string Title { get; init; }
    public required string ProcessName { get; init; }
    public required int ProcessId { get; init; }
    public required string ClassName { get; init; }
    public required WindowRect Bounds { get; init; }
    public required WindowState State { get; init; }
    public required IMonitor Monitor { get; init; }
    public required bool IsTopmost { get; init; }
}
