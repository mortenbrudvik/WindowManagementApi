# Window Management API

A general-purpose .NET 10 library for programmatic Windows window management with first-class multi-monitor, mixed-DPI support.

## Prerequisites

- Windows 10 or 11
- .NET 10.0 or later
- Process must be configured for per-monitor v2 DPI awareness

## Quick Start

### Standalone usage

```csharp
using WindowManagement;

await using var manager = WindowManagementFactory.Create();

// List all Alt+Tab windows
var windows = manager.GetAll();
foreach (var w in windows)
    Console.WriteLine($"{w.ProcessName}: {w.Title} ({w.Bounds})");

// Snap the foreground window to the left half of the primary monitor
var foreground = manager.GetForeground();
if (foreground != null)
    await manager.SnapAsync(foreground, manager.Monitors.Primary, SnapPosition.Left);
```

### With dependency injection

```csharp
using WindowManagement.DependencyInjection;

services.AddWindowManagement();
```

### With Autofac

```csharp
using WindowManagement.DependencyInjection;

builder.RegisterModule(new WindowManagementModule());
```

Then inject `IWindowManager`:

```csharp
public class MyService(IWindowManager windowManager)
{
    public async Task SnapToLeft()
    {
        var foreground = windowManager.GetForeground();
        if (foreground != null)
            await windowManager.SnapAsync(foreground, windowManager.Monitors.Primary, SnapPosition.Left);
    }
}
```

## Window Filtering

```csharp
// Default: Alt+Tab window set
var windows = manager.GetAll();

// Filter by process
var windows = manager.GetAll(f => f.WithProcess("notepad"));

// Filter by title with wildcards
var windows = manager.GetAll(f => f.WithTitle("*.txt*"));

// Exclude minimized windows
var windows = manager.GetAll(f => f.ExcludeMinimized());

// Raw enumeration (no Alt+Tab filtering)
var windows = manager.GetAll(f => f.Unfiltered());

// Custom predicate
var windows = manager.GetAll(f => f.Where(w => w.Bounds.Width > 500));
```

## Window Events (R3)

```csharp
manager.Created.Subscribe(e => Console.WriteLine($"Created: {e.Title}"));
manager.Moved.Subscribe(e => Console.WriteLine($"Moved: {e.Title} -> {e.NewBounds}"));
manager.Monitors.Connected.Subscribe(e => Console.WriteLine($"Monitor connected: {e.DeviceName}"));
```

## Low-Level API

For advanced consumers who need direct Win32 access with correct DPI math:

```csharp
// Inject or resolve
IWindowApi windowApi = ...;
IDisplayApi displayApi = ...;

var handles = windowApi.Enumerate(altTabOnly: true);
var bounds = windowApi.GetBounds(handle);
windowApi.SetBounds(handle, new WindowRect(0, 0, 800, 600));

var monitors = displayApi.GetAll();
var logical = displayApi.PhysicalToLogical(bounds, monitor.Handle);
```
