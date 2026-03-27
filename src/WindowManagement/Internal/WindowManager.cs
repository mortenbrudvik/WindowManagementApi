using System.Diagnostics;
using Microsoft.Extensions.Logging;
using R3;
using WindowManagement.Exceptions;
using WindowManagement.Filtering;
using WindowManagement.LowLevel;

namespace WindowManagement.Internal;

internal class WindowManager : IWindowManager
{
    private readonly IWindowApi _windowApi;
    private readonly IDisplayApi _displayApi;
    private readonly MonitorService _monitorService;
    private readonly WindowEventHook? _eventHook;
    private readonly ILogger? _logger;

    // Lazy event subjects for when event hook is not available
    private static readonly Observable<WindowEventArgs> EmptyWindowEvents = Observable.Empty<WindowEventArgs>();
    private static readonly Observable<WindowMovedEventArgs> EmptyMovedEvents = Observable.Empty<WindowMovedEventArgs>();
    private static readonly Observable<WindowStateEventArgs> EmptyStateEvents = Observable.Empty<WindowStateEventArgs>();

    public IMonitorService Monitors => _monitorService;

    public Observable<WindowEventArgs> Created => _eventHook?.Created ?? EmptyWindowEvents;
    public Observable<WindowEventArgs> Destroyed => _eventHook?.Destroyed ?? EmptyWindowEvents;
    public Observable<WindowMovedEventArgs> Moved => _eventHook?.Moved ?? EmptyMovedEvents;
    public Observable<WindowMovedEventArgs> Resized => _eventHook?.Resized ?? EmptyMovedEvents;
    public Observable<WindowStateEventArgs> StateChanged => _eventHook?.StateChanged ?? EmptyStateEvents;

    public WindowManager(IWindowApi windowApi, IDisplayApi displayApi, bool enforceDpiAwareness = true, ILogger<WindowManager>? logger = null)
    {
        if (enforceDpiAwareness && !displayApi.IsPerMonitorV2Aware())
            throw new DpiAwarenessException();

        _windowApi = windowApi;
        _displayApi = displayApi;
        _monitorService = new MonitorService(displayApi);
        _logger = logger;

        try
        {
            _eventHook = new WindowEventHook(windowApi, displayApi);
        }
        catch
        {
            // Event hooks may fail in certain contexts (e.g., test runners with mocked APIs)
        }
    }

    public IReadOnlyList<IWindow> GetAll(WindowFilter? filter = null)
    {
        filter ??= new WindowFilter();
        var handles = _windowApi.Enumerate(filter.AltTabOnly);

        var windows = new List<IWindow>();
        foreach (var hwnd in handles)
        {
            var window = BuildWindow(hwnd);
            if (window == null) continue;
            if (!MatchesFilter(window, filter)) continue;
            windows.Add(window);
        }

        return windows;
    }

    public IReadOnlyList<IWindow> GetAll(Action<WindowFilterBuilder> configure)
    {
        var builder = new WindowFilterBuilder();
        configure(builder);
        return GetAll(builder.Build());
    }

    public IWindow? GetForeground()
    {
        var hwnd = _windowApi.GetForeground();
        return hwnd == 0 ? null : BuildWindow(hwnd);
    }

    public Task MoveAsync(IWindow window, int x, int y)
    {
        _windowApi.Move(window.Handle, x, y);
        return Task.CompletedTask;
    }

    public Task ResizeAsync(IWindow window, int width, int height)
    {
        _windowApi.Resize(window.Handle, width, height);
        return Task.CompletedTask;
    }

    public Task SetBoundsAsync(IWindow window, WindowRect bounds)
    {
        _windowApi.SetBounds(window.Handle, bounds);
        return Task.CompletedTask;
    }

    public Task MoveToMonitorAsync(IWindow window, IMonitor monitor)
    {
        var workArea = monitor.WorkArea;
        _windowApi.SetBounds(window.Handle, new WindowRect(workArea.X, workArea.Y, window.Bounds.Width, window.Bounds.Height));
        return Task.CompletedTask;
    }

