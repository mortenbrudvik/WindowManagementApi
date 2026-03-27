namespace WindowManagement.LowLevel;

public interface IDisplayApi
{
    IReadOnlyList<DisplayInfo> GetAll();
    DisplayInfo GetPrimary();
    DisplayInfo GetForWindow(nint hwnd);
    DisplayInfo? GetAtPoint(int x, int y);
    int GetDpi(nint hmonitor);
    double GetScaleFactor(nint hmonitor);
    WindowRect LogicalToPhysical(WindowRect rect, nint hmonitor);
    WindowRect PhysicalToLogical(WindowRect rect, nint hmonitor);
    bool IsPerMonitorV2Aware();
}
