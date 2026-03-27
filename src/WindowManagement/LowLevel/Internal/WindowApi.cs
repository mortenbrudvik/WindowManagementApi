using System.Diagnostics;
using System.Runtime.InteropServices;
using WindowManagement.Exceptions;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WindowManagement.LowLevel.Internal;

internal class WindowApi : IWindowApi
{
    private const int MaxRestoreRetries = 5;
    private const int RestoreRetryDelayMs = 100;
    // Maximum plausible invisible border in pixels; values beyond this indicate a DWM query error
    private const int MaxReasonableBorder = 15;
    // Fallback invisible borders when DWM queries fail; matches typical Windows 10/11 DWM-composed windows (7px left/right/bottom, 0 top)
    private static readonly (int left, int top, int right, int bottom) DefaultInvisibleBorders = (7, 0, 7, 7);
    private static readonly HWND HWND_TOPMOST = new(-1);
    private static readonly HWND HWND_NOTOPMOST = new(-2);

    public nint GetForeground()
    {
        return PInvoke.GetForegroundWindow();
    }

    public IReadOnlyList<nint> Enumerate(bool altTabOnly = true)
    {
        var handles = new List<nint>();

        var success = PInvoke.EnumWindows((hwnd, _) =>
        {
            if (altTabOnly && !IsAltTabWindow(hwnd))
                return true;
            else if (!altTabOnly && !PInvoke.IsWindowVisible(hwnd))
                return true;

            handles.Add(hwnd);
            return true;
        }, 0);

        if (!success)
        {
            if (handles.Count == 0)
                throw new WindowManagementException(
                    $"EnumWindows failed. Win32 error: {Marshal.GetLastWin32Error()}");
            Trace.TraceWarning(
                $"EnumWindows returned failure after enumerating {handles.Count} windows. Results may be incomplete.");
        }

        return handles;
    }

    public string GetTitle(nint hwnd)
    {
        var length = PInvoke.GetWindowTextLength(new HWND(hwnd));
        if (length == 0) return string.Empty;

        unsafe
        {
            var buffer = stackalloc char[length + 1];
            PInvoke.GetWindowText(new HWND(hwnd), buffer, length + 1);
            return new string(buffer);
        }
    }

    public string GetClassName(nint hwnd)
    {
        unsafe
        {
            var buffer = stackalloc char[256];
            var length = PInvoke.GetClassName(new HWND(hwnd), buffer, 256);
            return length > 0 ? new string(buffer, 0, length) : string.Empty;
        }
    }

    public int GetProcessId(nint hwnd)
    {
        unsafe
        {
            uint pid;
            var threadId = PInvoke.GetWindowThreadProcessId(new HWND(hwnd), &pid);
            if (threadId == 0)
                throw new WindowNotFoundException(hwnd);
            return (int)pid;
        }
    }

    public WindowRect GetBounds(nint hwnd)
    {
        if (!PInvoke.GetWindowRect(new HWND(hwnd), out var rect))
            throw new WindowManagementException(
                $"GetWindowRect failed for window 0x{hwnd:X}. Win32 error: {Marshal.GetLastWin32Error()}");
        return new WindowRect(rect.left, rect.top, Math.Max(0, rect.right - rect.left), Math.Max(0, rect.bottom - rect.top));
    }

    public WindowRect GetClientBounds(nint hwnd)
    {
        if (!PInvoke.GetClientRect(new HWND(hwnd), out var rect))
            throw new WindowManagementException(
                $"GetClientRect failed for window 0x{hwnd:X}. Win32 error: {Marshal.GetLastWin32Error()}");
        return new WindowRect(rect.left, rect.top, Math.Max(0, rect.right - rect.left), Math.Max(0, rect.bottom - rect.top));
    }

    public WindowState GetState(nint hwnd)
    {
        if (PInvoke.IsIconic(new HWND(hwnd))) return WindowState.Minimized;
        if (PInvoke.IsZoomed(new HWND(hwnd))) return WindowState.Maximized;
        return WindowState.Normal;
    }

    public bool IsVisible(nint hwnd) => PInvoke.IsWindowVisible(new HWND(hwnd));

