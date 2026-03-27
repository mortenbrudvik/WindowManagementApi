# Integration Tests Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add comprehensive integration tests that exercise real Win32 window operations — lifecycle, move/resize, snap, state, filtering, and cross-monitor scenarios.

**Architecture:** A `TestWindow` utility wraps a WinForms `Form` on a dedicated STA thread to provide a real HWND under full test control. Three custom xUnit `FactAttribute` subclasses enable conditional skipping for multi-monitor/mixed-DPI/mixed-resolution scenarios. Eight test classes cover all scenarios from the design spec.

**Tech Stack:** .NET 10, xUnit, FluentAssertions, System.Windows.Forms (for TestWindow)

---

### Task 1: Enable WinForms in Integration Test Project

**Files:**
- Modify: `tests/WindowManagement.IntegrationTests/WindowManagement.IntegrationTests.csproj`

- [ ] **Step 1: Add UseWindowsForms to csproj**

Add `<UseWindowsForms>true</UseWindowsForms>` to the PropertyGroup in the csproj file:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <UseWindowsForms>true</UseWindowsForms>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FluentAssertions" Version="7.*" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\WindowManagement\WindowManagement.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Verify build succeeds**

Run: `dotnet build tests/WindowManagement.IntegrationTests`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add tests/WindowManagement.IntegrationTests/WindowManagement.IntegrationTests.csproj
git commit -m "chore: enable WinForms in integration test project"
```

---

### Task 2: Create TestWindow Utility

**Files:**
- Create: `tests/WindowManagement.IntegrationTests/Helpers/TestWindow.cs`

- [ ] **Step 1: Write TestWindow class**

```csharp
using System.Windows.Forms;

namespace WindowManagement.IntegrationTests.Helpers;

public class TestWindow : IDisposable
{
    private readonly Form _form;
    private readonly Thread _thread;
    private readonly ManualResetEventSlim _ready = new();
    private bool _disposed;

    private TestWindow(Options options)
    {
        Form? form = null;

        _thread = new Thread(() =>
        {
            form = new Form
            {
                Text = options.Title,
                Width = options.Width,
                Height = options.Height,
                StartPosition = FormStartPosition.Manual,
                Location = new System.Drawing.Point(options.X, options.Y),
                ShowInTaskbar = true,
            };

            if (!options.Resizable)
            {
                form.FormBorderStyle = FormBorderStyle.FixedSingle;
                form.MaximizeBox = false;
            }

            form.Shown += (_, _) => _ready.Set();
            Application.Run(form);
        });

        _thread.SetApartmentState(ApartmentState.STA);
        _thread.IsBackground = true;
        _thread.Start();

        if (!_ready.Wait(TimeSpan.FromSeconds(5)))
            throw new TimeoutException("TestWindow failed to become ready within 5 seconds.");

        _form = form!;

        // Allow window to fully render and become queryable by Win32 APIs
        Thread.Sleep(200);
    }

    public static TestWindow Create(Action<Options>? configure = null)
    {
        var options = new Options();
        configure?.Invoke(options);
        return new TestWindow(options);
    }

    public nint Handle => _form.Handle;

    public string Title => Invoke(() => _form.Text);

    public WindowRect Bounds
    {
        get
        {
            var b = Invoke(() => new { _form.Left, _form.Top, _form.Width, _form.Height });
            return new WindowRect(b.Left, b.Top, b.Width, b.Height);
        }
    }

    public void SetTitle(string title) => Invoke(() => _form.Text = title);

    public void SetPosition(int x, int y) =>
        Invoke(() => _form.Location = new System.Drawing.Point(x, y));

