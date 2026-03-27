using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WindowManagement;
using WindowManager.Demo.Models;

namespace WindowManager.Demo.ViewModels;

public partial class SnapViewModel : ObservableObject
{
    private readonly IWindowManager _windowManager;

    [ObservableProperty]
    private WindowItem? _selectedWindow;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isStatusSuccess;

    public ObservableCollection<WindowItem> Windows { get; } = [];
    public ObservableCollection<IMonitor> Monitors { get; } = [];

    public SnapViewModel(IWindowManager windowManager)
    {
        _windowManager = windowManager;
    }

    public void OnNavigatedTo()
    {
        RefreshWindows();
        RefreshMonitors();
        PreSelectForeground();
    }

    [RelayCommand]
    private void RefreshWindows()
    {
        Windows.Clear();
        foreach (IWindow window in _windowManager.GetAll())
        {
            Windows.Add(new WindowItem(window));
        }
    }

    private void RefreshMonitors()
    {
        Monitors.Clear();
        foreach (IMonitor monitor in _windowManager.Monitors.All)
        {
            Monitors.Add(monitor);
        }
    }

    private void PreSelectForeground()
    {
        IWindow? foreground = _windowManager.GetForeground();
        if (foreground is not null)
        {
            SelectedWindow = Windows.FirstOrDefault(w => w.Handle == foreground.Handle);
        }
    }

    [RelayCommand]
    private async Task SnapToAsync(SnapRequest request)
    {
        if (SelectedWindow is null)
        {
            StatusMessage = "No window selected";
            IsStatusSuccess = false;
            return;
        }

        try
        {
            await _windowManager.SnapAsync(SelectedWindow.Window, request.Monitor, request.Position);
            StatusMessage = $"Snapped \"{SelectedWindow.Title}\" to {request.Position} on {request.Monitor.DeviceName}";
            IsStatusSuccess = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Snap failed: {ex.Message}";
            IsStatusSuccess = false;
            RefreshWindows();
        }
    }
}

public record SnapRequest(IMonitor Monitor, SnapPosition Position);
