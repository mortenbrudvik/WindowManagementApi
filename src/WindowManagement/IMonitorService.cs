using R3;

namespace WindowManagement;

public interface IMonitorService
{
    IReadOnlyList<IMonitor> All { get; }
    IMonitor Primary { get; }
    IMonitor GetFor(IWindow window);
    IMonitor? GetAt(int x, int y);

    Observable<MonitorEventArgs> Connected { get; }
    Observable<MonitorEventArgs> Disconnected { get; }
    Observable<MonitorEventArgs> SettingsChanged { get; }
}
