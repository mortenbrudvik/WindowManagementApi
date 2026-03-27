using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WindowManagement.LowLevel.Internal;

internal class WindowApi : IWindowApi
{
    private const int MaxRestoreRetries = 5;
    private const int RestoreRetryDelayMs = 100;
    private const int MaxReasonableBorder = 15;

    public nint GetForeground()
    {
        return PInvoke.GetForegroundWindow();
    }

    public IReadOnlyList<nint> Enumerate(bool altTabOnly = true)
    {
        var handles = new List<nint>();

        PInvoke.EnumWindows((hwnd, _) =>
        {
            if (altTabOnly && !IsAltTabWindow(hwnd))
                return true;
            else if (!altTabOnly && !PInvoke.IsWindowVisible(hwnd))
                return true;

            handles.Add(hwnd);
            return true;
        }, 0);

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
            PInvoke.GetWindowThreadProcessId(new HWND(hwnd), &pid);
            return (int)pid;
        }
    }

    public WindowRect GetBounds(nint hwnd)
    {
        PInvoke.GetWindowRect(new HWND(hwnd), out var rect);
        return new WindowRect(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
    }

    public WindowRect GetClientBounds(nint hwnd)
    {
        PInvoke.GetClientRect(new HWND(hwnd), out var rect);
        return new WindowRect(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
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
        PInvoke.GetWindowRect(new HWND(hwnd), out var windowRect);

        RECT frameRect;
        unsafe
        {
            var hr = PInvoke.DwmGetWindowAttribute(new HWND(hwnd),
                Windows.Win32.Graphics.Dwm.DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS,
                &frameRect,
                (uint)sizeof(RECT));

            if (hr.Failed)
                return (0, 0, 0, 0);
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
            return (7, 0, 7, 7);
        }

        return (left, top, right, bottom);
    }

    public uint GetDpi(nint hwnd)
    {
        return PInvoke.GetDpiForWindow(new HWND(hwnd));
    }

    public void Move(nint hwnd, int x, int y)
    {
        ThrowIfInvalid(hwnd);
        PInvoke.SetWindowPos(new HWND(hwnd), HWND.Null, x, y, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER);
    }

    public void Resize(nint hwnd, int width, int height)
    {
        ThrowIfInvalid(hwnd);
        PInvoke.SetWindowPos(new HWND(hwnd), HWND.Null, 0, 0, width, height,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER);
    }

    public void SetBounds(nint hwnd, WindowRect bounds)
    {
        ThrowIfInvalid(hwnd);
        PInvoke.SetWindowPos(new HWND(hwnd), HWND.Null, bounds.X, bounds.Y, bounds.Width, bounds.Height,
            SET_WINDOW_POS_FLAGS.SWP_NOZORDER);
    }

    public void SetState(nint hwnd, WindowState state)
    {
        ThrowIfInvalid(hwnd);
        var cmd = state switch
        {
            WindowState.Normal => SHOW_WINDOW_CMD.SW_RESTORE,
            WindowState.Minimized => SHOW_WINDOW_CMD.SW_MINIMIZE,
            WindowState.Maximized => SHOW_WINDOW_CMD.SW_MAXIMIZE,
            _ => SHOW_WINDOW_CMD.SW_RESTORE
        };
        PInvoke.ShowWindow(new HWND(hwnd), cmd);
    }

    public void Focus(nint hwnd)
    {
        ThrowIfInvalid(hwnd);

        if (PInvoke.IsIconic(new HWND(hwnd)))
            RestoreMinimized(hwnd);

        PInvoke.SetForegroundWindow(new HWND(hwnd));
    }

    public void SetTopmost(nint hwnd, bool topmost)
    {
        ThrowIfInvalid(hwnd);
        var insertAfter = topmost ? new HWND(-1) : new HWND(-2);
        PInvoke.SetWindowPos(new HWND(hwnd), insertAfter, 0, 0, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE);
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
        PInvoke.SetWindowPos(new HWND(hwnd), new HWND(-1), 0, 0, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
        PInvoke.SetWindowPos(new HWND(hwnd), new HWND(-2), 0, 0, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
    }

    private void ThrowIfInvalid(nint hwnd)
    {
        if (!PInvoke.IsWindow(new HWND(hwnd)))
            throw new Exceptions.WindowNotFoundException(hwnd);
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