    public void SetSize(int width, int height) =>
        Invoke(() => _form.Size = new System.Drawing.Size(width, height));

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_form.IsHandleCreated)
        {
            _form.Invoke(() => _form.Close());
        }

        _thread.Join(TimeSpan.FromSeconds(3));
        _ready.Dispose();
    }

    private T Invoke<T>(Func<T> action)
    {
        if (_form.InvokeRequired)
            return (T)_form.Invoke(action);
        return action();
    }

    private void Invoke(Action action)
    {
        if (_form.InvokeRequired)
            _form.Invoke(action);
        else
            action();
    }

    public class Options
    {
        internal string Title { get; private set; } = "TestWindow";
        internal int Width { get; private set; } = 400;
        internal int Height { get; private set; } = 300;
        internal int X { get; private set; } = 100;
        internal int Y { get; private set; } = 100;
        internal bool Resizable { get; private set; } = true;

        public Options WithTitle(string title) { Title = title; return this; }
        public Options WithSize(int width, int height) { Width = width; Height = height; return this; }
        public Options WithPosition(int x, int y) { X = x; Y = y; return this; }
        public Options NonResizable() { Resizable = false; return this; }
    }
}
```

- [ ] **Step 2: Verify build succeeds**

Run: `dotnet build tests/WindowManagement.IntegrationTests`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add tests/WindowManagement.IntegrationTests/Helpers/TestWindow.cs
git commit -m "feat: add TestWindow utility for integration tests"
```

---

### Task 3: Create Skip Attributes

**Files:**
- Create: `tests/WindowManagement.IntegrationTests/Helpers/RequiresMultipleMonitorsFactAttribute.cs`
- Create: `tests/WindowManagement.IntegrationTests/Helpers/RequiresMixedDpiFactAttribute.cs`
- Create: `tests/WindowManagement.IntegrationTests/Helpers/RequiresMixedResolutionFactAttribute.cs`

- [ ] **Step 1: Write RequiresMultipleMonitorsFactAttribute**

```csharp
using WindowManagement.LowLevel.Internal;
using Xunit;

namespace WindowManagement.IntegrationTests.Helpers;

public class RequiresMultipleMonitorsFactAttribute : FactAttribute
{
    public RequiresMultipleMonitorsFactAttribute()
    {
        var displays = new DisplayApi().GetAll();
        if (displays.Count < 2)
            Skip = "Requires multiple monitors";
    }
}
```

- [ ] **Step 2: Write RequiresMixedDpiFactAttribute**

```csharp
using WindowManagement.LowLevel.Internal;
using Xunit;

namespace WindowManagement.IntegrationTests.Helpers;

public class RequiresMixedDpiFactAttribute : FactAttribute
{
    public RequiresMixedDpiFactAttribute()
    {
        var displays = new DisplayApi().GetAll();
        if (displays.Count < 2 || displays.Select(d => d.Dpi).Distinct().Count() < 2)
            Skip = "Requires monitors with different DPI values";
    }
}
```

- [ ] **Step 3: Write RequiresMixedResolutionFactAttribute**

```csharp
using WindowManagement.LowLevel.Internal;
using Xunit;

namespace WindowManagement.IntegrationTests.Helpers;

public class RequiresMixedResolutionFactAttribute : FactAttribute
{
    public RequiresMixedResolutionFactAttribute()
    {
        var displays = new DisplayApi().GetAll();
        if (displays.Count < 2 ||
            displays.Select(d => (d.Bounds.Width, d.Bounds.Height)).Distinct().Count() < 2)
            Skip = "Requires monitors with different resolutions";
    }
}
```

- [ ] **Step 4: Verify build succeeds**

Run: `dotnet build tests/WindowManagement.IntegrationTests`
Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add tests/WindowManagement.IntegrationTests/Helpers/RequiresMultipleMonitorsFactAttribute.cs
git add tests/WindowManagement.IntegrationTests/Helpers/RequiresMixedDpiFactAttribute.cs
git add tests/WindowManagement.IntegrationTests/Helpers/RequiresMixedResolutionFactAttribute.cs
git commit -m "feat: add conditional skip attributes for multi-monitor tests"
```

---

### Task 4: WindowLifecycleTests

**Files:**
- Create: `tests/WindowManagement.IntegrationTests/WindowLifecycleTests.cs`

- [ ] **Step 1: Write WindowLifecycleTests**

```csharp
using FluentAssertions;
using WindowManagement.DependencyInjection;
using WindowManagement.IntegrationTests.Helpers;
using Xunit;

namespace WindowManagement.IntegrationTests;

public class WindowLifecycleTests : IAsyncDisposable
{
    private readonly IWindowManager _manager;

    public WindowLifecycleTests()
    {
        _manager = WindowManagementFactory.Create(new WindowManagementOptions
        {
            EnforceDpiAwareness = false
        });
    }

