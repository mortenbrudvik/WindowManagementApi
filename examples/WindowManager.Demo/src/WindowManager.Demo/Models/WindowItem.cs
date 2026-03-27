using WindowManagement;

namespace WindowManager.Demo.Models;

public class WindowItem
{
    public IWindow Window { get; }

    public string Title => Window.Title;
    public string ProcessName => Window.ProcessName;
    public int ProcessId => Window.ProcessId;
    public string ClassName => Window.ClassName;
    public nint Handle => Window.Handle;
    public string HandleHex => $"0x{Handle:X8}";
    public WindowRect Bounds => Window.Bounds;
    public string BoundsText => $"{Bounds.X}, {Bounds.Y}, {Bounds.Width} x {Bounds.Height}";
    public WindowState State => Window.State;
    public bool IsTopmost => Window.IsTopmost;
    public string MonitorName => Window.Monitor.DeviceName;
    public int MonitorDpi => Window.Monitor.Dpi;
    public double MonitorScale => Window.Monitor.ScaleFactor;
    public string MonitorSummary => $"{MonitorName} ({Window.Monitor.Bounds.Width}x{Window.Monitor.Bounds.Height}, {MonitorDpi}DPI)";

    public WindowItem(IWindow window)
    {
        Window = window;
    }
}