    public bool IsTopmost(nint hwnd)
    {
        var exStyle = (WINDOW_EX_STYLE)(nint)PInvoke.GetWindowLong(new HWND(hwnd), WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        return exStyle.HasFlag(WINDOW_EX_STYLE.WS_EX_TOPMOST);
    }

    public bool IsValid(nint hwnd) => PInvoke.IsWindow(new HWND(hwnd));

    public bool IsResizable(nint hwnd)
    {
        var style = (WINDOW_STYLE)(nint)PInvoke.GetWindowLong(new HWND(hwnd), WINDOW_LONG_PTR_INDEX.GWL_STYLE);
        return style.HasFlag(WINDOW_STYLE.WS_THICKFRAME);
    }

    public (int left, int top, int right, int bottom) GetInvisibleBorders(nint hwnd)
    {
        if (!PInvoke.GetWindowRect(new HWND(hwnd), out var windowRect))
        {
            Trace.TraceWarning(
                $"GetWindowRect failed in GetInvisibleBorders for 0x{hwnd:X}; using default borders.");
            return DefaultInvisibleBorders;
        }

        RECT frameRect;
        unsafe
        {
            var hr = PInvoke.DwmGetWindowAttribute(new HWND(hwnd),
                Windows.Win32.Graphics.Dwm.DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS,
                &frameRect,
                (uint)sizeof(RECT));

            if (hr.Failed)
            {
                Trace.TraceWarning(
                    $"DwmGetWindowAttribute failed for 0x{hwnd:X} (HRESULT: 0x{hr.Value:X8}); using default borders.");
                return DefaultInvisibleBorders;
            }
        }

        int left = frameRect.left - windowRect.left;
        int top = frameRect.top - windowRect.top;
        int right = windowRect.right - frameRect.right;
        int bottom = windowRect.bottom - frameRect.bottom;

        if (left < 0 || left > MaxReasonableBorder ||
            top < 0 || top > MaxReasonableBorder ||
            right < 0 || right > MaxReasonableBorder ||
            bottom < 0 || bottom > MaxReasonableBorder)
        {
            Trace.TraceWarning(
                $"Computed invisible borders for 0x{hwnd:X} are out of range " +
                $"(L={left}, T={top}, R={right}, B={bottom}); using defaults.");
            return DefaultInvisibleBorders;
        }

        return (left, top, right, bottom);
    }

    public uint GetDpi(nint hwnd)
    {
        var dpi = PInvoke.GetDpiForWindow(new HWND(hwnd));
        if (dpi == 0)
            throw new WindowManagementException(
                $"GetDpiForWindow returned 0 for window 0x{hwnd:X}. The window handle may be invalid.");
        return dpi;
    }

    public void Move(nint hwnd, int x, int y)
    {
        ThrowIfInvalid(hwnd);
        if (!PInvoke.SetWindowPos(new HWND(hwnd), HWND.Null, x, y, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER))
            throw new WindowManagementException(
                $"SetWindowPos (Move) failed for window 0x{hwnd:X}. Win32 error: {Marshal.GetLastWin32Error()}");
    }

    public void Resize(nint hwnd, int width, int height)
    {
        ThrowIfInvalid(hwnd);
        if (!PInvoke.SetWindowPos(new HWND(hwnd), HWND.Null, 0, 0, width, height,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER))
            throw new WindowManagementException(
                $"SetWindowPos (Resize) failed for window 0x{hwnd:X}. Win32 error: {Marshal.GetLastWin32Error()}");
    }

    public void SetBounds(nint hwnd, WindowRect bounds)
    {
        ThrowIfInvalid(hwnd);
        if (!PInvoke.SetWindowPos(new HWND(hwnd), HWND.Null, bounds.X, bounds.Y, bounds.Width, bounds.Height,
            SET_WINDOW_POS_FLAGS.SWP_NOZORDER))
            throw new WindowManagementException(
                $"SetWindowPos (SetBounds) failed for window 0x{hwnd:X}. Win32 error: {Marshal.GetLastWin32Error()}");
    }

    public void SetState(nint hwnd, WindowState state)
    {
        ThrowIfInvalid(hwnd);
        var cmd = state switch
        {
            WindowState.Normal => SHOW_WINDOW_CMD.SW_RESTORE,
            WindowState.Minimized => SHOW_WINDOW_CMD.SW_MINIMIZE,
            WindowState.Maximized => SHOW_WINDOW_CMD.SW_MAXIMIZE,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
        PInvoke.ShowWindow(new HWND(hwnd), cmd);
    }

    public void Focus(nint hwnd)
    {
        ThrowIfInvalid(hwnd);

        if (PInvoke.IsIconic(new HWND(hwnd)))
        {
            if (!RestoreMinimized(hwnd))
                throw new WindowManagementException(
                    $"Failed to restore minimized window 0x{hwnd:X} after {MaxRestoreRetries} attempts.");
        }

        if (!PInvoke.SetForegroundWindow(new HWND(hwnd)))
            throw new WindowManagementException(
                $"SetForegroundWindow failed for window 0x{hwnd:X}. " +
                "The calling process may not have foreground activation rights.");
    }

    public void SetTopmost(nint hwnd, bool topmost)
    {
        ThrowIfInvalid(hwnd);
        var insertAfter = topmost ? HWND_TOPMOST : HWND_NOTOPMOST;
        if (!PInvoke.SetWindowPos(new HWND(hwnd), insertAfter, 0, 0, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE))
            throw new WindowManagementException(
                $"SetWindowPos (SetTopmost) failed for window 0x{hwnd:X}. Win32 error: {Marshal.GetLastWin32Error()}");
    }

    public bool RestoreMinimized(nint hwnd)
    {
        if (!PInvoke.IsIconic(new HWND(hwnd)))
            return true;

        var foreground = PInvoke.GetForegroundWindow();
        uint foregroundThread, targetThread;
        unsafe
        {
            foregroundThread = PInvoke.GetWindowThreadProcessId(foreground, (uint*)null);
            targetThread = PInvoke.GetWindowThreadProcessId(new HWND(hwnd), (uint*)null);
        }
        var currentThread = PInvoke.GetCurrentThreadId();

        bool attachedForeground = false, attachedTarget = false;

        try
        {
            if (foregroundThread != currentThread)
                attachedForeground = PInvoke.AttachThreadInput(currentThread, foregroundThread, true);
            if (targetThread != currentThread && targetThread != foregroundThread)
                attachedTarget = PInvoke.AttachThreadInput(currentThread, targetThread, true);

            PInvoke.ShowWindow(new HWND(hwnd), SHOW_WINDOW_CMD.SW_RESTORE);

            for (int i = 0; i < MaxRestoreRetries; i++)
            {
                if (!PInvoke.IsIconic(new HWND(hwnd)))
                    return true;
                Thread.Sleep(RestoreRetryDelayMs);
            }

            return !PInvoke.IsIconic(new HWND(hwnd));
        }
        finally
        {
            if (attachedTarget)
                PInvoke.AttachThreadInput(currentThread, targetThread, false);
            if (attachedForeground)
                PInvoke.AttachThreadInput(currentThread, foregroundThread, false);
        }
    }

    public void BringToFront(nint hwnd)
    {
        ThrowIfInvalid(hwnd);
        // Topmost-then-not-topmost trick: brings window to front without permanently pinning it
        var flags = SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE;
        if (!PInvoke.SetWindowPos(new HWND(hwnd), HWND_TOPMOST, 0, 0, 0, 0, flags))
            throw new WindowManagementException(
                $"SetWindowPos (BringToFront) failed for window 0x{hwnd:X}. Win32 error: {Marshal.GetLastWin32Error()}");
        if (!PInvoke.SetWindowPos(new HWND(hwnd), HWND_NOTOPMOST, 0, 0, 0, 0, flags))
            throw new WindowManagementException(
                $"SetWindowPos (BringToFront, remove topmost) failed for window 0x{hwnd:X}. " +
                $"The window may be stuck as topmost. Win32 error: {Marshal.GetLastWin32Error()}");
    }

    private void ThrowIfInvalid(nint hwnd)
    {
        if (!PInvoke.IsWindow(new HWND(hwnd)))
            throw new WindowNotFoundException(hwnd);
    }

    private bool IsAltTabWindow(HWND hwnd)
    {
        if (!PInvoke.IsWindowVisible(hwnd))
            return false;

        if (PInvoke.GetWindowTextLength(hwnd) == 0)
            return false;

        var exStyle = (WINDOW_EX_STYLE)(nint)PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);

        if (exStyle.HasFlag(WINDOW_EX_STYLE.WS_EX_TOOLWINDOW) && !exStyle.HasFlag(WINDOW_EX_STYLE.WS_EX_APPWINDOW))
            return false;

        var owner = PInvoke.GetWindow(hwnd, GET_WINDOW_CMD.GW_OWNER);
        if (owner != HWND.Null && !exStyle.HasFlag(WINDOW_EX_STYLE.WS_EX_APPWINDOW))
            return false;

        int cloaked;
        unsafe
        {
            var hr = PInvoke.DwmGetWindowAttribute(hwnd,
                Windows.Win32.Graphics.Dwm.DWMWINDOWATTRIBUTE.DWMWA_CLOAKED,
                &cloaked,
                (uint)sizeof(int));
            if (hr.Succeeded && cloaked != 0)
                return false;
        }

        return true;
    }
}
