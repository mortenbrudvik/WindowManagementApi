# Integration Tests Design Spec

## Goal

Add comprehensive integration tests to WindowManagementApi that mimic real-world use cases: moving windows across monitors, snapping, resizing, state changes, and filtering. Tests exercise the full stack against real Win32 APIs on a live desktop.

## TestWindow Utility

A fluent builder wrapping a WinForms `Form` on a dedicated STA thread.

### API

```csharp
public class TestWindow : IDisposable
{
    public static TestWindow Create(Action<Options>? configure = null);

    public nint Handle { get; }
    public string Title { get; }
    public WindowRect Bounds { get; }

    public void SetTitle(string title);
    public void SetPosition(int x, int y);
    public void SetSize(int width, int height);

    public void Dispose();

    public class Options
    {
        public Options WithTitle(string title);
        public Options WithSize(int width, int height);
        public Options WithPosition(int x, int y);
        public Options NonResizable();
    }
}
```

### Behavior

- Creates a real `Form` on a dedicated STA thread with its own message pump (`Application.Run`).
- Defaults: 400x300, titled `"TestWindow"`, positioned at (100, 100).
- `Dispose()` closes the form and joins the thread for deterministic cleanup.
- Mutators (`SetTitle`, `SetPosition`, `SetSize`) use `Form.Invoke` to marshal to the form's thread.
- After creating the form, waits up to 2 seconds for the window handle to become valid (polling with short sleeps), then an additional ~200ms for the window to fully render. Throws `TimeoutException` if the handle never becomes valid.

## Skip Attributes

Three custom xUnit `FactAttribute` subclasses that query monitor state via `DisplayApi` and set `Skip` when conditions aren't met.

| Attribute | Skips when |
|---|---|
| `RequiresMultipleMonitorsFactAttribute` | < 2 monitors detected |
| `RequiresMixedDpiFactAttribute` | All monitors share the same DPI |
| `RequiresMixedResolutionFactAttribute` | All monitors share the same resolution |

Each uses `DisplayApi` directly (low-level, no DPI enforcement). Setting `Skip` on a `FactAttribute` makes xUnit report the test as skipped, not failed.

## File Structure

```
tests/WindowManagement.IntegrationTests/
├── Helpers/
│   ├── TestWindow.cs
│   ├── RequiresMultipleMonitorsFactAttribute.cs
│   ├── RequiresMixedDpiFactAttribute.cs
│   └── RequiresMixedResolutionFactAttribute.cs
├── WindowLifecycleTests.cs
├── MoveAndResizeTests.cs
├── SnapPositionTests.cs
├── WindowStateTests.cs
├── FilteringTests.cs
├── CrossMonitorTests.cs
├── CrossMonitorDpiTests.cs
└── CrossMonitorResolutionTests.cs
```

## Test Scenarios

### WindowLifecycleTests (2 tests)

| Test | Description |
|---|---|
| `Create__WindowAppearsInGetAll` | Create TestWindow, call `GetAll()`, assert it contains the handle |
| `Close__WindowDisappearsFromGetAll` | Create TestWindow, dispose it, call `GetAll()`, assert handle is gone |

### MoveAndResizeTests (3 tests)

| Test | Description |
|---|---|
| `MoveAsync__WindowMovesToNewPosition` | Move to (200, 200), verify bounds X/Y |
| `ResizeAsync__WindowChangesSize` | Resize to 600x400, verify bounds Width/Height |
| `SetBoundsAsync__MovesAndResizesInOneCall` | Set bounds to (300, 300, 500, 350), verify all four values |

### SnapPositionTests (2 tests)

| Test | Description |
|---|---|
| `SnapAsync__AllPositions_WindowFillsExpectedArea` | `[Theory]` with all 9 snap positions + Fill. Snap and verify bounds are within the monitor's work area and roughly match expected proportions (half-width for Left/Right, half-height for Top/Bottom, quarter for corners, full for Fill) |
| `SnapAsync__NonResizableWindow_CentersInSnapZone` | Create a non-resizable TestWindow, snap it, verify it's centered within the expected snap zone rather than stretched |

### WindowStateTests (4 tests)

| Test | Description |
|---|---|
| `SetStateAsync__Minimize_WindowIsMinimized` | Minimize, verify state |
| `SetStateAsync__Maximize_WindowIsMaximized` | Maximize, verify state |
| `SetStateAsync__Restore_WindowReturnsToNormal` | Maximize then restore, verify Normal state |
| `FocusAsync__BringsWindowToForeground` | Create two TestWindows, focus the first one, verify it's the foreground window |

### FilteringTests (2 tests)

| Test | Description |
|---|---|
| `GetAll__WithProcessFilter_FindsTestWindow` | Filter by current process name, assert TestWindow is in results |
| `GetAll__WithTitleFilter_FindsTestWindow` | Give TestWindow a unique title, filter by title pattern, assert match |

### CrossMonitorTests (3 tests, `[RequiresMultipleMonitorsFact]`)

| Test | Description |
|---|---|
| `MoveToMonitorAsync__WindowMovesToTargetMonitor` | Move window to secondary monitor, verify `window.Monitor` matches target |
| `MoveAsync__CrossMonitor_WindowLandsOnCorrectMonitor` | Move to a position within secondary monitor's bounds, verify monitor assignment |
| `SnapAsync__OnSecondaryMonitor_SnapsToCorrectWorkArea` | Move to secondary, snap Left, verify bounds within secondary's work area |

### CrossMonitorDpiTests (2 tests, `[RequiresMixedDpiFact]`)

| Test | Description |
|---|---|
| `MoveToMonitorAsync__DifferentDpi_BoundsCorrectRelativeToTarget` | Move to monitor with different DPI, verify bounds within its work area |
| `SnapAsync__HighDpiMonitor_FillsExpectedProportion` | Snap Left on higher-DPI monitor, verify width is ~half the work area width |

### CrossMonitorResolutionTests (2 tests, `[RequiresMixedResolutionFact]`)

| Test | Description |
|---|---|
| `MoveToMonitorAsync__DifferentResolution_BoundsWithinWorkArea` | Move to monitor with different resolution, verify window fits within target work area |
| `SnapAsync__DifferentResolution_FillsCorrectProportion` | Snap on each monitor, verify proportions correct relative to each monitor's work area |

## Test Infrastructure

### Each test class

- Creates a `WindowManager` via `WindowManagementFactory.Create(new WindowManagementOptions { EnforceDpiAwareness = false })` in its constructor.
- Implements `IAsyncDisposable` to dispose the manager.
- Each test method creates its own `TestWindow` via `using var window = TestWindow.Create(...)` for full isolation.

### Assertions

- Use FluentAssertions throughout.
- Bounds checks use tolerances where appropriate (invisible borders can cause ±1-7px differences).
- Snap proportion checks verify approximate ratios (e.g., width within ±20px of half the work area width) rather than exact pixel values.

### Dependencies

- The integration test project needs `<UseWindowsForms>true</UseWindowsForms>` in its csproj to reference `System.Windows.Forms` for the `TestWindow` utility.
- No new NuGet packages required.

## Total: 20 tests across 8 test classes, plus 4 helper files