    [Fact]
    public void Create__WindowAppearsInGetAll()
    {
        using var window = TestWindow.Create();

        var windows = _manager.GetAll(f => f.Unfiltered());

        windows.Should().Contain(w => w.Handle == window.Handle);
    }

    [Fact]
    public void Close__WindowDisappearsFromGetAll()
    {
        var window = TestWindow.Create();
        var handle = window.Handle;

        window.Dispose();
        Thread.Sleep(200); // allow Win32 to process destruction

        var windows = _manager.GetAll(f => f.Unfiltered());
        windows.Should().NotContain(w => w.Handle == handle);
    }

    public async ValueTask DisposeAsync()
    {
        await _manager.DisposeAsync();
    }
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test tests/WindowManagement.IntegrationTests --filter "FullyQualifiedName~WindowLifecycleTests" -v n`
Expected: 2 passed.

- [ ] **Step 3: Commit**

```bash
git add tests/WindowManagement.IntegrationTests/WindowLifecycleTests.cs
git commit -m "test: add window lifecycle integration tests"
```

---

### Task 5: MoveAndResizeTests

**Files:**
- Create: `tests/WindowManagement.IntegrationTests/MoveAndResizeTests.cs`

- [ ] **Step 1: Write MoveAndResizeTests**

```csharp
using FluentAssertions;
using WindowManagement.DependencyInjection;
using WindowManagement.IntegrationTests.Helpers;
using Xunit;

namespace WindowManagement.IntegrationTests;

public class MoveAndResizeTests : IAsyncDisposable
{
    private const int Tolerance = 10;
    private readonly IWindowManager _manager;

    public MoveAndResizeTests()
    {
        _manager = WindowManagementFactory.Create(new WindowManagementOptions
        {
            EnforceDpiAwareness = false
        });
    }

    [Fact]
    public async Task MoveAsync__WindowMovesToNewPosition()
    {
        using var window = TestWindow.Create();
        var iWindow = FindWindow(window.Handle);

        await _manager.MoveAsync(iWindow, 200, 200);
        Thread.Sleep(100);

        var moved = FindWindow(window.Handle);
        moved.Bounds.X.Should().BeCloseTo(200, Tolerance);
        moved.Bounds.Y.Should().BeCloseTo(200, Tolerance);
    }

    [Fact]
    public async Task ResizeAsync__WindowChangesSize()
    {
        using var window = TestWindow.Create();
        var iWindow = FindWindow(window.Handle);

        await _manager.ResizeAsync(iWindow, 600, 400);
        Thread.Sleep(100);

        var resized = FindWindow(window.Handle);
        resized.Bounds.Width.Should().BeCloseTo(600, Tolerance);
        resized.Bounds.Height.Should().BeCloseTo(400, Tolerance);
    }

    [Fact]
    public async Task SetBoundsAsync__MovesAndResizesInOneCall()
    {
        using var window = TestWindow.Create();
        var iWindow = FindWindow(window.Handle);
        var target = new WindowRect(300, 300, 500, 350);

        await _manager.SetBoundsAsync(iWindow, target);
        Thread.Sleep(100);

        var updated = FindWindow(window.Handle);
        updated.Bounds.X.Should().BeCloseTo(target.X, Tolerance);
        updated.Bounds.Y.Should().BeCloseTo(target.Y, Tolerance);
        updated.Bounds.Width.Should().BeCloseTo(target.Width, Tolerance);
        updated.Bounds.Height.Should().BeCloseTo(target.Height, Tolerance);
    }

    private IWindow FindWindow(nint handle) =>
        _manager.GetAll(f => f.Unfiltered()).First(w => w.Handle == handle);

    public async ValueTask DisposeAsync()
    {
        await _manager.DisposeAsync();
    }
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test tests/WindowManagement.IntegrationTests --filter "FullyQualifiedName~MoveAndResizeTests" -v n`
Expected: 3 passed.

- [ ] **Step 3: Commit**

```bash
git add tests/WindowManagement.IntegrationTests/MoveAndResizeTests.cs
git commit -m "test: add move and resize integration tests"
```

---

### Task 6: SnapPositionTests

**Files:**
- Create: `tests/WindowManagement.IntegrationTests/SnapPositionTests.cs`

- [ ] **Step 1: Write SnapPositionTests**

```csharp
using FluentAssertions;
using WindowManagement.DependencyInjection;
using WindowManagement.IntegrationTests.Helpers;
using Xunit;

namespace WindowManagement.IntegrationTests;

public class SnapPositionTests : IAsyncDisposable
{
    private const int Tolerance = 20;
    private readonly IWindowManager _manager;

