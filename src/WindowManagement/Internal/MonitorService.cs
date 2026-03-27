using System.Diagnostics;
using Microsoft.Win32;
using WindowManagement.Exceptions;
using R3;
using WindowManagement.LowLevel;

namespace WindowManagement.Internal;

internal class MonitorService : IMonitorService, IDisposable
{
    private readonly IDisplayApi _displayApi;
    private readonly Subject<MonitorEventArgs> _connected = new();
    private readonly Subject<MonitorEventArgs> _disconnected = new();
    private readonly Subject<MonitorEventArgs> _settingsChanged = new();
    private List<IMonitor>? _cachedMonitors;

    public MonitorService(IDisplayApi displayApi)
    {
        _displayApi = displayApi;
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    public IReadOnlyList<IMonitor> All => _cachedMonitors ??= LoadMonitors();

    public IMonitor Primary => All.FirstOrDefault(m => m.IsPrimary)
        ?? throw new WindowManagementException("No primary monitor found. Monitor enumeration may have failed.");

    public IMonitor GetFor(IWindow window)
    {
        var display = _displayApi.GetForWindow(window.Handle);
        return ToMonitor(display);
    }

    public IMonitor? GetAt(int x, int y)
    {
        var display = _displayApi.GetAtPoint(x, y);
        return display != null ? ToMonitor(display) : null;
    }

    public Observable<MonitorEventArgs> Connected => _connected;
    public Observable<MonitorEventArgs> Disconnected => _disconnected;
    public Observable<MonitorEventArgs> SettingsChanged => _settingsChanged;

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        var previousCache = _cachedMonitors;

        IReadOnlyList<IMonitor> oldMonitors;
        IReadOnlyList<IMonitor> newMonitors;
        try
        {
            oldMonitors = _cachedMonitors ?? [];
            _cachedMonitors = null;
            newMonitors = All;
        }
        catch (WindowManagementException ex)
        {
            _cachedMonitors = previousCache;
            Trace.TraceError($"Failed to enumerate monitors during display settings change: {ex}");
            return;
        }

        var oldNames = oldMonitors.Select(m => m.DeviceName).ToHashSet();
        var newNames = newMonitors.Select(m => m.DeviceName).ToHashSet();

        foreach (var monitor in newMonitors.Where(m => !oldNames.Contains(m.DeviceName)))
            _connected.OnNext(new MonitorEventArgs { DeviceName = monitor.DeviceName, Bounds = monitor.Bounds });

        foreach (var monitor in oldMonitors.Where(m => !newNames.Contains(m.DeviceName)))
            _disconnected.OnNext(new MonitorEventArgs { DeviceName = monitor.DeviceName, Bounds = monitor.Bounds });

        foreach (var monitor in newMonitors.Where(m => oldNames.Contains(m.DeviceName)))
            _settingsChanged.OnNext(new MonitorEventArgs { DeviceName = monitor.DeviceName, Bounds = monitor.Bounds });
    }

    private List<IMonitor> LoadMonitors()
    {
        return _displayApi.GetAll().Select(ToMonitor).ToList<IMonitor>();
    }

    private static IMonitor ToMonitor(DisplayInfo d) => new MonitorInfo
    {
        Handle = d.Handle,
        DeviceName = d.DeviceName,
        DisplayName = d.DisplayName,
        IsPrimary = d.IsPrimary,
        Bounds = d.Bounds,
        WorkArea = d.WorkArea,
        Dpi = d.Dpi,
        ScaleFactor = d.ScaleFactor
    };

    public void Dispose()
    {
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        _connected.Dispose();
        _disconnected.Dispose();
        _settingsChanged.Dispose();
    }
}
