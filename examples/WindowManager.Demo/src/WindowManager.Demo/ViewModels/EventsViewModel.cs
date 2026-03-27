using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using R3;
using WindowManagement;
using WindowManager.Demo.Models;

namespace WindowManager.Demo.ViewModels;

public partial class EventsViewModel : ObservableObject, IDisposable
{
    private readonly IDisposable _subscriptions;

    [ObservableProperty]
    private bool _showWindowCreated = true;

    [ObservableProperty]
    private bool _showWindowDestroyed = true;

    [ObservableProperty]
    private bool _showWindowMoved = true;

    [ObservableProperty]
    private bool _showWindowResized = true;

    [ObservableProperty]
    private bool _showStateChanged = true;

    [ObservableProperty]
    private bool _showMonitorEvents = true;

    [ObservableProperty]
    private bool _autoScroll = true;

    public ObservableCollection<EventEntry> Events { get; } = [];
    public ObservableCollection<EventEntry> FilteredEvents { get; } = [];

    public EventsViewModel(IWindowManager windowManager)
    {
        _subscriptions = Disposable.Combine(
            windowManager.Created.Subscribe(e =>
                AddEvent("Window Created", $"{e.Title} ({e.ProcessName})")),
            windowManager.Destroyed.Subscribe(e =>
                AddEvent("Window Destroyed", $"{e.Title} ({e.ProcessName})")),
            windowManager.Moved.Subscribe(e =>
                AddEvent("Window Moved", $"{e.Title} — ({e.OldBounds.X},{e.OldBounds.Y}) -> ({e.NewBounds.X},{e.NewBounds.Y})")),
            windowManager.Resized.Subscribe(e =>
                AddEvent("Window Resized", $"{e.Title} — {e.OldBounds.Width}x{e.OldBounds.Height} -> {e.NewBounds.Width}x{e.NewBounds.Height}")),
            windowManager.StateChanged.Subscribe(e =>
                AddEvent("State Changed", $"{e.Title} — {e.OldState} -> {e.NewState}")),
            windowManager.Monitors.Connected.Subscribe(e =>
                AddEvent("Monitor Connected", $"{e.DeviceName} — {e.Bounds.Width}x{e.Bounds.Height}")),
            windowManager.Monitors.Disconnected.Subscribe(e =>
                AddEvent("Monitor Disconnected", e.DeviceName)),
            windowManager.Monitors.SettingsChanged.Subscribe(e =>
                AddEvent("Monitor Settings", $"{e.DeviceName} — {e.Bounds.Width}x{e.Bounds.Height}"))
        );
    }

    private void AddEvent(string type, string details)
    {
        var entry = new EventEntry
        {
            Timestamp = DateTime.Now,
            EventType = type,
            Details = details
        };

        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            Events.Insert(0, entry);
            if (ShouldShow(entry))
            {
                FilteredEvents.Insert(0, entry);
            }
        });
    }

    private bool ShouldShow(EventEntry entry) => entry.EventType switch
    {
        "Window Created" => ShowWindowCreated,
        "Window Destroyed" => ShowWindowDestroyed,
        "Window Moved" => ShowWindowMoved,
        "Window Resized" => ShowWindowResized,
        "State Changed" => ShowStateChanged,
        "Monitor Connected" or "Monitor Disconnected" or "Monitor Settings" => ShowMonitorEvents,
        _ => true
    };

    [RelayCommand]
    private void ClearEvents()
    {
        Events.Clear();
        FilteredEvents.Clear();
    }

    partial void OnShowWindowCreatedChanged(bool value) => RebuildFiltered();
    partial void OnShowWindowDestroyedChanged(bool value) => RebuildFiltered();
    partial void OnShowWindowMovedChanged(bool value) => RebuildFiltered();
    partial void OnShowWindowResizedChanged(bool value) => RebuildFiltered();
    partial void OnShowStateChangedChanged(bool value) => RebuildFiltered();
    partial void OnShowMonitorEventsChanged(bool value) => RebuildFiltered();

    private void RebuildFiltered()
    {
        FilteredEvents.Clear();
        foreach (EventEntry entry in Events)
        {
            if (ShouldShow(entry))
            {
                FilteredEvents.Add(entry);
            }
        }
    }

    public void Dispose()
    {
        _subscriptions.Dispose();
    }
}