    public SnapPositionTests()
    {
        _manager = WindowManagementFactory.Create(new WindowManagementOptions
        {
            EnforceDpiAwareness = false
        });
    }

    [Theory]
    [InlineData(SnapPosition.Left, 0.5, 1.0)]
    [InlineData(SnapPosition.Right, 0.5, 1.0)]
    [InlineData(SnapPosition.Top, 1.0, 0.5)]
    [InlineData(SnapPosition.Bottom, 1.0, 0.5)]
    [InlineData(SnapPosition.TopLeft, 0.5, 0.5)]
    [InlineData(SnapPosition.TopRight, 0.5, 0.5)]
    [InlineData(SnapPosition.BottomLeft, 0.5, 0.5)]
    [InlineData(SnapPosition.BottomRight, 0.5, 0.5)]
    [InlineData(SnapPosition.Fill, 1.0, 1.0)]
    public async Task SnapAsync__AllPositions_WindowFillsExpectedArea(
        SnapPosition position, double expectedWidthRatio, double expectedHeightRatio)
    {
        using var window = TestWindow.Create();
        var iWindow = FindWindow(window.Handle);
        var monitor = _manager.Monitors.Primary;

        await _manager.SnapAsync(iWindow, monitor, position);
        Thread.Sleep(100);

        var snapped = FindWindow(window.Handle);
        var workArea = monitor.WorkArea;

        // Window should be within the monitor's work area (with tolerance for invisible borders)
        snapped.Bounds.X.Should().BeGreaterThanOrEqualTo(workArea.X - Tolerance);
        snapped.Bounds.Y.Should().BeGreaterThanOrEqualTo(workArea.Y - Tolerance);
        snapped.Bounds.Right.Should().BeLessThanOrEqualTo(workArea.Right + Tolerance);
        snapped.Bounds.Bottom.Should().BeLessThanOrEqualTo(workArea.Bottom + Tolerance);

        // Verify proportions
        var expectedWidth = (int)(workArea.Width * expectedWidthRatio);
        var expectedHeight = (int)(workArea.Height * expectedHeightRatio);

        snapped.Bounds.Width.Should().BeCloseTo(expectedWidth, Tolerance);
        snapped.Bounds.Height.Should().BeCloseTo(expectedHeight, Tolerance);
    }

    [Fact]
    public async Task SnapAsync__NonResizableWindow_CentersInSnapZone()
    {
        using var window = TestWindow.Create(o => o.NonResizable().WithSize(300, 200));
        var iWindow = FindWindow(window.Handle);
        var monitor = _manager.Monitors.Primary;

        await _manager.SnapAsync(iWindow, monitor, SnapPosition.Left);
        Thread.Sleep(100);

        var snapped = FindWindow(window.Handle);
        var workArea = monitor.WorkArea;
        var snapZoneWidth = workArea.Width / 2;

        // Size should be unchanged (non-resizable)
        snapped.Bounds.Width.Should().BeCloseTo(300, Tolerance);
        snapped.Bounds.Height.Should().BeCloseTo(200, Tolerance);

        // Should be centered in the left snap zone
        var expectedCenterX = workArea.X + snapZoneWidth / 2;
        var actualCenterX = snapped.Bounds.X + snapped.Bounds.Width / 2;
        actualCenterX.Should().BeCloseTo(expectedCenterX, Tolerance);
    }

    private IWindow FindWindow(nint handle) =>
        _manager.GetAll(f => f.Unfiltered()).First(w => w.Handle == handle);

    public async ValueTask DisposeAsync()
    {
        await _manager.DisposeAsync();
    }
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test tests/WindowManagement.IntegrationTests --filter "FullyQualifiedName~SnapPositionTests" -v n`
Expected: 10 passed (9 theory cases + 1 fact).

- [ ] **Step 3: Commit**

```bash
git add tests/WindowManagement.IntegrationTests/SnapPositionTests.cs
git commit -m "test: add snap position integration tests"
```

---

### Task 7: WindowStateTests

**Files:**
- Create: `tests/WindowManagement.IntegrationTests/WindowStateTests.cs`

- [ ] **Step 1: Write WindowStateTests**

```csharp
using FluentAssertions;
using WindowManagement.DependencyInjection;
using WindowManagement.IntegrationTests.Helpers;
using Xunit;

namespace WindowManagement.IntegrationTests;

public class WindowStateTests : IAsyncDisposable
{
    private readonly IWindowManager _manager;

