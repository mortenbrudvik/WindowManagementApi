# WindowManagementApi Design

## Overview

A general-purpose .NET 10 library for programmatic Windows window management with first-class multi-monitor, mixed-DPI support. Built on CsWin32 (source-generated P/Invoke) with R3 for reactive events.

## Core Principles

- Two-layer API: high-level (monitor-relative operations) + low-level (raw Win32 with correct DPI math)
- Strict per-monitor v2 DPI awareness — throws on initialization if the process isn't configured correctly
- Default window enumeration matches Alt+Tab semantics; customizable via fluent filters
- Real-time window events via R3 observables
- Serializable window state models for easy consumer-side layout persistence
- CsWin32 for P/Invoke — zero hand-written Win32 signatures

## Dependencies

- `Microsoft.Windows.CsWin32` (build-time source generator)
- `R3` (reactive events)
- `Microsoft.Extensions.Logging.Abstractions` (optional logging)
- `Autofac` (native module support alongside `IServiceCollection`)

## Target Framework

`net10.0-windows`

## API Surface

### High-Level API

```csharp
namespace WindowManagement;

public interface IWindowManager : IAsyncDisposable
{
    IMonitorService Monitors { get; }

    // Discovery
    IReadOnlyList<IWindow> GetAll(WindowFilter? filter = null);
    IReadOnlyList<IWindow> GetAll(Action<WindowFilterBuilder> configure);
    IWindow? GetForeground();

    // Manipulation
    Task MoveAsync(IWindow window, int x, int y);
    Task ResizeAsync(IWindow window, int width, int height);
    Task SetBoundsAsync(IWindow window, WindowRect bounds);
    Task MoveToMonitorAsync(IWindow window, IMonitor monitor);
    Task SetStateAsync(IWindow window, WindowState state);
    Task FocusAsync(IWindow window);
    Task SnapAsync(IWindow window, IMonitor monitor, SnapPosition position);

    // Events
    Observable<WindowEventArgs> Created { get; }
    Observable<WindowEventArgs> Destroyed { get; }
    Observable<WindowMovedEventArgs> Moved { get; }
    Observable<WindowMovedEventArgs> Resized { get; }
    Observable<WindowStateEventArgs> StateChanged { get; }
}

public interface IMonitorService
{
    IReadOnlyList<IMonitor> All { get; }
    IMonitor Primary { get; }
    IMonitor GetFor(IWindow window);
    IMonitor? GetAt(int x, int y);

    Observable<MonitorEventArgs> Connected { get; }
    Observable<MonitorEventArgs> Disconnected { get; }
    Observable<MonitorEventArgs> SettingsChanged { get; }
}
```

### Supporting Types

```csharp
public interface IWindow
{
    nint Handle { get; }
    string Title { get; }
    string ProcessName { get; }
    int ProcessId { get; }
    string ClassName { get; }
    WindowRect Bounds { get; }
    WindowState State { get; }
    IMonitor Monitor { get; }
    bool IsTopmost { get; }
}

public interface IMonitor
{
    string DeviceName { get; }
    string DisplayName { get; }
    bool IsPrimary { get; }
    WindowRect Bounds { get; }
    WindowRect WorkArea { get; }
    int Dpi { get; }
    double ScaleFactor { get; }
}

public record WindowRect(int X, int Y, int Width, int Height);

public enum WindowState { Normal, Minimized, Maximized }

public enum SnapPosition
{
    Left, Right, Top, Bottom,
    TopLeft, TopRight, BottomLeft, BottomRight,
    Fill
}
```

### Window Filtering

Default `GetAll()` returns the Alt+Tab window set:
- Visible top-level windows only
- Excludes `WS_EX_TOOLWINDOW` unless `WS_EX_APPWINDOW`
- Excludes cloaked windows (DWM attribute check)
- Excludes windows with empty titles
- Excludes owned windows unless they have `WS_EX_APPWINDOW`

Fluent builder for customization:

```csharp
// Default: Alt+Tab window set
var windows = manager.GetAll();

// Fluent filtering on top of defaults
var windows = manager.GetAll(f => f
    .WithProcess("notepad")
    .WithTitle("*.txt")
    .ExcludeMinimized());

// Opt out of Alt+Tab filtering for raw enumeration
var windows = manager.GetAll(f => f.Unfiltered());

// Custom predicate escape hatch
var windows = manager.GetAll(f => f
    .Where(w => w.Bounds.Width > 500));
```

### Low-Level API