    public Task SetStateAsync(IWindow window, WindowState state)
    {
        _windowApi.SetState(window.Handle, state);
        return Task.CompletedTask;
    }

    public Task FocusAsync(IWindow window)
    {
        _windowApi.Focus(window.Handle);
        return Task.CompletedTask;
    }

    public Task SnapAsync(IWindow window, IMonitor monitor, SnapPosition position)
    {
        var targetBounds = SnapCalculator.Calculate(monitor.WorkArea, position);

        // Restore if minimized or maximized
        var state = _windowApi.GetState(window.Handle);
        if (state != WindowState.Normal)
            _windowApi.RestoreMinimized(window.Handle);

        // Handle non-resizable windows: center in zone
        if (!_windowApi.IsResizable(window.Handle))
        {
            var currentBounds = _windowApi.GetBounds(window.Handle);
            var x = targetBounds.X + (targetBounds.Width - currentBounds.Width) / 2;
            var y = targetBounds.Y + (targetBounds.Height - currentBounds.Height) / 2;
            _windowApi.SetBounds(window.Handle, new WindowRect(x, y, currentBounds.Width, currentBounds.Height));
            return Task.CompletedTask;
        }

        // Compensate for invisible borders
        var borders = _windowApi.GetInvisibleBorders(window.Handle);
        var adjusted = new WindowRect(
            targetBounds.X - borders.left,
            targetBounds.Y - borders.top,
            targetBounds.Width + borders.left + borders.right,
            targetBounds.Height + borders.top + borders.bottom);

        // Cross-monitor DPI compensation
        var windowDpi = _windowApi.GetDpi(window.Handle);
        var targetDpi = (uint)monitor.Dpi;

        if (windowDpi > 0 && targetDpi > 0 && windowDpi != targetDpi)
        {
            double dpiRatio = (double)windowDpi / targetDpi;
            adjusted = new WindowRect(
                adjusted.X,
                adjusted.Y,
                (int)Math.Round(adjusted.Width * dpiRatio),
                (int)Math.Round(adjusted.Height * dpiRatio));
        }

        _windowApi.SetBounds(window.Handle, adjusted);
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _eventHook?.Dispose();
        _monitorService.Dispose();
    }

    private WindowInfo? BuildWindow(nint hwnd)
    {
        try
        {
            var title = _windowApi.GetTitle(hwnd);
            var pid = _windowApi.GetProcessId(hwnd);
            var processName = GetProcessName(pid);

            return new WindowInfo
            {
                Handle = hwnd,
                Title = title,
                ProcessName = processName,
                ProcessId = pid,
                ClassName = _windowApi.GetClassName(hwnd),
                Bounds = _windowApi.GetBounds(hwnd),
                State = _windowApi.GetState(hwnd),
                Monitor = _monitorService.GetFor(new WindowInfo
                {
                    Handle = hwnd, Title = title, ProcessName = processName,
                    ProcessId = pid, ClassName = "", Bounds = new WindowRect(0, 0, 0, 0),
                    State = WindowState.Normal, Monitor = null!, IsTopmost = false
                }),
                IsTopmost = _windowApi.IsTopmost(hwnd)
            };
        }
        catch
        {
            return null;
        }
    }

    private static string GetProcessName(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return process.ProcessName;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool MatchesFilter(IWindow window, WindowFilter filter)
    {
        if (filter.ProcessName != null &&
            !window.ProcessName.Equals(filter.ProcessName, StringComparison.OrdinalIgnoreCase))
            return false;

        if (filter.TitlePattern != null &&
            !MatchesWildcard(window.Title, filter.TitlePattern))
            return false;

        if (filter.ClassName != null &&
            !window.ClassName.Equals(filter.ClassName, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!filter.IncludeMinimized && window.State == WindowState.Minimized)
            return false;

        if (filter.Predicate != null && !filter.Predicate(window))
            return false;

        return true;
    }

    private static bool MatchesWildcard(string text, string pattern)
    {
        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        return System.Text.RegularExpressions.Regex.IsMatch(text, regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}