    public WindowStateTests()
    {
        _manager = WindowManagementFactory.Create(new WindowManagementOptions
        {
            EnforceDpiAwareness = false
        });
    }

    [Fact]
    public async Task SetStateAsync__Minimize_WindowIsMinimized()
    {
        using var window = TestWindow.Create();
        var iWindow = FindWindow(window.Handle);

        await _manager.SetStateAsync(iWindow, WindowState.Minimized);
        Thread.Sleep(200);

        var updated = FindWindow(window.Handle);
        updated.State.Should().Be(WindowState.Minimized);
    }

    [Fact]
    public async Task SetStateAsync__Maximize_WindowIsMaximized()
    {
        using var window = TestWindow.Create();
        var iWindow = FindWindow(window.Handle);

        await _manager.SetStateAsync(iWindow, WindowState.Maximized);
        Thread.Sleep(200);

        var updated = FindWindow(window.Handle);
        updated.State.Should().Be(WindowState.Maximized);
    }

    [Fact]
    public async Task SetStateAsync__Restore_WindowReturnsToNormal()
    {
        using var window = TestWindow.Create();
        var iWindow = FindWindow(window.Handle);

        await _manager.SetStateAsync(iWindow, WindowState.Maximized);
        Thread.Sleep(200);

        await _manager.SetStateAsync(FindWindow(window.Handle), WindowState.Normal);
        Thread.Sleep(200);

        var updated = FindWindow(window.Handle);
        updated.State.Should().Be(WindowState.Normal);
    }

    [Fact]
    public async Task FocusAsync__BringsWindowToForeground()
    {
        using var window1 = TestWindow.Create(o => o.WithTitle("FocusTest_First"));
        using var window2 = TestWindow.Create(o => o.WithTitle("FocusTest_Second"));
        Thread.Sleep(100);

        // window2 is in front (created last). Focus window1.
        var iWindow1 = FindWindow(window1.Handle);
        await _manager.FocusAsync(iWindow1);
        Thread.Sleep(200);

        var foreground = _manager.GetForeground();
        foreground.Should().NotBeNull();
        foreground!.Handle.Should().Be(window1.Handle);
    }

    private IWindow FindWindow(nint handle) =>
        _manager.GetAll(f => f.Unfiltered()).First(w => w.Handle == handle);

    public async ValueTask DisposeAsync()
    {
        await _manager.DisposeAsync();
    }
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test tests/WindowManagement.IntegrationTests --filter "FullyQualifiedName~WindowStateTests" -v n`
Expected: 4 passed.

- [ ] **Step 3: Commit**

```bash
git add tests/WindowManagement.IntegrationTests/WindowStateTests.cs
git commit -m "test: add window state integration tests"
```

---

### Task 8: FilteringTests

**Files:**
- Create: `tests/WindowManagement.IntegrationTests/FilteringTests.cs`

- [ ] **Step 1: Write FilteringTests**

The test process name is the xUnit test runner process. The title filter uses wildcard matching (`*` and `?`).

```csharp
using System.Diagnostics;
using FluentAssertions;
using WindowManagement.DependencyInjection;
using WindowManagement.IntegrationTests.Helpers;
using Xunit;

namespace WindowManagement.IntegrationTests;

public class FilteringTests : IAsyncDisposable
{
    private readonly IWindowManager _manager;

    public FilteringTests()
    {
        _manager = WindowManagementFactory.Create(new WindowManagementOptions
        {
            EnforceDpiAwareness = false
        });
    }

    [Fact]
    public void GetAll__WithProcessFilter_FindsTestWindow()
    {
        using var window = TestWindow.Create();
        var processName = Process.GetCurrentProcess().ProcessName;

        var filtered = _manager.GetAll(f => f.Unfiltered().WithProcess(processName));

        filtered.Should().Contain(w => w.Handle == window.Handle);
    }