```csharp
namespace WindowManagement.LowLevel;

public interface IWindowApi
{
    nint GetForeground();
    IReadOnlyList<nint> Enumerate(bool altTabOnly = true);

    string GetTitle(nint hwnd);
    string GetClassName(nint hwnd);
    int GetProcessId(nint hwnd);
    WindowRect GetBounds(nint hwnd);
    WindowRect GetClientBounds(nint hwnd);
    WindowState GetState(nint hwnd);
    bool IsVisible(nint hwnd);
    bool IsTopmost(nint hwnd);

    void Move(nint hwnd, int x, int y);
    void Resize(nint hwnd, int width, int height);
    void SetBounds(nint hwnd, WindowRect bounds);
    void SetState(nint hwnd, WindowState state);
    void Focus(nint hwnd);
    void SetTopmost(nint hwnd, bool topmost);
}

public interface IDisplayApi
{
    IReadOnlyList<DisplayInfo> GetAll();
    DisplayInfo GetPrimary();
    DisplayInfo GetForWindow(nint hwnd);
    DisplayInfo? GetAtPoint(int x, int y);
    int GetDpi(nint hmonitor);
    double GetScaleFactor(nint hmonitor);
    WindowRect LogicalToPhysical(WindowRect rect, nint hmonitor);
    WindowRect PhysicalToLogical(WindowRect rect, nint hmonitor);
}

public record DisplayInfo(
    nint Handle, string DeviceName, string DisplayName,
    bool IsPrimary, WindowRect Bounds, WindowRect WorkArea,
    int Dpi, double ScaleFactor);
```

## DI Registration

```csharp
// Microsoft.Extensions.DependencyInjection
services.AddWindowManagement();

// Autofac native
builder.RegisterModule(new WindowManagementModule());

// Both support options
builder.RegisterModule(new WindowManagementModule(options =>
{
    options.EnforceDpiAwareness = true;
}));

// Standalone (without DI)
await using var manager = WindowManagementFactory.Create();
```

```csharp
public class WindowManagementOptions
{
    public bool EnforceDpiAwareness { get; set; } = true;
}
```

DI registration registers all four interfaces: `IWindowManager`, `IMonitorService`, `IWindowApi`, `IDisplayApi`.

## Error Handling & Edge Cases

### DPI Enforcement

Throws `DpiAwarenessException` (subclass of `WindowManagementException`) on construction if the process isn't configured for per-monitor v2 DPI awareness.

### Stale Window Handles

Windows can close between enumeration and manipulation. All manipulation methods throw `WindowNotFoundException` if the handle is no longer valid.

### Cross-Monitor DPI Compensation

When moving a window between monitors with different DPI, the library automatically applies the DPI ratio correction (`windowDpi / targetDpi`) so the window lands at the correct size. This follows the proven pattern from the Snap app.

### Invisible Border Compensation

Windows 10/11 has invisible borders (typically 7px left/right/bottom). `SnapAsync` and `SetBoundsAsync` compensate for these using `DwmGetWindowAttribute(DWMWA_EXTENDED_FRAME_BOUNDS)`, with a fallback to default values (7,0,7,7) when the API returns mismatched coordinate systems.

### Non-Resizable Windows

`SnapAsync` detects fixed-size windows (no `WS_THICKFRAME` style) and centers them within the zone instead of stretching.

### Minimized Window Restore

`SnapAsync` and `FocusAsync` handle minimized windows with retry logic (5 attempts, 100ms apart) following the PowerToys pattern. Uses `AttachThreadInput` trick to restore without activation (prevents taskbar flashing for Electron apps).

## Project Structure

```
WindowManagementApi/
├── src/
│   └── WindowManagement/
│       ├── WindowManagement.csproj
│       ├── IWindowManager.cs
│       ├── IMonitorService.cs
│       ├── Models/
│       │   ├── WindowRect.cs
│       │   ├── WindowState.cs
│       │   ├── SnapPosition.cs
│       │   └── EventArgs/
│       ├── Filtering/
│       │   ├── WindowFilter.cs
│       │   └── WindowFilterBuilder.cs
│       ├── LowLevel/
│       │   ├── IWindowApi.cs
│       │   ├── IDisplayApi.cs
│       │   └── Internal/          <- CsWin32 generated code lives here
│       ├── DependencyInjection/
│       │   ├── ServiceCollectionExtensions.cs
│       │   ├── WindowManagementModule.cs    <- Autofac
│       │   └── WindowManagementOptions.cs
│       └── NativeMethods.txt       <- CsWin32 API list
├── tests/
│   ├── WindowManagement.Tests/
│   └── WindowManagement.IntegrationTests/
├── examples/
│   └── WindowManagement.ExampleApp/
└── docs/
```

Single NuGet package `WindowManagement`. Follows the same structure as VirtualDisplayDriverApi.

## Coordinate System

All coordinates in the public API are in **physical pixels** (not logical/DPI-scaled), since the library enforces per-monitor v2 DPI awareness. The low-level `IDisplayApi` provides `LogicalToPhysical` and `PhysicalToLogical` conversion methods for consumers that need to interop with systems using logical coordinates.
