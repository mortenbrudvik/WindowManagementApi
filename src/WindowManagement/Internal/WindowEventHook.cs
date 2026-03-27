using System.Diagnostics;
using R3;
using WindowManagement.Exceptions;
using WindowManagement.LowLevel;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;

namespace WindowManagement.Internal;

internal class WindowEventHook : IDisposable
{
    private const uint EVENT_SYSTEM_MINIMIZESTART = 0x0016;
    private const uint EVENT_SYSTEM_MINIMIZEEND = 0x0017;
    private const uint EVENT_OBJECT_CREATE = 0x8000;
    private const uint EVENT_OBJECT_DESTROY = 0x8001;
    private const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
    private const uint WINEVENT_OUTOFCONTEXT = 0x0000;

    private readonly IWindowApi _windowApi;
    private readonly IDisplayApi _displayApi;
    private readonly Subject<WindowEventArgs> _created = new();
    private readonly Subject<WindowEventArgs> _destroyed = new();
    private readonly Subject<WindowMovedEventArgs> _moved = new();
    private readonly Subject<WindowMovedEventArgs> _resized = new();
    private readonly Subject<WindowStateEventArgs> _stateChanged = new();

    private readonly WINEVENTPROC _callback;
    private readonly UnhookWinEventSafeHandle? _objectHookHandle;
    private readonly UnhookWinEventSafeHandle? _systemHookHandle;
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

        _objectHookHandle = PInvoke.SetWinEventHook(
            EVENT_OBJECT_CREATE,
            EVENT_OBJECT_LOCATIONCHANGE,
            null, _callback, 0, 0,
            WINEVENT_OUTOFCONTEXT);

        if (_objectHookHandle == null || _objectHookHandle.IsInvalid)
            throw new WindowManagementException(
                "Failed to install object event hook (CREATE/DESTROY/LOCATIONCHANGE). Window events will not be available.");

        try
        {
            _systemHookHandle = PInvoke.SetWinEventHook(
                EVENT_SYSTEM_MINIMIZESTART,
                EVENT_SYSTEM_MINIMIZEEND,
                null, _callback, 0, 0,
                WINEVENT_OUTOFCONTEXT);
        }
        catch
        {
            _objectHookHandle.Dispose();
            throw;
        }

        if (_systemHookHandle == null || _systemHookHandle.IsInvalid)
        {
            _objectHookHandle.Dispose();
            throw new WindowManagementException(
                "Failed to install system event hook (MINIMIZE_START/END). State change events will not be available.");
        }
    }

    private void OnWinEvent(HWINEVENTHOOK hWinEventHook, uint @event, HWND hwnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime)
    {
        // Only handle window-level events (not child objects)
        if (idObject != 0 || hwnd.IsNull)
            return;

        var handle = (nint)hwnd;

        try
        {
            switch (@event)
            {
                case EVENT_OBJECT_CREATE:
                    EmitCreated(handle);
                    break;
                case EVENT_OBJECT_DESTROY:
                    EmitDestroyed(handle);
                    break;
                case EVENT_OBJECT_LOCATIONCHANGE:
                    EmitLocationChange(handle);
                    break;
                case EVENT_SYSTEM_MINIMIZESTART:
                case EVENT_SYSTEM_MINIMIZEEND:
                    EmitStateChange(handle);
                    break;
            }
        }
        catch (WindowNotFoundException)
        {
            // Window was destroyed during event processing — expected race condition
        }
        catch (WindowManagementException ex)
        {
            Trace.TraceWarning(
                $"Error processing event for window 0x{handle:X}: {ex.Message}");
        }
        catch (Exception ex)
        {
            Trace.TraceError(
                $"Unexpected error in Win32 event callback for window 0x{handle:X}: {ex}");
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
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or WindowManagementException)
        {
            // ArgumentException: process exited between getting PID and looking it up
            // InvalidOperationException: process object cannot be associated with a running process
            // WindowManagementException: window handle became invalid (race condition)
            return string.Empty;
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        _objectHookHandle?.Dispose();
        _systemHookHandle?.Dispose();
        _created.Dispose();
        _destroyed.Dispose();
        _moved.Dispose();
        _resized.Dispose();
        _stateChanged.Dispose();
    }
}
