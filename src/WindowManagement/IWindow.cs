namespace WindowManagement;

public interface IWindow
{
    nint Handle { get; }
    string Title { get; }
    string ProcessName { get; }
    int ProcessId { get; }
    string ClassName { get; }
    WindowRect Bounds { get; }
    WindowState State { get; }
    IMonitor Monitor { get; }
    bool IsTopmost { get; }
}
