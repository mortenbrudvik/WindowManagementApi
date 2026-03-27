using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using R3;
using WindowManagement;

namespace WindowManager.Demo.ViewModels;

public partial class MonitorsViewModel : ObservableObject, IDisposable
{
    private readonly IMonitorService _monitorService;
    private readonly IDisposable _subscriptions;

    [ObservableProperty]
    private IMonitor? _selectedMonitor;

    public ObservableCollection<IMonitor> Monitors { get; } = [];

    [ObservableProperty]
    private double _canvasScale = 0.15;

    [ObservableProperty]
    private double _offsetX;

    [ObservableProperty]
    private double _offsetY;

    public MonitorsViewModel(IMonitorService monitorService)
    {
        _monitorService = monitorService;

        _subscriptions = Disposable.Combine(
            monitorService.Connected.Subscribe(_ => RefreshMonitors()),
            monitorService.Disconnected.Subscribe(_ => RefreshMonitors()),
            monitorService.SettingsChanged.Subscribe(_ => RefreshMonitors())
        );
    }

    public void OnNavigatedTo()
    {
        RefreshMonitors();
    }

    [RelayCommand]
    private void RefreshMonitors()
    {
        Monitors.Clear();

        int minX = int.MaxValue, minY = int.MaxValue;
        int maxRight = int.MinValue, maxBottom = int.MinValue;

        foreach (IMonitor monitor in _monitorService.All)
        {
            Monitors.Add(monitor);

            if (monitor.Bounds.X < minX) minX = monitor.Bounds.X;
            if (monitor.Bounds.Y < minY) minY = monitor.Bounds.Y;
            if (monitor.Bounds.Right > maxRight) maxRight = monitor.Bounds.Right;
            if (monitor.Bounds.Bottom > maxBottom) maxBottom = monitor.Bounds.Bottom;
        }

        OffsetX = -minX;
        OffsetY = -minY;

        int totalWidth = maxRight - minX;
        int totalHeight = maxBottom - minY;

        if (totalWidth > 0)
        {
            CanvasScale = 500.0 / totalWidth;
            double heightScale = 350.0 / totalHeight;
            if (heightScale < CanvasScale) CanvasScale = heightScale;
        }

        SelectedMonitor ??= Monitors.FirstOrDefault(m => m.IsPrimary)
                            ?? Monitors.FirstOrDefault();
    }

    public void Dispose()
    {
        _subscriptions.Dispose();
    }
}
