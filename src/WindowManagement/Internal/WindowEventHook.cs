using R3;
using WindowManagement.LowLevel;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;

namespace WindowManagement.Internal;

internal class WindowEventHook : IDisposable
{
    private readonly IWindowApi _windowApi;
    private readonly IDisplayApi _displayApi;
    private readonly Subject<WindowEventArgs> _created = new();
    private readonly Subject<WindowEventArgs> _destroyed = new();
    private readonly Subject<WindowMovedEventArgs> _moved = new();
    private readonly Subject<WindowMovedEventArgs> _resized = new();
    private readonly Subject<WindowStateEventArgs> _stateChanged = new();
    private readonly WINEVENTPROC _callback;
    private readonly UnhookWinEventSafeHandle? _hookHandle;
    private readonly Dictionary<nint, WindowRect> _trackedBounds = new();
    private readonly Dictionary<nint, WindowState> _trackedStates = new();
    private int _disposed;

    public Observable<WindowEventArgs> Created => _created;
    public Observable<WindowEventArgs> Destroyed => _destroyed;
    public Observable<WindowMovedEventArgs> Moved => _moved;
    public Observable<WindowMovedEventArgs> Resized => _resized;
    public Observable<WindowStateEventArgs> StateChanged => _stateChanged;

    public WindowEventHook(IWindowApi windowApi, IDisplayApi displayApi)
    {
        _windowApi = windowApi;
        _displayApi = displayApi;

        _callback = OnWinEvent;

        try
        {
            _hookHandle = PInvoke.SetWinEventHook(
                (uint)0x8000, // EVENT_OBJECT_CREATE
                (uint)0x800B, // EVENT_OBJECT_LOCATIONCHANGE
                null,
                _callback,
                0, 0,
                0x0000); // WINEVENT_OUTOFCONTEXT
        }
        catch
        {
            // Event hooks may fail in certain contexts (e.g., test runners)
            // The manager still works without events
        }
    }

    private void OnWinEvent(HWINEVENTHOOK hWinEventHook, uint @event, HWND hwnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime)
    {
        // Only handle window-level events (not child objects)
        if (idObject != 0 || hwnd.IsNull)
            return;

        var handle = (nint)hwnd;

        switch (@event)
        {
            case 0x8000: // EVENT_OBJECT_CREATE
                EmitCreated(handle);
                break;
            case 0x8001: // EVENT_OBJECT_DESTROY
                EmitDestroyed(handle);
                break;
            case 0x800B: // EVENT_OBJECT_LOCATIONCHANGE
                EmitLocationChange(handle);
                break;
            case 0x0016: // EVENT_SYSTEM_MINIMIZESTART
            case 0x0017: // EVENT_SYSTEM_MINIMIZEEND
                EmitStateChange(handle);
                break;
        }
    }

    private void EmitCreated(nint handle)
    {
        if (!_windowApi.IsValid(handle)) return;
        var title = _windowApi.GetTitle(handle);
        _created.OnNext(new WindowEventArgs
        {
            Handle = handle,
            Title = title,
            ProcessName = GetProcessNameSafe(handle)
        });
    }

    private void EmitDestroyed(nint handle)
    {
        _trackedBounds.Remove(handle);
        _trackedStates.Remove(handle);
        _destroyed.OnNext(new WindowEventArgs
        {
            Handle = handle,
            Title = string.Empty,
            ProcessName = string.Empty
        });
    }

    private void EmitLocationChange(nint handle)
    {
        if (!_windowApi.IsValid(handle)) return;

        var newBounds = _windowApi.GetBounds(handle);
        var hadBounds = _trackedBounds.TryGetValue(handle, out var oldBounds);
        _trackedBounds[handle] = newBounds;

        if (!hadBounds) return;

        if (oldBounds!.X != newBounds.X || oldBounds.Y != newBounds.Y)
        {
            _moved.OnNext(new WindowMovedEventArgs
            {
                Handle = handle,
                Title = _windowApi.GetTitle(handle),
                ProcessName = GetProcessNameSafe(handle),
                OldBounds = oldBounds,
                NewBounds = newBounds
            });
        }

        if (oldBounds.Width != newBounds.Width || oldBounds.Height != newBounds.Height)
        {
            _resized.OnNext(new WindowMovedEventArgs
            {
                Handle = handle,
                Title = _windowApi.GetTitle(handle),
                ProcessName = GetProcessNameSafe(handle),
                OldBounds = oldBounds,
                NewBounds = newBounds
            });
        }
    }

    private void EmitStateChange(nint handle)
    {
        if (!_windowApi.IsValid(handle)) return;

        var newState = _windowApi.GetState(handle);
        var hadState = _trackedStates.TryGetValue(handle, out var oldState);
        _trackedStates[handle] = newState;

        if (hadState && oldState != newState)
        {
            _stateChanged.OnNext(new WindowStateEventArgs
            {
                Handle = handle,
                Title = _windowApi.GetTitle(handle),
                ProcessName = GetProcessNameSafe(handle),
                OldState = oldState,
                NewState = newState
            });
        }
    }

    private string GetProcessNameSafe(nint handle)
    {
        try
        {
            var pid = _windowApi.GetProcessId(handle);
            using var process = System.Diagnostics.Process.GetProcessById(pid);
            return process.ProcessName;
        }
        catch { return string.Empty; }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        _hookHandle?.Dispose();
        _created.Dispose();
        _destroyed.Dispose();
        _moved.Dispose();
        _resized.Dispose();
        _stateChanged.Dispose();
    }
}
