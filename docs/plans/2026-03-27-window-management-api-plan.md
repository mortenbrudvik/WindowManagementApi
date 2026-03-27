# WindowManagementApi Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a general-purpose .NET 10 library for programmatic Windows window management with first-class multi-monitor, mixed-DPI support.

**Architecture:** Two-layer API — high-level `IWindowManager`/`IMonitorService` for consumers, low-level `IWindowApi`/`IDisplayApi` for advanced use. High-level layer delegates to low-level interfaces, making it fully testable via mocking. CsWin32 generates all P/Invoke signatures at build time.

**Tech Stack:** .NET 10, CsWin32, R3 (reactive), xUnit + FluentAssertions + NSubstitute, Autofac + Microsoft.Extensions.DependencyInjection

**Design doc:** `docs/plans/2026-03-27-window-management-api-design.md`

---

## Task 1: Project Scaffolding

**Files:**
- Create: `src/WindowManagement/WindowManagement.csproj`
- Create: `src/WindowManagement/NativeMethods.txt`
- Create: `src/WindowManagement/NativeMethods.json`
- Create: `tests/WindowManagement.Tests/WindowManagement.Tests.csproj`
- Create: `tests/WindowManagement.IntegrationTests/WindowManagement.IntegrationTests.csproj`
- Create: `WindowManagementApi.slnx`

**Step 1: Create the source project**

```xml
<!-- src/WindowManagement/WindowManagement.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>WindowManagement</RootNamespace>
    <PackageId>WindowManagement</PackageId>
    <Description>A general-purpose .NET library for programmatic Windows window management with multi-monitor DPI support</Description>
  </PropertyGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="WindowManagement.Tests" />
    <InternalsVisibleTo Include="WindowManagement.IntegrationTests" />
    <InternalsVisibleTo Include="DynamicProxyGenAssembly2" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Windows.CsWin32" Version="0.3.*" PrivateAssets="all" />
    <PackageReference Include="R3" Version="1.*" />
    <PackageReference Include="Autofac" Version="8.*" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.*" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.*" />
    <PackageReference Include="Microsoft.Extensions.Options" Version="10.*" />
  </ItemGroup>

  <ItemGroup>
    <None Include="../../README.md" Pack="true" PackagePath="" />
    <None Include="../../LICENSE" Pack="true" PackagePath="" />
  </ItemGroup>

</Project>
```

**Step 2: Create NativeMethods.txt**

```
// Window enumeration and info
EnumWindows
GetWindowText
GetWindowTextLength
GetClassName
GetWindowRect
GetClientRect
GetWindowLongPtr
GetWindowThreadProcessId
IsWindow
IsWindowVisible
IsIconic
IsZoomed
GetWindow

// Window manipulation
SetWindowPos
ShowWindow
GetWindowPlacement
SetWindowPlacement
SetForegroundWindow
GetForegroundWindow
BringWindowToTop
AttachThreadInput

// Monitor APIs
MonitorFromWindow
MonitorFromPoint
GetMonitorInfo
EnumDisplayMonitors

// DPI APIs
GetDpiForWindow
GetDpiForMonitor
GetWindowDpiAwarenessContext
GetAwarenessFromDpiAwarenessContext
GetProcessDpiAwarenessContext
AreDpiAwarenessContextsEqual

// DWM APIs
DwmGetWindowAttribute

// Accessibility event hooks
SetWinEventHook
UnhookWinEvent

// Thread/Process
GetCurrentThreadId
GetCurrentProcessId
OpenProcess
QueryFullProcessImageName

// Constants and types referenced directly
HWND
HMONITOR
RECT
WINDOWPLACEMENT
SHOW_WINDOW_CMD
SET_WINDOW_POS_FLAGS
WINDOW_STYLE
WINDOW_EX_STYLE
WINEVENT_OUTOFCONTEXT
MONITOR_FROM_FLAGS
DPI_AWARENESS_CONTEXT
DWMWINDOWATTRIBUTE
```

**Step 3: Create NativeMethods.json**

```json
{
  "$schema": "https://aka.ms/CsWin32.schema.json",
  "wideCharOnly": true,
  "allowMarshaling": true
}
```

**Step 4: Create the unit test project**

```xml
<!-- tests/WindowManagement.Tests/WindowManagement.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FluentAssertions" Version="7.*" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.*" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="NSubstitute" Version="5.*" />
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\WindowManagement\WindowManagement.csproj" />
  </ItemGroup>

</Project>
```

**Step 5: Create the integration test project**

```xml
<!-- tests/WindowManagement.IntegrationTests/WindowManagement.IntegrationTests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
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

**Step 6: Create the solution file**

```xml
<!-- WindowManagementApi.slnx -->
<Solution>
  <Folder Name="/src/">
    <Project Path="src/WindowManagement/WindowManagement.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/WindowManagement.Tests/WindowManagement.Tests.csproj" />
    <Project Path="tests/WindowManagement.IntegrationTests/WindowManagement.IntegrationTests.csproj" />
  </Folder>
</Solution>
```

**Step 7: Verify it builds**

Run: `dotnet build WindowManagementApi.slnx`
Expected: Build succeeded with 0 errors

**Step 8: Commit**

```bash
git add -A
git commit -m "feat: scaffold WindowManagementApi project structure"
```

---

## Task 2: Models and Enums

**Files:**
- Create: `src/WindowManagement/Models/WindowRect.cs`
- Create: `src/WindowManagement/Models/WindowState.cs`
- Create: `src/WindowManagement/Models/SnapPosition.cs`
- Create: `src/WindowManagement/Models/EventArgs/WindowEventArgs.cs`
- Create: `src/WindowManagement/Models/EventArgs/WindowMovedEventArgs.cs`
- Create: `src/WindowManagement/Models/EventArgs/WindowStateEventArgs.cs`
- Create: `src/WindowManagement/Models/EventArgs/MonitorEventArgs.cs`
- Create: `src/WindowManagement/Models/DisplayInfo.cs`
- Test: `tests/WindowManagement.Tests/Models/WindowRectTests.cs`

**Step 1: Write WindowRect tests**

```csharp
// tests/WindowManagement.Tests/Models/WindowRectTests.cs
namespace WindowManagement.Tests.Models;

public class WindowRectTests
{
    [Fact]
    public void Constructor__SetsAllProperties()
    {
        var rect = new WindowRect(10, 20, 800, 600);

        rect.X.Should().Be(10);
        rect.Y.Should().Be(20);
        rect.Width.Should().Be(800);
        rect.Height.Should().Be(600);
    }

    [Fact]
    public void Right__ReturnsXPlusWidth()
    {
        var rect = new WindowRect(10, 20, 800, 600);

        rect.Right.Should().Be(810);
    }

    [Fact]
    public void Bottom__ReturnsYPlusHeight()
    {
        var rect = new WindowRect(10, 20, 800, 600);

        rect.Bottom.Should().Be(620);
    }