    [Fact]
    public void GetAll__WithTitleFilter_FindsTestWindow()
    {
        var uniqueTitle = $"IntegrationTest_{Guid.NewGuid():N}";
        using var window = TestWindow.Create(o => o.WithTitle(uniqueTitle));

        var filtered = _manager.GetAll(f => f.Unfiltered().WithTitle($"*{uniqueTitle}*"));

        filtered.Should().ContainSingle(w => w.Handle == window.Handle);
    }

    public async ValueTask DisposeAsync()
    {
        await _manager.DisposeAsync();
    }
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test tests/WindowManagement.IntegrationTests --filter "FullyQualifiedName~FilteringTests" -v n`
Expected: 2 passed.

- [ ] **Step 3: Commit**

```bash
git add tests/WindowManagement.IntegrationTests/FilteringTests.cs
git commit -m "test: add filtering integration tests"
```

---

### Task 9: CrossMonitorTests

**Files:**
- Create: `tests/WindowManagement.IntegrationTests/CrossMonitorTests.cs`

- [ ] **Step 1: Write CrossMonitorTests**

```csharp
using FluentAssertions;
using WindowManagement.DependencyInjection;
using WindowManagement.IntegrationTests.Helpers;
using Xunit;

namespace WindowManagement.IntegrationTests;

public class CrossMonitorTests : IAsyncDisposable
{
    private const int Tolerance = 20;
    private readonly IWindowManager _manager;

    public CrossMonitorTests()
    {
        _manager = WindowManagementFactory.Create(new WindowManagementOptions
        {
            EnforceDpiAwareness = false
        });
    }

    [RequiresMultipleMonitorsFact]
    public async Task MoveToMonitorAsync__WindowMovesToTargetMonitor()
    {
        using var window = TestWindow.Create();
        var iWindow = FindWindow(window.Handle);
        var targetMonitor = _manager.Monitors.All.First(m => !m.IsPrimary);

        await _manager.MoveToMonitorAsync(iWindow, targetMonitor);
        Thread.Sleep(200);

        var moved = FindWindow(window.Handle);
        moved.Monitor.DeviceName.Should().Be(targetMonitor.DeviceName);
    }

    [RequiresMultipleMonitorsFact]
    public async Task MoveAsync__CrossMonitor_WindowLandsOnCorrectMonitor()
    {
        using var window = TestWindow.Create();
        var iWindow = FindWindow(window.Handle);
        var targetMonitor = _manager.Monitors.All.First(m => !m.IsPrimary);
        var targetCenter = targetMonitor.WorkArea;

        // Move to center of target monitor's work area
        var targetX = targetCenter.X + targetCenter.Width / 4;
        var targetY = targetCenter.Y + targetCenter.Height / 4;

        await _manager.MoveAsync(iWindow, targetX, targetY);
        Thread.Sleep(200);

        var moved = FindWindow(window.Handle);
        moved.Monitor.DeviceName.Should().Be(targetMonitor.DeviceName);
    }

    [RequiresMultipleMonitorsFact]
    public async Task SnapAsync__OnSecondaryMonitor_SnapsToCorrectWorkArea()
    {
        using var window = TestWindow.Create();
        var iWindow = FindWindow(window.Handle);
        var targetMonitor = _manager.Monitors.All.First(m => !m.IsPrimary);

        // Move to secondary first
        await _manager.MoveToMonitorAsync(iWindow, targetMonitor);
        Thread.Sleep(200);

        // Snap Left on secondary
        await _manager.SnapAsync(FindWindow(window.Handle), targetMonitor, SnapPosition.Left);
        Thread.Sleep(100);

        var snapped = FindWindow(window.Handle);
        var workArea = targetMonitor.WorkArea;

        snapped.Bounds.X.Should().BeGreaterThanOrEqualTo(workArea.X - Tolerance);
        snapped.Bounds.Y.Should().BeGreaterThanOrEqualTo(workArea.Y - Tolerance);
        snapped.Bounds.Right.Should().BeLessThanOrEqualTo(workArea.X + workArea.Width / 2 + Tolerance);
        snapped.Bounds.Width.Should().BeCloseTo(workArea.Width / 2, Tolerance);
    }

    private IWindow FindWindow(nint handle) =>
        _manager.GetAll(f => f.Unfiltered()).First(w => w.Handle == handle);

