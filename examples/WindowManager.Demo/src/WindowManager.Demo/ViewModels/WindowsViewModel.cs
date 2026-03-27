using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using R3;
using WindowManagement;
using WindowManager.Demo.Models;

namespace WindowManager.Demo.ViewModels;

public partial class WindowsViewModel : ObservableObject, IDisposable
{
    private readonly IWindowManager _windowManager;
    private readonly IDisposable _subscriptions;

    [ObservableProperty]
    private WindowItem? _selectedWindow;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ObservableCollection<WindowItem> Windows { get; } = [];

    public WindowsViewModel(IWindowManager windowManager)
    {
        _windowManager = windowManager;

        _subscriptions = Disposable.Combine(
            windowManager.Created.Subscribe(_ =>
                Application.Current?.Dispatcher.Invoke(RefreshWindows)),
            windowManager.Destroyed.Subscribe(_ =>
                Application.Current?.Dispatcher.Invoke(RefreshWindows))
        );
    }

    public void OnNavigatedTo()
    {
        RefreshWindows();
    }

    [RelayCommand]
    private void RefreshWindows()
    {
        var previous = SelectedWindow?.Handle;
        Windows.Clear();

        foreach (IWindow window in _windowManager.GetAll())
        {
            var item = new WindowItem(window);

            if (!string.IsNullOrEmpty(SearchText)
                && !item.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                && !item.ProcessName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Windows.Add(item);
        }

        if (previous.HasValue)
        {
            SelectedWindow = Windows.FirstOrDefault(w => w.Handle == previous.Value);
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        RefreshWindows();
    }

    public void Dispose()
    {
        _subscriptions.Dispose();
    }
}