    [Fact]
    public void Equality__TwoRectsWithSameValues_AreEqual()
    {
        var a = new WindowRect(10, 20, 800, 600);
        var b = new WindowRect(10, 20, 800, 600);

        a.Should().Be(b);
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/WindowManagement.Tests --filter "FullyQualifiedName~WindowRectTests" -v quiet`
Expected: FAIL — `WindowRect` does not exist

**Step 3: Implement all models**

```csharp
// src/WindowManagement/Models/WindowRect.cs
namespace WindowManagement;

public record WindowRect(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;
    public int Bottom => Y + Height;
}
```

```csharp
// src/WindowManagement/Models/WindowState.cs
namespace WindowManagement;

public enum WindowState
{
    Normal,
    Minimized,
    Maximized
}
```

```csharp
// src/WindowManagement/Models/SnapPosition.cs
namespace WindowManagement;

public enum SnapPosition
{
    Left,
    Right,
    Top,
    Bottom,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    Fill
}
```

```csharp
// src/WindowManagement/Models/DisplayInfo.cs
namespace WindowManagement.LowLevel;

public record DisplayInfo(
    nint Handle,
    string DeviceName,
    string DisplayName,
    bool IsPrimary,
    WindowRect Bounds,
    WindowRect WorkArea,
    int Dpi,
    double ScaleFactor);
```

```csharp
// src/WindowManagement/Models/EventArgs/WindowEventArgs.cs
namespace WindowManagement;

public class WindowEventArgs : EventArgs
{
    public required nint Handle { get; init; }
    public required string Title { get; init; }
    public required string ProcessName { get; init; }
}
```

```csharp
// src/WindowManagement/Models/EventArgs/WindowMovedEventArgs.cs
namespace WindowManagement;

public class WindowMovedEventArgs : WindowEventArgs
{
    public required WindowRect OldBounds { get; init; }
    public required WindowRect NewBounds { get; init; }
}
```

```csharp
// src/WindowManagement/Models/EventArgs/WindowStateEventArgs.cs
namespace WindowManagement;

public class WindowStateEventArgs : WindowEventArgs
{
    public required WindowState OldState { get; init; }
    public required WindowState NewState { get; init; }
}
```

```csharp
// src/WindowManagement/Models/EventArgs/MonitorEventArgs.cs
namespace WindowManagement;

public class MonitorEventArgs : EventArgs
{
    public required string DeviceName { get; init; }
    public required WindowRect Bounds { get; init; }
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/WindowManagement.Tests --filter "FullyQualifiedName~WindowRectTests" -v quiet`
Expected: All 4 tests PASS

**Step 5: Commit**

```bash
git add -A
git commit -m "feat: add core models, enums, and event args"
```

---

## Task 3: Exceptions

**Files:**
- Create: `src/WindowManagement/Exceptions/WindowManagementException.cs`
- Create: `src/WindowManagement/Exceptions/WindowNotFoundException.cs`
- Create: `src/WindowManagement/Exceptions/DpiAwarenessException.cs`
- Test: `tests/WindowManagement.Tests/Exceptions/ExceptionTests.cs`

**Step 1: Write exception tests**

```csharp
// tests/WindowManagement.Tests/Exceptions/ExceptionTests.cs
using WindowManagement.Exceptions;

namespace WindowManagement.Tests.Exceptions;

public class ExceptionTests
{
    [Fact]
    public void WindowManagementException__IsException()
    {
        var ex = new WindowManagementException("test");
        ex.Should().BeAssignableTo<Exception>();
        ex.Message.Should().Be("test");
    }

    [Fact]
    public void WindowNotFoundException__IsWindowManagementException()
    {
        var ex = new WindowNotFoundException(0x1234);
        ex.Should().BeAssignableTo<WindowManagementException>();
        ex.Handle.Should().Be(0x1234);
    }

    [Fact]
    public void DpiAwarenessException__IsWindowManagementException()
    {
        var ex = new DpiAwarenessException();
        ex.Should().BeAssignableTo<WindowManagementException>();
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/WindowManagement.Tests --filter "FullyQualifiedName~ExceptionTests" -v quiet`
Expected: FAIL

**Step 3: Implement exceptions**

```csharp
// src/WindowManagement/Exceptions/WindowManagementException.cs
namespace WindowManagement.Exceptions;

public class WindowManagementException : Exception
{
    public WindowManagementException(string message) : base(message) { }
    public WindowManagementException(string message, Exception inner) : base(message, inner) { }
}
```

```csharp
// src/WindowManagement/Exceptions/WindowNotFoundException.cs
namespace WindowManagement.Exceptions;

public class WindowNotFoundException : WindowManagementException
{
    public nint Handle { get; }

    public WindowNotFoundException(nint handle)
        : base($"Window handle 0x{handle:X} is no longer valid.")
    {
        Handle = handle;
    }
}
```

```csharp
// src/WindowManagement/Exceptions/DpiAwarenessException.cs
namespace WindowManagement.Exceptions;

public class DpiAwarenessException : WindowManagementException
{
    public DpiAwarenessException()
        : base("Process must be configured for per-monitor v2 DPI awareness. " +
               "Set DpiAwareness in your app manifest or call SetProcessDpiAwarenessContext before creating WindowManager.")
    { }
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/WindowManagement.Tests --filter "FullyQualifiedName~ExceptionTests" -v quiet`
Expected: All 3 tests PASS

**Step 5: Commit**

```bash
git add -A
git commit -m "feat: add exception types"
```

---

## Task 4: Low-Level Interfaces

**Files:**
- Create: `src/WindowManagement/LowLevel/IWindowApi.cs`
- Create: `src/WindowManagement/LowLevel/IDisplayApi.cs`

**Step 1: Create the interfaces**

```csharp
// src/WindowManagement/LowLevel/IWindowApi.cs
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
    bool IsValid(nint hwnd);
    bool IsResizable(nint hwnd);
    (int left, int top, int right, int bottom) GetInvisibleBorders(nint hwnd);
    uint GetDpi(nint hwnd);

    void Move(nint hwnd, int x, int y);
    void Resize(nint hwnd, int width, int height);
    void SetBounds(nint hwnd, WindowRect bounds);
    void SetState(nint hwnd, WindowState state);
    void Focus(nint hwnd);
    void SetTopmost(nint hwnd, bool topmost);
    bool RestoreMinimized(nint hwnd);
    void BringToFront(nint hwnd);
}
```

```csharp
// src/WindowManagement/LowLevel/IDisplayApi.cs
namespace WindowManagement.LowLevel;

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
    bool IsPerMonitorV2Aware();
}
```

**Step 2: Verify it builds**

Run: `dotnet build src/WindowManagement/WindowManagement.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add -A
git commit -m "feat: add low-level IWindowApi and IDisplayApi interfaces"
```

---

## Task 5: High-Level Interfaces

**Files:**
- Create: `src/WindowManagement/IWindow.cs`
- Create: `src/WindowManagement/IMonitor.cs`
- Create: `src/WindowManagement/IWindowManager.cs`
- Create: `src/WindowManagement/IMonitorService.cs`
- Create: `src/WindowManagement/Filtering/WindowFilter.cs`
- Create: `src/WindowManagement/Filtering/WindowFilterBuilder.cs`
- Test: `tests/WindowManagement.Tests/Filtering/WindowFilterBuilderTests.cs`

**Step 1: Write WindowFilterBuilder tests**

```csharp
// tests/WindowManagement.Tests/Filtering/WindowFilterBuilderTests.cs
using WindowManagement.Filtering;

namespace WindowManagement.Tests.Filtering;

public class WindowFilterBuilderTests
{
    [Fact]
    public void Default__UseAltTabFiltering()
    {
        var builder = new WindowFilterBuilder();
        var filter = builder.Build();

        filter.AltTabOnly.Should().BeTrue();
    }

    [Fact]
    public void Unfiltered__DisablesAltTabFiltering()
    {
        var builder = new WindowFilterBuilder();
        builder.Unfiltered();
        var filter = builder.Build();

        filter.AltTabOnly.Should().BeFalse();
    }

    [Fact]
    public void WithProcess__SetsProcessNameFilter()
    {
        var builder = new WindowFilterBuilder();
        builder.WithProcess("notepad");
        var filter = builder.Build();

        filter.ProcessName.Should().Be("notepad");
    }

    [Fact]
    public void WithTitle__SetsTitlePattern()
    {
        var builder = new WindowFilterBuilder();
        builder.WithTitle("*.txt");
        var filter = builder.Build();

        filter.TitlePattern.Should().Be("*.txt");
    }

    [Fact]
    public void ExcludeMinimized__SetsFlag()
    {
        var builder = new WindowFilterBuilder();
        builder.ExcludeMinimized();
        var filter = builder.Build();

        filter.IncludeMinimized.Should().BeFalse();
    }

    [Fact]
    public void Where__AddsPredicate()
    {
        var builder = new WindowFilterBuilder();
        builder.Where(w => w.Bounds.Width > 500);
        var filter = builder.Build();

        filter.Predicate.Should().NotBeNull();
    }

    [Fact]
    public void FluentChaining__AllFiltersApplied()
    {
        var builder = new WindowFilterBuilder();
        builder
            .WithProcess("notepad")
            .WithTitle("*.txt")
            .ExcludeMinimized();
        var filter = builder.Build();

        filter.ProcessName.Should().Be("notepad");
        filter.TitlePattern.Should().Be("*.txt");
        filter.IncludeMinimized.Should().BeFalse();
        filter.AltTabOnly.Should().BeTrue();
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/WindowManagement.Tests --filter "FullyQualifiedName~WindowFilterBuilderTests" -v quiet`
Expected: FAIL

**Step 3: Implement filter types**

```csharp
// src/WindowManagement/Filtering/WindowFilter.cs
namespace WindowManagement.Filtering;

public class WindowFilter
{
    public bool AltTabOnly { get; init; } = true;
    public string? ProcessName { get; init; }
    public string? TitlePattern { get; init; }
    public string? ClassName { get; init; }
    public bool IncludeMinimized { get; init; } = true;
    public Func<IWindow, bool>? Predicate { get; init; }
}
```

```csharp
// src/WindowManagement/Filtering/WindowFilterBuilder.cs
namespace WindowManagement.Filtering;

public class WindowFilterBuilder
{
    private bool _altTabOnly = true;
    private string? _processName;
    private string? _titlePattern;
    private string? _className;
    private bool _includeMinimized = true;
    private Func<IWindow, bool>? _predicate;

    public WindowFilterBuilder Unfiltered()
    {
        _altTabOnly = false;
        return this;
    }

    public WindowFilterBuilder WithProcess(string processName)
    {
        _processName = processName;
        return this;
    }

    public WindowFilterBuilder WithTitle(string pattern)
    {
        _titlePattern = pattern;
        return this;
    }

    public WindowFilterBuilder WithClassName(string className)
    {
        _className = className;
        return this;
    }

    public WindowFilterBuilder ExcludeMinimized()
    {
        _includeMinimized = false;
        return this;
    }

    public WindowFilterBuilder Where(Func<IWindow, bool> predicate)
    {
        _predicate = predicate;
        return this;
    }

    public WindowFilter Build() => new()
    {
        AltTabOnly = _altTabOnly,
        ProcessName = _processName,
        TitlePattern = _titlePattern,
        ClassName = _className,
        IncludeMinimized = _includeMinimized,
        Predicate = _predicate
    };
}
```

**Step 4: Create high-level interfaces**

```csharp
// src/WindowManagement/IWindow.cs
namespace WindowManagement;

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
```

```csharp
// src/WindowManagement/IMonitor.cs
namespace WindowManagement;

public interface IMonitor
{
    nint Handle { get; }
    string DeviceName { get; }
    string DisplayName { get; }
    bool IsPrimary { get; }
    WindowRect Bounds { get; }
    WindowRect WorkArea { get; }
    int Dpi { get; }
    double ScaleFactor { get; }
}
```

```csharp
// src/WindowManagement/IMonitorService.cs
using R3;

namespace WindowManagement;

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

```csharp
// src/WindowManagement/IWindowManager.cs
using R3;
using WindowManagement.Filtering;

namespace WindowManagement;

public interface IWindowManager : IAsyncDisposable
{
    IMonitorService Monitors { get; }

    IReadOnlyList<IWindow> GetAll(WindowFilter? filter = null);
    IReadOnlyList<IWindow> GetAll(Action<WindowFilterBuilder> configure);
    IWindow? GetForeground();

    Task MoveAsync(IWindow window, int x, int y);
    Task ResizeAsync(IWindow window, int width, int height);
    Task SetBoundsAsync(IWindow window, WindowRect bounds);
    Task MoveToMonitorAsync(IWindow window, IMonitor monitor);
    Task SetStateAsync(IWindow window, WindowState state);
    Task FocusAsync(IWindow window);
    Task SnapAsync(IWindow window, IMonitor monitor, SnapPosition position);

    Observable<WindowEventArgs> Created { get; }
    Observable<WindowEventArgs> Destroyed { get; }
    Observable<WindowMovedEventArgs> Moved { get; }
    Observable<WindowMovedEventArgs> Resized { get; }
    Observable<WindowStateEventArgs> StateChanged { get; }
}
```

**Step 5: Run tests to verify they pass**

Run: `dotnet test tests/WindowManagement.Tests --filter "FullyQualifiedName~WindowFilterBuilderTests" -v quiet`
Expected: All 7 tests PASS

**Step 6: Commit**

```bash
git add -A
git commit -m "feat: add high-level interfaces and window filter builder"
```

---

## Task 6: Low-Level DisplayApi Implementation

**Files:**
- Create: `src/WindowManagement/LowLevel/Internal/DisplayApi.cs`
- Test: `tests/WindowManagement.IntegrationTests/DisplayApiIntegrationTests.cs`

**Step 1: Write integration tests**

These test against the real system — they require at least one monitor to run.

```csharp
// tests/WindowManagement.IntegrationTests/DisplayApiIntegrationTests.cs
using WindowManagement.LowLevel;
using WindowManagement.LowLevel.Internal;

namespace WindowManagement.IntegrationTests;

public class DisplayApiIntegrationTests
{
    private readonly DisplayApi _api = new();

    [Fact]
    public void GetAll__ReturnsAtLeastOneMonitor()
    {
        var monitors = _api.GetAll();

        monitors.Should().NotBeEmpty();
    }

    [Fact]
    public void GetAll__AllMonitorsHavePositiveDimensions()
    {
        var monitors = _api.GetAll();

        foreach (var m in monitors)
        {
            m.Bounds.Width.Should().BeGreaterThan(0);
            m.Bounds.Height.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public void GetPrimary__ReturnsPrimaryMonitor()
    {
        var primary = _api.GetPrimary();

        primary.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void GetAll__ExactlyOnePrimaryMonitor()
    {
        var monitors = _api.GetAll();

        monitors.Count(m => m.IsPrimary).Should().Be(1);
    }

    [Fact]
    public void GetDpi__ReturnsReasonableValue()
    {
        var primary = _api.GetPrimary();
        var dpi = _api.GetDpi(primary.Handle);

        dpi.Should().BeInRange(72, 600);
    }

    [Fact]
    public void GetScaleFactor__ReturnsReasonableValue()
    {
        var primary = _api.GetPrimary();
        var scale = _api.GetScaleFactor(primary.Handle);

        scale.Should().BeInRange(0.5, 5.0);
    }

    [Fact]
    public void GetAtPoint__CenterOfPrimary_ReturnsPrimary()
    {
        var primary = _api.GetPrimary();
        var centerX = primary.Bounds.X + primary.Bounds.Width / 2;
        var centerY = primary.Bounds.Y + primary.Bounds.Height / 2;

        var result = _api.GetAtPoint(centerX, centerY);

        result.Should().NotBeNull();
        result!.DeviceName.Should().Be(primary.DeviceName);
    }

    [Fact]
    public void IsPerMonitorV2Aware__ReturnsBoolean()
    {
        // Just verify it doesn't throw — actual value depends on test host config
        var result = _api.IsPerMonitorV2Aware();
        result.Should().Be(result); // no-op assertion, we just want no throw
    }
}
```

**Step 2: Implement DisplayApi**

```csharp
// src/WindowManagement/LowLevel/Internal/DisplayApi.cs
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
            PInvoke.EnumDisplayMonitors(null, null, (hMonitor, _, _, _) =>
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
        var context = PInvoke.GetProcessDpiAwarenessContext();
        return PInvoke.AreDpiAwarenessContextsEqual(context, DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
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
```

**Note:** The exact CsWin32-generated API signatures may differ slightly. After the scaffolding builds in Task 1, inspect the generated types (via IDE autocomplete or the `obj/` folder) and adjust the implementation to match. The key Win32 calls are correct — the struct field names or calling conventions may need minor tweaks.

**Step 3: Run integration tests**

Run: `dotnet test tests/WindowManagement.IntegrationTests --filter "FullyQualifiedName~DisplayApiIntegrationTests" -v quiet`
Expected: All tests PASS

**Step 4: Commit**

```bash
git add -A
git commit -m "feat: implement DisplayApi with multi-monitor DPI support"
```

---

## Task 7: Low-Level WindowApi Implementation

**Files:**
- Create: `src/WindowManagement/LowLevel/Internal/WindowApi.cs`
- Test: `tests/WindowManagement.IntegrationTests/WindowApiIntegrationTests.cs`

**Step 1: Write integration tests**

```csharp
// tests/WindowManagement.IntegrationTests/WindowApiIntegrationTests.cs
using WindowManagement.LowLevel;
using WindowManagement.LowLevel.Internal;

namespace WindowManagement.IntegrationTests;

public class WindowApiIntegrationTests
{
    private readonly WindowApi _api = new();

    [Fact]
    public void Enumerate__AltTabOnly_ReturnsWindows()
    {
        var windows = _api.Enumerate(altTabOnly: true);

        windows.Should().NotBeEmpty();
    }

    [Fact]
    public void Enumerate__Unfiltered_ReturnsMoreOrEqualWindows()
    {
        var altTab = _api.Enumerate(altTabOnly: true);
        var all = _api.Enumerate(altTabOnly: false);

        all.Count.Should().BeGreaterThanOrEqualTo(altTab.Count);
    }

    [Fact]
    public void GetForeground__ReturnsNonZeroHandle()
    {
        var hwnd = _api.GetForeground();

        hwnd.Should().NotBe(0);
    }

    [Fact]
    public void GetTitle__ForegroundWindow_ReturnsNonEmptyString()
    {
        var hwnd = _api.GetForeground();
        var title = _api.GetTitle(hwnd);

        title.Should().NotBeEmpty();
    }

    [Fact]
    public void GetBounds__ForegroundWindow_ReturnsNonZeroDimensions()
    {
        var hwnd = _api.GetForeground();
        var bounds = _api.GetBounds(hwnd);

        bounds.Width.Should().NotBe(0);
        bounds.Height.Should().NotBe(0);
    }

    [Fact]
    public void IsValid__ForegroundWindow_ReturnsTrue()
    {
        var hwnd = _api.GetForeground();

        _api.IsValid(hwnd).Should().BeTrue();
    }

    [Fact]
    public void IsValid__InvalidHandle_ReturnsFalse()
    {
        _api.IsValid(0xDEAD).Should().BeFalse();
    }

    [Fact]
    public void GetProcessId__ForegroundWindow_ReturnsNonZero()
    {
        var hwnd = _api.GetForeground();
        var pid = _api.GetProcessId(hwnd);

        pid.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetState__ForegroundWindow_ReturnsNormalOrMaximized()
    {
        var hwnd = _api.GetForeground();
        var state = _api.GetState(hwnd);

        state.Should().BeOneOf(WindowState.Normal, WindowState.Maximized);
    }
}
```

**Step 2: Implement WindowApi**

```csharp
// src/WindowManagement/LowLevel/Internal/WindowApi.cs
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
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
        var exStyle = (WINDOW_EX_STYLE)PInvoke.GetWindowLongPtr(new HWND(hwnd), WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        return exStyle.HasFlag(WINDOW_EX_STYLE.WS_EX_TOPMOST);
    }

    public bool IsValid(nint hwnd) => PInvoke.IsWindow(new HWND(hwnd));

    public bool IsResizable(nint hwnd)
    {
        var style = (WINDOW_STYLE)PInvoke.GetWindowLongPtr(new HWND(hwnd), WINDOW_LONG_PTR_INDEX.GWL_STYLE);
        return style.HasFlag(WINDOW_STYLE.WS_THICKFRAME);
    }

    public (int left, int top, int right, int bottom) GetInvisibleBorders(nint hwnd)
    {
        PInvoke.GetWindowRect(new HWND(hwnd), out var windowRect);

        var hr = PInvoke.DwmGetWindowAttribute(new HWND(hwnd),
            Windows.Win32.Graphics.Dwm.DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS,
            out RECT frameRect,
            (uint)Marshal.SizeOf<RECT>());

        if (hr.Failed)
            return (0, 0, 0, 0);

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
        var insertAfter = topmost ? HWND.HWND_TOPMOST : HWND.HWND_NOTOPMOST;
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
            PInvoke.GetWindowThreadProcessId(foreground, (uint*)null);
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
        PInvoke.SetWindowPos(new HWND(hwnd), HWND.HWND_TOPMOST, 0, 0, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
        PInvoke.SetWindowPos(new HWND(hwnd), HWND.HWND_NOTOPMOST, 0, 0, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
    }

    private void ThrowIfInvalid(nint hwnd)
    {
        if (!PInvoke.IsWindow(new HWND(hwnd)))
            throw new Exceptions.WindowNotFoundException(hwnd);
    }

    private bool IsAltTabWindow(HWND hwnd)
    {
        // Must be visible
        if (!PInvoke.IsWindowVisible(hwnd))
            return false;

        // Must have a title
        if (PInvoke.GetWindowTextLength(hwnd) == 0)
            return false;

        var exStyle = (WINDOW_EX_STYLE)PInvoke.GetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);

        // Tool windows never show in Alt+Tab (unless they have WS_EX_APPWINDOW)
        if (exStyle.HasFlag(WINDOW_EX_STYLE.WS_EX_TOOLWINDOW) && !exStyle.HasFlag(WINDOW_EX_STYLE.WS_EX_APPWINDOW))
            return false;

        // Owned windows don't show in Alt+Tab (unless they have WS_EX_APPWINDOW)
        var owner = PInvoke.GetWindow(hwnd, GET_WINDOW_CMD.GW_OWNER);
        if (owner != HWND.Null && !exStyle.HasFlag(WINDOW_EX_STYLE.WS_EX_APPWINDOW))
            return false;

        // Check for cloaked windows (UWP background frames)
        var hr = PInvoke.DwmGetWindowAttribute(hwnd,
            Windows.Win32.Graphics.Dwm.DWMWINDOWATTRIBUTE.DWMWA_CLOAKED,
            out int cloaked,
            (uint)sizeof(int));
        if (hr.Succeeded && cloaked != 0)
            return false;

        return true;
    }
}
```

**Step 3: Run integration tests**

Run: `dotnet test tests/WindowManagement.IntegrationTests --filter "FullyQualifiedName~WindowApiIntegrationTests" -v quiet`
Expected: All tests PASS

**Step 4: Commit**

```bash
git add -A
git commit -m "feat: implement WindowApi with Alt+Tab filtering and DPI support"
```

---

## Task 8: Internal Model Implementations

**Files:**
- Create: `src/WindowManagement/Internal/WindowInfo.cs`
- Create: `src/WindowManagement/Internal/MonitorInfo.cs`

**Step 1: Implement internal models**

```csharp
// src/WindowManagement/Internal/MonitorInfo.cs
namespace WindowManagement.Internal;

internal class MonitorInfo : IMonitor
{
    public required nint Handle { get; init; }
    public required string DeviceName { get; init; }
    public required string DisplayName { get; init; }
    public required bool IsPrimary { get; init; }
    public required WindowRect Bounds { get; init; }
    public required WindowRect WorkArea { get; init; }
    public required int Dpi { get; init; }
    public required double ScaleFactor { get; init; }
}
```

```csharp
// src/WindowManagement/Internal/WindowInfo.cs
namespace WindowManagement.Internal;

internal class WindowInfo : IWindow
{
    public required nint Handle { get; init; }
    public required string Title { get; init; }
    public required string ProcessName { get; init; }
    public required int ProcessId { get; init; }
    public required string ClassName { get; init; }
    public required WindowRect Bounds { get; init; }
    public required WindowState State { get; init; }
    public required IMonitor Monitor { get; init; }
    public required bool IsTopmost { get; init; }
}
```

**Step 2: Verify it builds**

Run: `dotnet build src/WindowManagement/WindowManagement.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add -A
git commit -m "feat: add internal WindowInfo and MonitorInfo implementations"
```

---

## Task 9: MonitorService Implementation

**Files:**
- Create: `src/WindowManagement/Internal/MonitorService.cs`
- Test: `tests/WindowManagement.Tests/MonitorServiceTests.cs`

**Step 1: Write unit tests**

```csharp
// tests/WindowManagement.Tests/MonitorServiceTests.cs
using NSubstitute;
using WindowManagement.Internal;
using WindowManagement.LowLevel;

namespace WindowManagement.Tests;

public class MonitorServiceTests
{
    private readonly IDisplayApi _displayApi = Substitute.For<IDisplayApi>();
    private readonly MonitorService _service;

    public MonitorServiceTests()
    {
        var primary = new DisplayInfo(1, @"\\.\DISPLAY1", "Monitor 1", true,
            new WindowRect(0, 0, 1920, 1080), new WindowRect(0, 0, 1920, 1040), 96, 1.0);
        var secondary = new DisplayInfo(2, @"\\.\DISPLAY2", "Monitor 2", false,
            new WindowRect(1920, 0, 2560, 1440), new WindowRect(1920, 0, 2560, 1400), 144, 1.5);

        _displayApi.GetAll().Returns([primary, secondary]);
        _displayApi.GetPrimary().Returns(primary);

        _service = new MonitorService(_displayApi);
    }

    [Fact]
    public void All__ReturnsBothMonitors()
    {
        _service.All.Should().HaveCount(2);
    }

    [Fact]
    public void Primary__ReturnsPrimaryMonitor()
    {
        _service.Primary.IsPrimary.Should().BeTrue();
        _service.Primary.DeviceName.Should().Be(@"\\.\DISPLAY1");
    }

    [Fact]
    public void GetAt__CenterOfPrimary_ReturnsPrimaryMonitor()
    {
        _displayApi.GetAtPoint(960, 540).Returns(
            new DisplayInfo(1, @"\\.\DISPLAY1", "Monitor 1", true,
                new WindowRect(0, 0, 1920, 1080), new WindowRect(0, 0, 1920, 1040), 96, 1.0));

        var result = _service.GetAt(960, 540);

        result.Should().NotBeNull();
        result!.DeviceName.Should().Be(@"\\.\DISPLAY1");
    }

    [Fact]
    public void GetAt__PointOutsideAllMonitors_ReturnsNull()
    {
        _displayApi.GetAtPoint(-9999, -9999).Returns((DisplayInfo?)null);

        var result = _service.GetAt(-9999, -9999);

        result.Should().BeNull();
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/WindowManagement.Tests --filter "FullyQualifiedName~MonitorServiceTests" -v quiet`
Expected: FAIL

**Step 3: Implement MonitorService**

```csharp
// src/WindowManagement/Internal/MonitorService.cs
using Microsoft.Win32;
using R3;
using WindowManagement.LowLevel;

namespace WindowManagement.Internal;

internal class MonitorService : IMonitorService, IDisposable
{
    private readonly IDisplayApi _displayApi;
    private readonly Subject<MonitorEventArgs> _connected = new();
    private readonly Subject<MonitorEventArgs> _disconnected = new();
    private readonly Subject<MonitorEventArgs> _settingsChanged = new();
    private List<IMonitor>? _cachedMonitors;

    public MonitorService(IDisplayApi displayApi)
    {
        _displayApi = displayApi;
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    public IReadOnlyList<IMonitor> All => _cachedMonitors ??= LoadMonitors();

    public IMonitor Primary => All.First(m => m.IsPrimary);

    public IMonitor GetFor(IWindow window)
    {
        var display = _displayApi.GetForWindow(window.Handle);
        return ToMonitor(display);
    }

    public IMonitor? GetAt(int x, int y)
    {
        var display = _displayApi.GetAtPoint(x, y);
        return display != null ? ToMonitor(display) : null;
    }

    public Observable<MonitorEventArgs> Connected => _connected;
    public Observable<MonitorEventArgs> Disconnected => _disconnected;
    public Observable<MonitorEventArgs> SettingsChanged => _settingsChanged;

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        var oldMonitors = _cachedMonitors ?? [];
        _cachedMonitors = null; // Force reload
        var newMonitors = All;

        var oldNames = oldMonitors.Select(m => m.DeviceName).ToHashSet();
        var newNames = newMonitors.Select(m => m.DeviceName).ToHashSet();

        foreach (var monitor in newMonitors.Where(m => !oldNames.Contains(m.DeviceName)))
            _connected.OnNext(new MonitorEventArgs { DeviceName = monitor.DeviceName, Bounds = monitor.Bounds });

        foreach (var monitor in oldMonitors.Where(m => !newNames.Contains(m.DeviceName)))
            _disconnected.OnNext(new MonitorEventArgs { DeviceName = monitor.DeviceName, Bounds = monitor.Bounds });

        foreach (var monitor in newMonitors.Where(m => oldNames.Contains(m.DeviceName)))
            _settingsChanged.OnNext(new MonitorEventArgs { DeviceName = monitor.DeviceName, Bounds = monitor.Bounds });
    }

    private List<IMonitor> LoadMonitors()
    {
        return _displayApi.GetAll().Select(ToMonitor).ToList<IMonitor>();
    }

    private static IMonitor ToMonitor(DisplayInfo d) => new MonitorInfo
    {
        Handle = d.Handle,
        DeviceName = d.DeviceName,
        DisplayName = d.DisplayName,
        IsPrimary = d.IsPrimary,
        Bounds = d.Bounds,
        WorkArea = d.WorkArea,
        Dpi = d.Dpi,
        ScaleFactor = d.ScaleFactor
    };

    public void Dispose()
    {
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        _connected.Dispose();
        _disconnected.Dispose();
        _settingsChanged.Dispose();
    }
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/WindowManagement.Tests --filter "FullyQualifiedName~MonitorServiceTests" -v quiet`
Expected: All 4 tests PASS

**Step 5: Commit**

```bash
git add -A
git commit -m "feat: implement MonitorService with display change events"
```

---

## Task 10: WindowManager Implementation

**Files:**
- Create: `src/WindowManagement/Internal/WindowManager.cs`
- Test: `tests/WindowManagement.Tests/WindowManagerTests.cs`
- Test: `tests/WindowManagement.Tests/WindowManagerSnapTests.cs`

**Step 1: Write unit tests for core operations**

```csharp
// tests/WindowManagement.Tests/WindowManagerTests.cs
using NSubstitute;
using WindowManagement.Exceptions;
using WindowManagement.Filtering;
using WindowManagement.Internal;
using WindowManagement.LowLevel;

namespace WindowManagement.Tests;

public class WindowManagerTests
{
    private readonly IWindowApi _windowApi = Substitute.For<IWindowApi>();
    private readonly IDisplayApi _displayApi = Substitute.For<IDisplayApi>();
    private readonly WindowManager _manager;

    public WindowManagerTests()
    {
        var primaryDisplay = new DisplayInfo(1, @"\\.\DISPLAY1", "Monitor 1", true,
            new WindowRect(0, 0, 1920, 1080), new WindowRect(0, 0, 1920, 1040), 96, 1.0);

        _displayApi.GetAll().Returns([primaryDisplay]);
        _displayApi.GetPrimary().Returns(primaryDisplay);
        _displayApi.GetForWindow(Arg.Any<nint>()).Returns(primaryDisplay);
        _displayApi.IsPerMonitorV2Aware().Returns(true);

        _manager = new WindowManager(_windowApi, _displayApi);
    }

    [Fact]
    public void Constructor__NotDpiAware_ThrowsDpiAwarenessException()
    {
        _displayApi.IsPerMonitorV2Aware().Returns(false);

        var act = () => new WindowManager(_windowApi, _displayApi, enforceDpiAwareness: true);

        act.Should().Throw<DpiAwarenessException>();
    }

    [Fact]
    public void Constructor__DpiAwarenessFalse_DoesNotThrow()
    {
        _displayApi.IsPerMonitorV2Aware().Returns(false);

        var act = () => new WindowManager(_windowApi, _displayApi, enforceDpiAwareness: false);

        act.Should().NotThrow();
    }

    [Fact]
    public void GetAll__ReturnsWindowsFromApi()
    {
        _windowApi.Enumerate(true).Returns([1, 2]);
        SetupWindowHandle(1, "Notepad", "notepad", "Notepad", WindowState.Normal);
        SetupWindowHandle(2, "Calculator", "calc", "CalcFrame", WindowState.Normal);

        var windows = _manager.GetAll();

        windows.Should().HaveCount(2);
    }

    [Fact]
    public void GetAll__WithProcessFilter_FiltersCorrectly()
    {
        _windowApi.Enumerate(true).Returns([1, 2]);
        SetupWindowHandle(1, "Notepad", "notepad", "Notepad", WindowState.Normal);
        SetupWindowHandle(2, "Calculator", "calc", "CalcFrame", WindowState.Normal);

        var windows = _manager.GetAll(f => f.WithProcess("notepad"));

        windows.Should().HaveCount(1);
        windows[0].ProcessName.Should().Be("notepad");
    }

    [Fact]
    public void GetAll__WithTitleWildcard_FiltersCorrectly()
    {
        _windowApi.Enumerate(true).Returns([1, 2]);
        SetupWindowHandle(1, "readme.txt - Notepad", "notepad", "Notepad", WindowState.Normal);
        SetupWindowHandle(2, "Calculator", "calc", "CalcFrame", WindowState.Normal);

        var windows = _manager.GetAll(f => f.WithTitle("*.txt*"));

        windows.Should().HaveCount(1);
        windows[0].Title.Should().Contain(".txt");
    }

    [Fact]
    public void GetAll__ExcludeMinimized_FiltersCorrectly()
    {
        _windowApi.Enumerate(true).Returns([1, 2]);
        SetupWindowHandle(1, "Notepad", "notepad", "Notepad", WindowState.Normal);
        SetupWindowHandle(2, "Calculator", "calc", "CalcFrame", WindowState.Minimized);

        var windows = _manager.GetAll(f => f.ExcludeMinimized());

        windows.Should().HaveCount(1);
    }

    [Fact]
    public async Task MoveAsync__ValidWindow_CallsWindowApi()
    {
        _windowApi.IsValid(1).Returns(true);
        var window = CreateWindow(1);

        await _manager.MoveAsync(window, 100, 200);

        _windowApi.Received(1).Move(1, 100, 200);
    }

    [Fact]
    public async Task MoveAsync__InvalidWindow_ThrowsWindowNotFoundException()
    {
        _windowApi.IsValid(1).Returns(false);
        _windowApi.When(x => x.Move(1, Arg.Any<int>(), Arg.Any<int>()))
            .Do(_ => throw new WindowNotFoundException(1));
        var window = CreateWindow(1);

        var act = () => _manager.MoveAsync(window, 100, 200);

        await act.Should().ThrowAsync<WindowNotFoundException>();
    }

    private void SetupWindowHandle(nint hwnd, string title, string processName, string className, WindowState state)
    {
        _windowApi.GetTitle(hwnd).Returns(title);
        _windowApi.GetClassName(hwnd).Returns(className);
        _windowApi.GetProcessId(hwnd).Returns((int)hwnd * 1000);
        _windowApi.GetBounds(hwnd).Returns(new WindowRect(0, 0, 800, 600));
        _windowApi.GetState(hwnd).Returns(state);
        _windowApi.IsTopmost(hwnd).Returns(false);
        _windowApi.IsValid(hwnd).Returns(true);

        // Map process ID to process name via a helper
        // For unit tests, we simulate this by having the manager look up process names
        // The actual mapping is done using Process.GetProcessById in production
    }

    private IWindow CreateWindow(nint hwnd) => new WindowInfo
    {
        Handle = hwnd,
        Title = _windowApi.GetTitle(hwnd),
        ProcessName = "notepad",
        ProcessId = _windowApi.GetProcessId(hwnd),
        ClassName = _windowApi.GetClassName(hwnd),
        Bounds = _windowApi.GetBounds(hwnd),
        State = _windowApi.GetState(hwnd),
        Monitor = new MonitorInfo
        {
            Handle = 1, DeviceName = @"\\.\DISPLAY1", DisplayName = "Monitor 1",
            IsPrimary = true, Bounds = new WindowRect(0, 0, 1920, 1080),
            WorkArea = new WindowRect(0, 0, 1920, 1040), Dpi = 96, ScaleFactor = 1.0
        },
        IsTopmost = false
    };
}
```

**Step 2: Write snap calculation tests**

```csharp
// tests/WindowManagement.Tests/WindowManagerSnapTests.cs
using WindowManagement.Internal;

namespace WindowManagement.Tests;

public class WindowManagerSnapTests
{
    [Theory]
    [InlineData(SnapPosition.Fill, 0, 0, 1920, 1040)]
    [InlineData(SnapPosition.Left, 0, 0, 960, 1040)]
    [InlineData(SnapPosition.Right, 960, 0, 960, 1040)]
    [InlineData(SnapPosition.Top, 0, 0, 1920, 520)]
    [InlineData(SnapPosition.Bottom, 0, 520, 1920, 520)]
    [InlineData(SnapPosition.TopLeft, 0, 0, 960, 520)]
    [InlineData(SnapPosition.TopRight, 960, 0, 960, 520)]
    [InlineData(SnapPosition.BottomLeft, 0, 520, 960, 520)]
    [InlineData(SnapPosition.BottomRight, 960, 520, 960, 520)]
    public void CalculateSnapBounds__ReturnsCorrectBounds(SnapPosition position, int x, int y, int w, int h)
    {
        var workArea = new WindowRect(0, 0, 1920, 1040);

        var result = SnapCalculator.Calculate(workArea, position);

        result.Should().Be(new WindowRect(x, y, w, h));
    }

    [Theory]
    [InlineData(SnapPosition.Left, 1920, 0, 1280, 1400)]
    [InlineData(SnapPosition.Right, 3200, 0, 1280, 1400)]
    public void CalculateSnapBounds__SecondMonitor_CorrectOffset(SnapPosition position, int x, int y, int w, int h)
    {
        var workArea = new WindowRect(1920, 0, 2560, 1400);

        var result = SnapCalculator.Calculate(workArea, position);

        result.Should().Be(new WindowRect(x, y, w, h));
    }
}
```

**Step 3: Run tests to verify they fail**

Run: `dotnet test tests/WindowManagement.Tests --filter "FullyQualifiedName~WindowManager" -v quiet`
Expected: FAIL

**Step 4: Implement SnapCalculator**

```csharp
// src/WindowManagement/Internal/SnapCalculator.cs
namespace WindowManagement.Internal;

internal static class SnapCalculator
{
    public static WindowRect Calculate(WindowRect workArea, SnapPosition position)
    {
        var halfWidth = workArea.Width / 2;
        var halfHeight = workArea.Height / 2;

        return position switch
        {
            SnapPosition.Fill => workArea,

            SnapPosition.Left => new WindowRect(
                workArea.X, workArea.Y, halfWidth, workArea.Height),

            SnapPosition.Right => new WindowRect(
                workArea.X + halfWidth, workArea.Y, workArea.Width - halfWidth, workArea.Height),

            SnapPosition.Top => new WindowRect(
                workArea.X, workArea.Y, workArea.Width, halfHeight),

            SnapPosition.Bottom => new WindowRect(
                workArea.X, workArea.Y + halfHeight, workArea.Width, workArea.Height - halfHeight),

            SnapPosition.TopLeft => new WindowRect(
                workArea.X, workArea.Y, halfWidth, halfHeight),

            SnapPosition.TopRight => new WindowRect(
                workArea.X + halfWidth, workArea.Y, workArea.Width - halfWidth, halfHeight),

            SnapPosition.BottomLeft => new WindowRect(
                workArea.X, workArea.Y + halfHeight, halfWidth, workArea.Height - halfHeight),

            SnapPosition.BottomRight => new WindowRect(
                workArea.X + halfWidth, workArea.Y + halfHeight, workArea.Width - halfWidth, workArea.Height - halfHeight),

            _ => workArea
        };
    }
}
```

**Step 5: Implement WindowManager**

```csharp
// src/WindowManagement/Internal/WindowManager.cs
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using R3;
using WindowManagement.Exceptions;
using WindowManagement.Filtering;
using WindowManagement.LowLevel;

namespace WindowManagement.Internal;

internal class WindowManager : IWindowManager
{
    private readonly IWindowApi _windowApi;
    private readonly IDisplayApi _displayApi;
    private readonly MonitorService _monitorService;
    private readonly WindowEventHook _eventHook;
    private readonly ILogger? _logger;

    public IMonitorService Monitors => _monitorService;

    public Observable<WindowEventArgs> Created => _eventHook.Created;
    public Observable<WindowEventArgs> Destroyed => _eventHook.Destroyed;
    public Observable<WindowMovedEventArgs> Moved => _eventHook.Moved;
    public Observable<WindowMovedEventArgs> Resized => _eventHook.Resized;
    public Observable<WindowStateEventArgs> StateChanged => _eventHook.StateChanged;

    public WindowManager(IWindowApi windowApi, IDisplayApi displayApi, bool enforceDpiAwareness = true, ILogger<WindowManager>? logger = null)
    {
        if (enforceDpiAwareness && !displayApi.IsPerMonitorV2Aware())
            throw new DpiAwarenessException();

        _windowApi = windowApi;
        _displayApi = displayApi;
        _monitorService = new MonitorService(displayApi);
        _eventHook = new WindowEventHook(windowApi, displayApi);
        _logger = logger;
    }

    public IReadOnlyList<IWindow> GetAll(WindowFilter? filter = null)
    {
        filter ??= new WindowFilter();
        var handles = _windowApi.Enumerate(filter.AltTabOnly);

        var windows = new List<IWindow>();
        foreach (var hwnd in handles)
        {
            var window = BuildWindow(hwnd);
            if (window == null) continue;
            if (!MatchesFilter(window, filter)) continue;
            windows.Add(window);
        }

        return windows;
    }

    public IReadOnlyList<IWindow> GetAll(Action<WindowFilterBuilder> configure)
    {
        var builder = new WindowFilterBuilder();
        configure(builder);
        return GetAll(builder.Build());
    }

    public IWindow? GetForeground()
    {
        var hwnd = _windowApi.GetForeground();
        return hwnd == 0 ? null : BuildWindow(hwnd);
    }

    public Task MoveAsync(IWindow window, int x, int y)
    {
        _windowApi.Move(window.Handle, x, y);
        return Task.CompletedTask;
    }

    public Task ResizeAsync(IWindow window, int width, int height)
    {
        _windowApi.Resize(window.Handle, width, height);
        return Task.CompletedTask;
    }

    public Task SetBoundsAsync(IWindow window, WindowRect bounds)
    {
        _windowApi.SetBounds(window.Handle, bounds);
        return Task.CompletedTask;
    }

    public Task MoveToMonitorAsync(IWindow window, IMonitor monitor)
    {
        var workArea = monitor.WorkArea;
        _windowApi.SetBounds(window.Handle, new WindowRect(workArea.X, workArea.Y, window.Bounds.Width, window.Bounds.Height));
        return Task.CompletedTask;
    }

    public Task SetStateAsync(IWindow window, WindowState state)
    {
        _windowApi.SetState(window.Handle, state);
        return Task.CompletedTask;
    }

    public Task FocusAsync(IWindow window)
    {
        _windowApi.Focus(window.Handle);
        return Task.CompletedTask;
    }

    public Task SnapAsync(IWindow window, IMonitor monitor, SnapPosition position)
    {
        var targetBounds = SnapCalculator.Calculate(monitor.WorkArea, position);

        // Restore if minimized or maximized
        var state = _windowApi.GetState(window.Handle);
        if (state != WindowState.Normal)
            _windowApi.RestoreMinimized(window.Handle);

        // Handle non-resizable windows: center in zone
        if (!_windowApi.IsResizable(window.Handle))
        {
            var currentBounds = _windowApi.GetBounds(window.Handle);
            var x = targetBounds.X + (targetBounds.Width - currentBounds.Width) / 2;
            var y = targetBounds.Y + (targetBounds.Height - currentBounds.Height) / 2;
            _windowApi.SetBounds(window.Handle, new WindowRect(x, y, currentBounds.Width, currentBounds.Height));
            return Task.CompletedTask;
        }

        // Compensate for invisible borders
        var borders = _windowApi.GetInvisibleBorders(window.Handle);
        var adjusted = new WindowRect(
            targetBounds.X - borders.left,
            targetBounds.Y - borders.top,
            targetBounds.Width + borders.left + borders.right,
            targetBounds.Height + borders.top + borders.bottom);

        // Cross-monitor DPI compensation
        var windowDpi = _windowApi.GetDpi(window.Handle);
        var targetDpi = (uint)monitor.Dpi;

        if (windowDpi > 0 && targetDpi > 0 && windowDpi != targetDpi)
        {
            double dpiRatio = (double)windowDpi / targetDpi;
            adjusted = new WindowRect(
                adjusted.X,
                adjusted.Y,
                (int)Math.Round(adjusted.Width * dpiRatio),
                (int)Math.Round(adjusted.Height * dpiRatio));
        }

        _windowApi.SetBounds(window.Handle, adjusted);
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _eventHook.Dispose();
        _monitorService.Dispose();
    }

    private WindowInfo? BuildWindow(nint hwnd)
    {
        try
        {
            var title = _windowApi.GetTitle(hwnd);
            var pid = _windowApi.GetProcessId(hwnd);
            var processName = GetProcessName(pid);

            return new WindowInfo
            {
                Handle = hwnd,
                Title = title,
                ProcessName = processName,
                ProcessId = pid,
                ClassName = _windowApi.GetClassName(hwnd),
                Bounds = _windowApi.GetBounds(hwnd),
                State = _windowApi.GetState(hwnd),
                Monitor = _monitorService.GetFor(new WindowInfo
                {
                    Handle = hwnd, Title = title, ProcessName = processName,
                    ProcessId = pid, ClassName = "", Bounds = new WindowRect(0, 0, 0, 0),
                    State = WindowState.Normal, Monitor = null!, IsTopmost = false
                }),
                IsTopmost = _windowApi.IsTopmost(hwnd)
            };
        }
        catch
        {
            return null;
        }
    }

    private static string GetProcessName(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return process.ProcessName;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool MatchesFilter(IWindow window, WindowFilter filter)
    {
        if (filter.ProcessName != null &&
            !window.ProcessName.Equals(filter.ProcessName, StringComparison.OrdinalIgnoreCase))
            return false;

        if (filter.TitlePattern != null &&
            !MatchesWildcard(window.Title, filter.TitlePattern))
            return false;

        if (filter.ClassName != null &&
            !window.ClassName.Equals(filter.ClassName, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!filter.IncludeMinimized && window.State == WindowState.Minimized)
            return false;

        if (filter.Predicate != null && !filter.Predicate(window))
            return false;

        return true;
    }

    private static bool MatchesWildcard(string text, string pattern)
    {
        // Simple wildcard matching using regex
        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        return System.Text.RegularExpressions.Regex.IsMatch(text, regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}
```

**Step 6: Run tests to verify they pass**

Run: `dotnet test tests/WindowManagement.Tests --filter "FullyQualifiedName~WindowManager" -v quiet`
Expected: All tests PASS

**Step 7: Commit**

```bash
git add -A
git commit -m "feat: implement WindowManager with snap, DPI compensation, and filtering"
```

---

## Task 11: Window Event Hook (R3 Observables)

**Files:**
- Create: `src/WindowManagement/Internal/WindowEventHook.cs`
- Test: `tests/WindowManagement.Tests/WindowEventHookTests.cs`

**Step 1: Write tests**

```csharp
// tests/WindowManagement.Tests/WindowEventHookTests.cs
using NSubstitute;
using WindowManagement.Internal;
using WindowManagement.LowLevel;

namespace WindowManagement.Tests;

public class WindowEventHookTests
{
    [Fact]
    public void Created__IsObservable()
    {
        var windowApi = Substitute.For<IWindowApi>();
        var displayApi = Substitute.For<IDisplayApi>();
        using var hook = new WindowEventHook(windowApi, displayApi);

        hook.Created.Should().NotBeNull();
    }

    [Fact]
    public void Dispose__CompletesAllStreams()
    {
        var windowApi = Substitute.For<IWindowApi>();
        var displayApi = Substitute.For<IDisplayApi>();
        var hook = new WindowEventHook(windowApi, displayApi);

        bool completed = false;
        hook.Created.Subscribe(_ => { }, result => completed = true);

        hook.Dispose();

        completed.Should().BeTrue();
    }
}
```

**Step 2: Implement WindowEventHook**

```csharp
// src/WindowManagement/Internal/WindowEventHook.cs
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
                WINEVENT_OUTOFCONTEXT);
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
```

**Note:** The exact CsWin32-generated types for `SetWinEventHook` (callback delegate type, return handle type) may differ from what's shown. After Task 1 builds, check the generated API via IDE and adjust. The event constants and logic are correct.

**Step 3: Run tests**

Run: `dotnet test tests/WindowManagement.Tests --filter "FullyQualifiedName~WindowEventHookTests" -v quiet`
Expected: All tests PASS

**Step 4: Commit**

```bash
git add -A
git commit -m "feat: implement window event hook with R3 observables"
```

---

## Task 12: DI Registration and Factory

**Files:**
- Create: `src/WindowManagement/DependencyInjection/WindowManagementOptions.cs`
- Create: `src/WindowManagement/DependencyInjection/ServiceCollectionExtensions.cs`
- Create: `src/WindowManagement/DependencyInjection/WindowManagementModule.cs`
- Create: `src/WindowManagement/WindowManagementFactory.cs`
- Test: `tests/WindowManagement.Tests/DependencyInjection/ServiceCollectionExtensionsTests.cs`
- Test: `tests/WindowManagement.Tests/DependencyInjection/WindowManagementModuleTests.cs`
- Test: `tests/WindowManagement.Tests/WindowManagementFactoryTests.cs`

**Step 1: Write DI tests**

```csharp
// tests/WindowManagement.Tests/DependencyInjection/ServiceCollectionExtensionsTests.cs
using Microsoft.Extensions.DependencyInjection;
using WindowManagement.DependencyInjection;
using WindowManagement.LowLevel;

namespace WindowManagement.Tests.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddWindowManagement__RegistersAllInterfaces()
    {
        var services = new ServiceCollection();

        services.AddWindowManagement(opts => opts.EnforceDpiAwareness = false);

        var provider = services.BuildServiceProvider();
        provider.GetService<IWindowApi>().Should().NotBeNull();
        provider.GetService<IDisplayApi>().Should().NotBeNull();
        provider.GetService<IWindowManager>().Should().NotBeNull();
        provider.GetService<IMonitorService>().Should().NotBeNull();
    }

    [Fact]
    public void AddWindowManagement__WindowManagerIsSingleton()
    {
        var services = new ServiceCollection();
        services.AddWindowManagement(opts => opts.EnforceDpiAwareness = false);
        var provider = services.BuildServiceProvider();

        var a = provider.GetService<IWindowManager>();
        var b = provider.GetService<IWindowManager>();

        a.Should().BeSameAs(b);
    }
}
```

```csharp
// tests/WindowManagement.Tests/DependencyInjection/WindowManagementModuleTests.cs
using Autofac;
using WindowManagement.DependencyInjection;
using WindowManagement.LowLevel;

namespace WindowManagement.Tests.DependencyInjection;

public class WindowManagementModuleTests
{
    [Fact]
    public void RegisterModule__RegistersAllInterfaces()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule(new WindowManagementModule(opts => opts.EnforceDpiAwareness = false));
        var container = builder.Build();

        container.Resolve<IWindowApi>().Should().NotBeNull();
        container.Resolve<IDisplayApi>().Should().NotBeNull();
        container.Resolve<IWindowManager>().Should().NotBeNull();
        container.Resolve<IMonitorService>().Should().NotBeNull();
    }
}
```

```csharp
// tests/WindowManagement.Tests/WindowManagementFactoryTests.cs
namespace WindowManagement.Tests;

public class WindowManagementFactoryTests
{
    [Fact]
    public void Create__ReturnsWindowManager()
    {
        var options = new DependencyInjection.WindowManagementOptions { EnforceDpiAwareness = false };

        var manager = WindowManagementFactory.Create(options);

        manager.Should().NotBeNull();
        manager.Should().BeAssignableTo<IWindowManager>();
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/WindowManagement.Tests --filter "FullyQualifiedName~ServiceCollectionExtensionsTests|FullyQualifiedName~WindowManagementModuleTests|FullyQualifiedName~WindowManagementFactoryTests" -v quiet`
Expected: FAIL

**Step 3: Implement options**

```csharp
// src/WindowManagement/DependencyInjection/WindowManagementOptions.cs
namespace WindowManagement.DependencyInjection;

public class WindowManagementOptions
{
    public bool EnforceDpiAwareness { get; set; } = true;
}
```

**Step 4: Implement ServiceCollectionExtensions**

```csharp
// src/WindowManagement/DependencyInjection/ServiceCollectionExtensions.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using WindowManagement.Internal;
using WindowManagement.LowLevel;
using WindowManagement.LowLevel.Internal;

namespace WindowManagement.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWindowManagement(
        this IServiceCollection services,
        Action<WindowManagementOptions>? configure = null)
    {
        var options = new WindowManagementOptions();
        configure?.Invoke(options);

        services.TryAddSingleton<IWindowApi>(_ => new WindowApi());
        services.TryAddSingleton<IDisplayApi>(_ => new DisplayApi());

        services.TryAddSingleton<IWindowManager>(sp =>
        {
            var windowApi = sp.GetRequiredService<IWindowApi>();
            var displayApi = sp.GetRequiredService<IDisplayApi>();
            var logger = sp.GetService<ILogger<WindowManager>>();
            return new WindowManager(windowApi, displayApi, options.EnforceDpiAwareness, logger);
        });

        services.TryAddSingleton<IMonitorService>(sp =>
        {
            var manager = sp.GetRequiredService<IWindowManager>();
            return manager.Monitors;
        });

        return services;
    }
}
```

**Step 5: Implement Autofac module**

```csharp
// src/WindowManagement/DependencyInjection/WindowManagementModule.cs
using Autofac;
using Microsoft.Extensions.Logging;
using WindowManagement.Internal;
using WindowManagement.LowLevel;
using WindowManagement.LowLevel.Internal;

namespace WindowManagement.DependencyInjection;

public class WindowManagementModule : Module
{
    private readonly Action<WindowManagementOptions>? _configure;

    public WindowManagementModule(Action<WindowManagementOptions>? configure = null)
    {
        _configure = configure;
    }

    protected override void Load(ContainerBuilder builder)
    {
        var options = new WindowManagementOptions();
        _configure?.Invoke(options);

        builder.RegisterType<WindowApi>().As<IWindowApi>().SingleInstance();
        builder.RegisterType<DisplayApi>().As<IDisplayApi>().SingleInstance();

        builder.Register(ctx =>
        {
            var windowApi = ctx.Resolve<IWindowApi>();
            var displayApi = ctx.Resolve<IDisplayApi>();
            var logger = ctx.ResolveOptional<ILogger<WindowManager>>();
            return new WindowManager(windowApi, displayApi, options.EnforceDpiAwareness, logger);
        }).As<IWindowManager>().SingleInstance();

        builder.Register(ctx => ctx.Resolve<IWindowManager>().Monitors)
            .As<IMonitorService>().SingleInstance();
    }
}
```

**Step 6: Implement factory**

```csharp
// src/WindowManagement/WindowManagementFactory.cs
using Microsoft.Extensions.Logging;
using WindowManagement.DependencyInjection;
using WindowManagement.Internal;
using WindowManagement.LowLevel.Internal;

namespace WindowManagement;

public static class WindowManagementFactory
{
    public static IWindowManager Create(
        WindowManagementOptions? options = null,
        ILoggerFactory? loggerFactory = null)
    {
        options ??= new WindowManagementOptions();
        var windowApi = new WindowApi();
        var displayApi = new DisplayApi();
        var logger = loggerFactory?.CreateLogger<WindowManager>();
        return new WindowManager(windowApi, displayApi, options.EnforceDpiAwareness, logger);
    }
}
```

**Step 7: Run tests**

Run: `dotnet test tests/WindowManagement.Tests --filter "FullyQualifiedName~ServiceCollectionExtensionsTests|FullyQualifiedName~WindowManagementModuleTests|FullyQualifiedName~WindowManagementFactoryTests" -v quiet`
Expected: All tests PASS

**Step 8: Commit**

```bash
git add -A
git commit -m "feat: add DI registration, Autofac module, and factory"
```

---

## Task 13: Integration Tests

**Files:**
- Test: `tests/WindowManagement.IntegrationTests/WindowManagerIntegrationTests.cs`

**Step 1: Write end-to-end integration tests**

```csharp
// tests/WindowManagement.IntegrationTests/WindowManagerIntegrationTests.cs
using WindowManagement.DependencyInjection;

namespace WindowManagement.IntegrationTests;

public class WindowManagerIntegrationTests : IAsyncDisposable
{
    private readonly IWindowManager _manager;

    public WindowManagerIntegrationTests()
    {
        _manager = WindowManagementFactory.Create(new WindowManagementOptions
        {
            EnforceDpiAwareness = false
        });
    }

    [Fact]
    public void GetAll__ReturnsVisibleWindows()
    {
        var windows = _manager.GetAll();

        windows.Should().NotBeEmpty();
    }

    [Fact]
    public void GetAll__AllWindowsHaveTitles()
    {
        var windows = _manager.GetAll();

        foreach (var w in windows)
            w.Title.Should().NotBeEmpty();
    }

    [Fact]
    public void GetAll__AllWindowsHaveMonitors()
    {
        var windows = _manager.GetAll();

        foreach (var w in windows)
            w.Monitor.Should().NotBeNull();
    }

    [Fact]
    public void GetForeground__ReturnsAWindow()
    {
        var window = _manager.GetForeground();

        window.Should().NotBeNull();
        window!.Title.Should().NotBeEmpty();
    }

    [Fact]
    public void Monitors_All__ReturnsMonitors()
    {
        var monitors = _manager.Monitors.All;

        monitors.Should().NotBeEmpty();
    }

    [Fact]
    public void Monitors_Primary__ReturnsPrimary()
    {
        var primary = _manager.Monitors.Primary;

        primary.IsPrimary.Should().BeTrue();
        primary.Dpi.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetAll__Unfiltered_ReturnsMoreWindows()
    {
        var altTab = _manager.GetAll();
        var all = _manager.GetAll(f => f.Unfiltered());

        all.Count.Should().BeGreaterThanOrEqualTo(altTab.Count);
    }

    [Fact]
    public void GetAll__WithProcessFilter_FiltersCorrectly()
    {
        var foreground = _manager.GetForeground();
        if (foreground == null) return;

        var filtered = _manager.GetAll(f => f.WithProcess(foreground.ProcessName));

        filtered.Should().Contain(w => w.ProcessName == foreground.ProcessName);
    }

    public async ValueTask DisposeAsync()
    {
        await _manager.DisposeAsync();
    }
}
```

**Step 2: Run integration tests**

Run: `dotnet test tests/WindowManagement.IntegrationTests -v quiet`
Expected: All tests PASS

**Step 3: Commit**

```bash
git add -A
git commit -m "feat: add end-to-end integration tests"
```

---

## Task 14: Final Polish

**Files:**
- Create: `README.md`

**Step 1: Create README**

```markdown
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
```

**Step 2: Run all tests**

Run: `dotnet test WindowManagementApi.slnx -v quiet`
Expected: All tests PASS

**Step 3: Commit**

```bash
git add -A
git commit -m "feat: add README with usage examples"
```

---

## Build Order Summary

| Task | Component | Depends On |
|------|-----------|------------|
| 1 | Project scaffolding | — |
| 2 | Models and enums | 1 |
| 3 | Exceptions | 1 |
| 4 | Low-level interfaces | 2 |
| 5 | High-level interfaces + filter builder | 2, 4 |
| 6 | DisplayApi implementation | 1, 4 |
| 7 | WindowApi implementation | 2, 3, 4 |
| 8 | Internal model implementations | 5 |
| 9 | MonitorService | 4, 5, 8 |
| 10 | WindowManager | 3, 5, 7, 8, 9 |
| 11 | Window event hook | 2, 7, 10 |
| 12 | DI registration + factory | 6, 7, 9, 10 |
| 13 | Integration tests | 12 |
| 14 | README | 13 |

## Implementation Notes

### CsWin32 API Adjustments

The CsWin32 source generator produces types that may differ slightly from the code samples in this plan. After Task 1 builds successfully, inspect the generated types in `obj/` or via IDE autocomplete. Common adjustments needed:

- **Struct field names**: CsWin32 may use different casing (e.g., `left` vs `Left` on `RECT`)
- **Enum flag names**: Check exact enum member names for `WINDOW_STYLE`, `WINDOW_EX_STYLE`, `SET_WINDOW_POS_FLAGS`
- **Handle types**: `HWND` and `HMONITOR` have implicit conversions to/from `nint`, but the exact API may vary
- **Callback delegates**: `SetWinEventHook` callback type may be `WINEVENTPROC` or similar — check the generated delegate signature
- **Unsafe contexts**: Some APIs require `unsafe` blocks for pointer parameters — CsWin32 generates both safe and unsafe overloads

### Testing Strategy

- **Unit tests** (WindowManagement.Tests): Test high-level logic via mocked `IWindowApi`/`IDisplayApi`. Covers filtering, snap calculation, DI registration, options, exceptions.
- **Integration tests** (WindowManagement.IntegrationTests): Test against real Win32 APIs. Requires a Windows desktop session with at least one monitor. These verify actual window enumeration, monitor queries, and DPI detection work correctly.