    public async ValueTask DisposeAsync()
    {
        await _manager.DisposeAsync();
    }
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test tests/WindowManagement.IntegrationTests --filter "FullyQualifiedName~CrossMonitorTests" -v n`
Expected: 3 passed (or 3 skipped if single monitor).

- [ ] **Step 3: Commit**

```bash
git add tests/WindowManagement.IntegrationTests/CrossMonitorTests.cs
git commit -m "test: add cross-monitor integration tests"
```

---

### Task 10: CrossMonitorDpiTests

**Files:**
- Create: `tests/WindowManagement.IntegrationTests/CrossMonitorDpiTests.cs`

- [ ] **Step 1: Write CrossMonitorDpiTests**

```csharp
using FluentAssertions;
using WindowManagement.DependencyInjection;
using WindowManagement.IntegrationTests.Helpers;
using Xunit;

namespace WindowManagement.IntegrationTests;

public class CrossMonitorDpiTests : IAsyncDisposable
{
    private const int Tolerance = 20;
    private readonly IWindowManager _manager;

    public CrossMonitorDpiTests()
    {
        _manager = WindowManagementFactory.Create(new WindowManagementOptions
        {
            EnforceDpiAwareness = false
        });
    }

    [RequiresMixedDpiFact]
    public async Task MoveToMonitorAsync__DifferentDpi_BoundsCorrectRelativeToTarget()
    {
        using var window = TestWindow.Create();
        var iWindow = FindWindow(window.Handle);

        // Find a monitor with a different DPI than the current one
        var currentMonitor = iWindow.Monitor;
        var targetMonitor = _manager.Monitors.All.First(m => m.Dpi != currentMonitor.Dpi);

        await _manager.MoveToMonitorAsync(iWindow, targetMonitor);
        Thread.Sleep(200);

        var moved = FindWindow(window.Handle);
        var workArea = targetMonitor.WorkArea;

        // Window should be positioned within the target monitor's work area
        moved.Bounds.X.Should().BeGreaterThanOrEqualTo(workArea.X - Tolerance);
        moved.Bounds.Y.Should().BeGreaterThanOrEqualTo(workArea.Y - Tolerance);
        moved.Bounds.Right.Should().BeLessThanOrEqualTo(workArea.Right + Tolerance);
        moved.Bounds.Bottom.Should().BeLessThanOrEqualTo(workArea.Bottom + Tolerance);
    }

    [RequiresMixedDpiFact]
    public async Task SnapAsync__HighDpiMonitor_FillsExpectedProportion()
    {
        using var window = TestWindow.Create();
        var iWindow = FindWindow(window.Handle);

        // Find the monitor with the highest DPI
        var highDpiMonitor = _manager.Monitors.All.OrderByDescending(m => m.Dpi).First();

        // Move to target monitor first
        await _manager.MoveToMonitorAsync(iWindow, highDpiMonitor);
        Thread.Sleep(200);

        // Snap Left
        await _manager.SnapAsync(FindWindow(window.Handle), highDpiMonitor, SnapPosition.Left);
        Thread.Sleep(100);

        var snapped = FindWindow(window.Handle);
        var workArea = highDpiMonitor.WorkArea;
        var expectedWidth = workArea.Width / 2;

        // Width should be approximately half the work area despite DPI differences
        snapped.Bounds.Width.Should().BeCloseTo(expectedWidth, Tolerance);
        snapped.Bounds.Height.Should().BeCloseTo(workArea.Height, Tolerance);
    }

    private IWindow FindWindow(nint handle) =>
        _manager.GetAll(f => f.Unfiltered()).First(w => w.Handle == handle);

    public async ValueTask DisposeAsync()
    {
        await _manager.DisposeAsync();
    }
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test tests/WindowManagement.IntegrationTests --filter "FullyQualifiedName~CrossMonitorDpiTests" -v n`
Expected: 2 passed (or 2 skipped if same DPI across all monitors).

- [ ] **Step 3: Commit**

```bash
git add tests/WindowManagement.IntegrationTests/CrossMonitorDpiTests.cs
git commit -m "test: add cross-monitor mixed-DPI integration tests"
```

---

### Task 11: CrossMonitorResolutionTests

**Files:**
- Create: `tests/WindowManagement.IntegrationTests/CrossMonitorResolutionTests.cs`

- [ ] **Step 1: Write CrossMonitorResolutionTests**

```csharp
using FluentAssertions;
using WindowManagement.DependencyInjection;
using WindowManagement.IntegrationTests.Helpers;
using Xunit;

namespace WindowManagement.IntegrationTests;

public class CrossMonitorResolutionTests : IAsyncDisposable
{
    private const int Tolerance = 20;
    private readonly IWindowManager _manager;

