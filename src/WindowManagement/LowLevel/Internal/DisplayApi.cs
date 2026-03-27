using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.HiDpi;

namespace WindowManagement.LowLevel.Internal;

internal class DisplayApi : IDisplayApi
{
    public IReadOnlyList<DisplayInfo> GetAll()
    {
        var monitors = new List<DisplayInfo>();

        unsafe
        {
            PInvoke.EnumDisplayMonitors(HDC.Null, (RECT?)null, (hMonitor, _, _, _) =>
            {
                var info = GetDisplayInfo(hMonitor);
                if (info != null)
                    monitors.Add(info);
                return true;
            }, 0);
        }

        return monitors;
    }

    public DisplayInfo GetPrimary()
    {
        return GetAll().First(m => m.IsPrimary);
    }

    public DisplayInfo GetForWindow(nint hwnd)
    {
        var hMonitor = PInvoke.MonitorFromWindow(new HWND(hwnd), MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
        return GetDisplayInfo(hMonitor) ?? GetPrimary();
    }

    public DisplayInfo? GetAtPoint(int x, int y)
    {
        var point = new System.Drawing.Point(x, y);
        var hMonitor = PInvoke.MonitorFromPoint(point, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONULL);
        if (hMonitor.IsNull)
            return null;
        return GetDisplayInfo(hMonitor);
    }

    public int GetDpi(nint hmonitor)
    {
        var hr = PInvoke.GetDpiForMonitor(
            new HMONITOR(hmonitor),
            MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI,
            out var dpiX,
            out _);
        return hr.Succeeded ? (int)dpiX : 96;
    }

    public double GetScaleFactor(nint hmonitor)
    {
        return GetDpi(hmonitor) / 96.0;
    }

    public WindowRect LogicalToPhysical(WindowRect rect, nint hmonitor)
    {
        var scale = GetScaleFactor(hmonitor);
        return new WindowRect(
            (int)(rect.X * scale),
            (int)(rect.Y * scale),
            (int)(rect.Width * scale),
            (int)(rect.Height * scale));
    }

    public WindowRect PhysicalToLogical(WindowRect rect, nint hmonitor)
    {
        var scale = GetScaleFactor(hmonitor);
        return new WindowRect(
            (int)(rect.X / scale),
            (int)(rect.Y / scale),
            (int)(rect.Width / scale),
            (int)(rect.Height / scale));
    }

    public bool IsPerMonitorV2Aware()
    {
        var context = PInvoke.GetThreadDpiAwarenessContext();
        // DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 has well-known value -4
        var perMonitorV2 = new DPI_AWARENESS_CONTEXT((nint)(-4));
        return PInvoke.AreDpiAwarenessContextsEqual(context, perMonitorV2);
    }

    private static DisplayInfo? GetDisplayInfo(HMONITOR hMonitor)
    {
        var monitorInfo = new MONITORINFOEXW();
        monitorInfo.monitorInfo.cbSize = (uint)Marshal.SizeOf<MONITORINFOEXW>();

        if (!PInvoke.GetMonitorInfo(hMonitor, ref monitorInfo.monitorInfo))
            return null;

        var bounds = monitorInfo.monitorInfo.rcMonitor;
        var workArea = monitorInfo.monitorInfo.rcWork;
        var isPrimary = (monitorInfo.monitorInfo.dwFlags & 1) != 0; // MONITORINFOF_PRIMARY

        var deviceName = monitorInfo.szDevice.ToString();

        var hr = PInvoke.GetDpiForMonitor(hMonitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out var dpiX, out _);
        var dpi = hr.Succeeded ? (int)dpiX : 96;

        return new DisplayInfo(
            Handle: hMonitor,
            DeviceName: deviceName,
            DisplayName: deviceName,
            IsPrimary: isPrimary,
            Bounds: new WindowRect(bounds.left, bounds.top, bounds.right - bounds.left, bounds.bottom - bounds.top),
            WorkArea: new WindowRect(workArea.left, workArea.top, workArea.right - workArea.left, workArea.bottom - workArea.top),
            Dpi: dpi,
            ScaleFactor: dpi / 96.0);
    }
}
