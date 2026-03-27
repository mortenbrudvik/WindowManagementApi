# WindowManager.Demo — Example WPF App Design

## Overview

A polished WPF utility app (mini FancyZones) that showcases the WindowManagementApi library. Lives as a standalone solution in `examples/WindowManager.Demo/` with a project reference to the library source.

## Goals

- Demonstrate all major library features in a real, usable tool
- Follow WPF template standards: WPF-UI, Autofac, CommunityToolkit.Mvvm, R3
- Serve as both a developer reference and a genuinely useful window management utility

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Runtime | .NET 10 (`net10.0-windows10.0.26100.0`) |
| UI Framework | WPF with WPF-UI 4.2+ |
| MVVM | CommunityToolkit.Mvvm 8.4+ |
| DI Container | Autofac 9.0+ |
| Reactive | R3 (via WindowManagementApi) |
| Window Management | WindowManagementApi (project reference) |

## UI Layout

FluentWindow with Mica backdrop and NavigationView sidebar containing 4 tabs.

### Tab 1: Windows

Window list + details panel side by side.

**Left panel — Window list:**
- Scrollable list populated from `IWindowManager.GetAll()`
- Each row: title, process name, state, monitor
- Search box filters the list
- Auto-updates via `IWindowManager.Created` / `Destroyed` observables
- Window count in footer

**Right panel — Details:**
- Shown when a window is selected
- 2-column property grid: Process, Handle, State, PID, Bounds, ClassName, Monitor (with resolution + DPI), Topmost, Resizable

### Tab 2: Monitors

Scaled visual representation of the monitor layout.

- Monitors drawn as rectangles proportional to their resolution, positioned based on `IMonitor.Bounds`
- Primary monitor highlighted
- Selecting a monitor shows details: DeviceName, DisplayName, IsPrimary, Bounds, WorkArea, DPI, ScaleFactor
- Auto-refreshes via `IMonitorService.Connected` / `Disconnected` / `SettingsChanged` observables

### Tab 3: Snap

Interactive snap zone interface — the main showcase feature.

- **Window selector:** Dropdown to pick the target window (from `IWindowManager.GetAll()`)
- **Monitor layout:** Scaled representations of all monitors, sized proportionally
- **Snap zone grid per monitor:** Clickable zones for all 9 `SnapPosition` values:
  - Quadrants: TopLeft, TopRight, BottomLeft, BottomRight (2×2 grid inside each monitor)
  - Halves: Left, Right, Top, Bottom (buttons below each monitor)
  - Fill (button below each monitor)
- **Click a zone** → calls `IWindowManager.SnapAsync(window, monitor, position)`
- **Status bar:** Confirms snap result or shows error

### Tab 4: Events

Live-updating event feed from R3 observables.

- Subscribes to all 8 observables:
  - Window: `Created`, `Destroyed`, `Moved`, `Resized`, `StateChanged`
  - Monitor: `Connected`, `Disconnected`, `SettingsChanged`
- Each event displayed as a timestamped row: Time | Type | Details
- Filter checkboxes to toggle event types on/off
- Auto-scroll toggle (on by default)
- Clear button to reset the log

## Project Structure

```
examples/
└── WindowManager.Demo/
    ├── src/
    │   └── WindowManager.Demo/
    │       ├── App.xaml
    │       ├── App.xaml.cs
    │       ├── MainWindow.xaml
    │       ├── MainWindow.xaml.cs
    │       ├── Views/
    │       │   ├── WindowsPage.xaml
    │       │   ├── MonitorsPage.xaml
    │       │   ├── SnapPage.xaml
    │       │   └── EventsPage.xaml
    │       ├── ViewModels/
    │       │   ├── MainWindowViewModel.cs
    │       │   ├── WindowsViewModel.cs
    │       │   ├── MonitorsViewModel.cs
    │       │   ├── SnapViewModel.cs
    │       │   └── EventsViewModel.cs
    │       ├── Models/
    │       │   ├── WindowItem.cs
    │       │   └── EventEntry.cs
    │       ├── Modules/
    │       │   └── AppModule.cs
    │       └── Services/
    │           └── ApplicationHostService.cs
    ├── Directory.Build.props
    ├── .editorconfig
    ├── WindowManager.Demo.sln
    └── CLAUDE.md
```

## Architecture

### Dependency Injection

- `App.xaml.cs` builds `IHost` with `AutofacServiceProviderFactory`
- `AppModule` (Autofac module) registers:
  - `WindowManagementModule` from the library
  - All ViewModels as transient
  - All Pages as transient
  - `INavigationViewPageProvider` for WPF-UI navigation
- `ApplicationHostService` (IHostedService) resolves and shows `MainWindow`

### Startup Sequence

1. `App.xaml.cs` builds `IHost` with Autofac
2. `AppModule` registers `WindowManagementModule(configure: o => o.EnforceDpiAwareness = true)`
3. `ApplicationHostService` resolves `MainWindow` and shows it
4. `NavigationView` navigates to `WindowsPage` by default

### Data Flow

**Windows tab:**
- `WindowsViewModel` calls `IWindowManager.GetAll()` on load → `ObservableCollection<WindowItem>`
- Selecting a window updates `SelectedWindow` → details panel binds to it
- `Created`/`Destroyed` observables auto-add/remove from the list
- Refresh button re-fetches the full list

**Snap tab:**
- `SnapViewModel` exposes window list for dropdown (from `IWindowManager.GetAll()`)
- Monitor layout from `IMonitorService.All`, scaled proportionally
- Click zone → `IWindowManager.SnapAsync(window, monitor, position)`
- Status message confirms result or shows error

**Monitors tab:**
- `MonitorsViewModel` reads `IMonitorService.All` and `Primary`
- Renders monitors on a Canvas/ItemsControl with absolute positioning
- `Connected`/`Disconnected`/`SettingsChanged` trigger refresh

**Events tab:**
- `EventsViewModel` subscribes to all 8 observables on init
- Each event prepended to `ObservableCollection<EventEntry>`
- Filter toggles visibility by type
- Subscriptions disposed when VM is disposed

### Error Handling

- `WindowNotFoundException`: window closed between selection and action — show inline message, refresh list
- `DpiAwarenessException`: prevented by app manifest setting per-monitor v2 DPI awareness

## Library APIs Demonstrated

| API | Where Used |
|-----|-----------|
| `WindowManagementModule` (Autofac) | `AppModule` registration |
| `IWindowManager.GetAll()` | Windows tab, Snap tab dropdown |
| `IWindowManager.GetForeground()` | Snap tab pre-selects the foreground window in the dropdown |
| `IWindowManager.SnapAsync()` | Snap tab zone clicks |
| `IWindowManager.Created/Destroyed/Moved/Resized/StateChanged` | Windows tab auto-update, Events tab |
| `IMonitorService.All` / `Primary` | Monitors tab, Snap tab layout |
| `IMonitorService.GetFor(window)` | Windows tab monitor column |
| `IMonitorService.Connected/Disconnected/SettingsChanged` | Monitors tab refresh, Events tab |
| `IWindow.*` properties | Windows tab details panel |
| `IMonitor.*` properties | Monitors tab details |
| `SnapPosition` enum | Snap tab zone grid |
| `WindowState` enum | Windows tab state display |
| `WindowRect` record | Windows tab bounds display |