    public CrossMonitorResolutionTests()
    {
        _manager = WindowManagementFactory.Create(new WindowManagementOptions
        {
            EnforceDpiAwareness = false
        });
    }

    [RequiresMixedResolutionFact]
    public async Task MoveToMonitorAsync__DifferentResolution_BoundsWithinWorkArea()
    {
        using var window = TestWindow.Create(o => o.WithSize(400, 300));
        var iWindow = FindWindow(window.Handle);

        // Find a monitor with a different resolution
        var currentMonitor = iWindow.Monitor;
        var targetMonitor = _manager.Monitors.All.First(m =>
            m.Bounds.Width != currentMonitor.Bounds.Width ||
            m.Bounds.Height != currentMonitor.Bounds.Height);

        await _manager.MoveToMonitorAsync(iWindow, targetMonitor);
        Thread.Sleep(200);

        var moved = FindWindow(window.Handle);
        var workArea = targetMonitor.WorkArea;

        // Window should fit within target monitor's work area
        moved.Bounds.X.Should().BeGreaterThanOrEqualTo(workArea.X - Tolerance);
        moved.Bounds.Y.Should().BeGreaterThanOrEqualTo(workArea.Y - Tolerance);
        moved.Bounds.Right.Should().BeLessThanOrEqualTo(workArea.Right + Tolerance);
        moved.Bounds.Bottom.Should().BeLessThanOrEqualTo(workArea.Bottom + Tolerance);
    }

    [RequiresMixedResolutionFact]
    public async Task SnapAsync__DifferentResolution_FillsCorrectProportion()
    {
        using var window = TestWindow.Create();

        // Test snap on each monitor — proportions should be correct relative to that monitor's work area
        foreach (var monitor in _manager.Monitors.All)
        {
            var iWindow = FindWindow(window.Handle);
            await _manager.MoveToMonitorAsync(iWindow, monitor);
            Thread.Sleep(200);

            await _manager.SnapAsync(FindWindow(window.Handle), monitor, SnapPosition.Left);
            Thread.Sleep(100);

            var snapped = FindWindow(window.Handle);
            var workArea = monitor.WorkArea;
            var expectedWidth = workArea.Width / 2;

            snapped.Bounds.Width.Should().BeCloseTo(expectedWidth, Tolerance,
                $"on monitor {monitor.DeviceName} ({workArea.Width}x{workArea.Height})");
            snapped.Bounds.Height.Should().BeCloseTo(workArea.Height, Tolerance,
                $"on monitor {monitor.DeviceName} ({workArea.Width}x{workArea.Height})");
        }
    }

    private IWindow FindWindow(nint handle) =>
        _manager.GetAll(f => f.Unfiltered()).First(w => w.Handle == handle);

    public async ValueTask DisposeAsync()
    {
        await _manager.DisposeAsync();
    }
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test tests/WindowManagement.IntegrationTests --filter "FullyQualifiedName~CrossMonitorResolutionTests" -v n`
Expected: 2 passed (or 2 skipped if same resolution across all monitors).

- [ ] **Step 3: Commit**

```bash
git add tests/WindowManagement.IntegrationTests/CrossMonitorResolutionTests.cs
git commit -m "test: add cross-monitor mixed-resolution integration tests"
```

---

### Task 12: Full Test Suite Verification

- [ ] **Step 1: Run the complete integration test suite**

Run: `dotnet test tests/WindowManagement.IntegrationTests -v n`
Expected: All tests pass (multi-monitor tests skip on single-monitor machines).

- [ ] **Step 2: Run the complete solution test suite to verify no regressions**

Run: `dotnet test WindowManagementApi.slnx -v n`
Expected: All unit tests and integration tests pass.

- [ ] **Step 3: Commit (if any fixups were needed)**

Only commit if fixes were needed during verification.
