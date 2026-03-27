# CsWin32 Setup Guide for WindowManagementApi

Research compiled from the official CsWin32 GitHub repository and documentation.

---

## 1. Adding CsWin32 to a .csproj File

### NuGet Package

The only required package is `Microsoft.Windows.CsWin32`. Latest stable version as of March 2026: **0.3.269**.

```bash
dotnet add package Microsoft.Windows.CsWin32
```

This single package transitively brings in the Win32 metadata (`Microsoft.Windows.SDK.Win32Metadata`) and documentation (`Microsoft.Windows.SDK.Win32Docs`). You do not need to add those separately.

### .csproj Configuration

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <!-- CsWin32 sets AllowUnsafeBlocks=true automatically via its .props import.
         Just make sure you do NOT explicitly set it to false. -->
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Windows.CsWin32" Version="0.3.269">
      <!-- CsWin32 is a build-time-only source generator; it ships no runtime DLLs -->
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

</Project>
```

Key notes:
- The NuGet package's `.props` file automatically sets `AllowUnsafeBlocks` to `true`. You do not need to set it yourself. Just verify your .csproj does not explicitly set `<AllowUnsafeBlocks>false</AllowUnsafeBlocks>`.
- `PrivateAssets=all` is correct because CsWin32 is purely a compile-time source generator -- it produces no runtime assemblies.
- For .NET Framework 4.5+ or .NET Standard 2.0 targets you would also need `System.Memory` and `System.Runtime.CompilerServices.Unsafe`, but for `net10.0-windows` these are not needed.
- C# 9+ is required; use the latest language version for best results (CsWin32 sometimes emits C# 11 syntax).

---

## 2. NativeMethods.txt Format

Create a file named `NativeMethods.txt` in the project root (same directory as the .csproj). The file uses a simple line-based format:

### Rules

- **One entry per line**
- **Blank lines** are ignored
- **Comments** start with `//`
- Each line can be:
  - A **function name**: `CreateFile` or `CreateFileW`
  - A **type name** (struct, enum, interface, constant): `RECT`, `HWND`, `WINDOWPLACEMENT`
  - A **constant name**: `WS_VISIBLE`, `S_OK`
  - A **macro name**: `HRESULT_FROM_WIN32`, `HIWORD`
  - A **namespace** to pull in everything from it: `Windows.Win32.Storage.FileSystem`
  - A **module wildcard**: `Kernel32.*`
  - A **constant prefix with wildcard**: `ALG_SID_MD*`
  - An **exclusion** (prefix with `-`): `-BSTR`, `-Windows.Win32.Foundation.*`

### Dependency Resolution

When you list any function or type, **all supporting types are automatically generated**. For example, listing `EnumWindows` will automatically generate `WNDENUMPROC` (the callback delegate), `HWND`, `BOOL`, `LPARAM`, etc.

### Example from the CsWin32 test suite

```
BeginPaint
CreateWindowExW
EnumWindows
GetForegroundWindow
GetWindowText
GetWindowTextLength
RECT
ShowWindow
HWND
```

No signatures, no parameters, no return types -- just the bare API/type name.

---

## 3. Exact NativeMethods.txt for Window Management

Here is the complete list for the WindowManagementApi project:

```
// Window enumeration
EnumWindows

// Window text
GetWindowText
GetWindowTextLength

// Window class
GetClassName

// Window geometry
GetWindowRect

// Window positioning
SetWindowPos
BringWindowToTop

// Window state
ShowWindow
GetWindowPlacement
SetWindowPlacement
IsWindow
IsWindowVisible
IsIconic
IsZoomed

// Window styles (GetWindowLong/GetWindowLongPtr)
GetWindowLongPtr
SetWindowLongPtr

// Window process info
GetWindowThreadProcessId

// Foreground window
SetForegroundWindow
GetForegroundWindow

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
SetProcessDpiAwarenessContext
GetProcessDpiAwarenessContext
IsValidDpiAwarenessContext

// DWM APIs
DwmGetWindowAttribute

// Window event hooks
SetWinEventHook
UnhookWinEvent

// Thread management
AttachThreadInput
GetCurrentThreadId

// Constants and types used directly
HWND
HMONITOR
RECT
WINDOWPLACEMENT
SHOW_WINDOW_CMD
SET_WINDOW_POS_FLAGS
WINDOW_STYLE
WINDOW_EX_STYLE
DWMWINDOWATTRIBUTE
MONITOR_FROM_FLAGS
DPI_AWARENESS
DPI_AWARENESS_CONTEXT
```

### Notes on specific APIs

- **GetWindowLong vs GetWindowLongPtr**: Use `GetWindowLongPtr` (and `SetWindowLongPtr`). CsWin32 will generate the correct platform-aware version. The metadata uses `GetWindowLongPtr` as the entry point name. On 64-bit .NET (which is all modern .NET targets), `GetWindowLongPtr` is the correct API. CsWin32 handles the 32/64 bit distinction automatically.

- **GetWindowTextW vs GetWindowText**: Use `GetWindowText` (without the `W` suffix). By default, CsWin32's `wideCharOnly` setting is `true`, which strips the `W` suffix and omits ANSI variants. You write `GetWindowText` in NativeMethods.txt and CsWin32 generates the Unicode version.

- **Dependent types are auto-generated**: Listing `EnumWindows` auto-generates the `WNDENUMPROC` delegate type, `HWND`, `LPARAM`, `BOOL` etc. Listing `SetWindowPos` auto-generates `SET_WINDOW_POS_FLAGS`. You only need to explicitly list types/constants if you reference them directly without going through a function that uses them.

---

## 4. How CsWin32 Generates Types

### Source Generation Mechanism

CsWin32 is a Roslyn **incremental source generator**. At compile time it:
1. Reads your `NativeMethods.txt` entries
2. Looks up each entry in the Win32 metadata (`.winmd` file from `Microsoft.Windows.SDK.Win32Metadata`)
3. Generates C# source files with the P/Invoke signatures, structs, enums, delegates, constants, and COM interfaces
4. Includes XML documentation pulled from Microsoft Learn

The generated code is **not written to disk** by default (it exists only in the compiler's in-memory pipeline). You can see it in Visual Studio via the Source Generators node in Solution Explorer, or via the Analyzers node.

### Namespace

All generated code lives under `Windows.Win32`:

| Kind | Namespace |
|------|-----------|
| P/Invoke methods | `Windows.Win32.PInvoke` (static partial class) |
| Constants | `Windows.Win32.PInvoke` (same class) |
| Structs (RECT, WINDOWPLACEMENT, etc.) | `Windows.Win32.*` (sub-namespaces matching the Win32 metadata organization, e.g. `Windows.Win32.Foundation`, `Windows.Win32.UI.WindowsAndMessaging`) |
| Enums (SHOW_WINDOW_CMD, etc.) | Same sub-namespaces |
| Delegates (WNDENUMPROC, etc.) | Same sub-namespaces |
| Handle types (HWND, HMONITOR, etc.) | `Windows.Win32.Foundation` |

### SafeHandle Generation

When `useSafeHandles` is `true` (the default), CsWin32 generates:
- **SafeHandle wrapper types** for handle-based APIs (e.g., `CreateFile` returns a `SafeFileHandle` or `CloseHandleSafeHandle`)
- **Friendly overloads** that accept/return SafeHandle types alongside the raw overloads
- For window handles (`HWND`), monitor handles (`HMONITOR`), etc., CsWin32 generates **lightweight struct wrappers** (not SafeHandles, since these are non-owned handles). `HWND` is a struct with an implicit conversion to/from `nint`/`IntPtr`.

### Handle Structs (HWND, HMONITOR, etc.)

These are generated as readonly structs with:
- A `Value` property of type `nint`
- Implicit conversion operators to/from `nint`
- `static HWND Null` property
- Equality, hashing, etc.

Example of what CsWin32 generates for `HWND`:
```csharp
namespace Windows.Win32.Foundation;

readonly partial struct HWND : IEquatable<HWND>
{
    readonly nint Value;
    internal HWND(nint value) => this.Value = value;
    public static HWND Null => default;
    public bool IsNull => Value == 0;
    public static implicit operator nint(HWND value) => value.Value;
    public static explicit operator HWND(nint value) => new(value);
    // ... Equals, GetHashCode, etc.
}
```

### Delegate Types

Callback delegates like `WNDENUMPROC` (for `EnumWindows`) are generated as delegate types:
```csharp
internal delegate BOOL WNDENUMPROC(HWND param0, LPARAM param1);
```

### Struct Generation

Structs like `RECT`, `WINDOWPLACEMENT`, `MONITORINFOEXW` are generated as partial structs with all fields. For structs with a `cbSize` field, you need to set it manually:
```csharp
WINDOWPLACEMENT wp = default;
wp.length = (uint)sizeof(WINDOWPLACEMENT);  // or Marshal.SizeOf<WINDOWPLACEMENT>()
```

### Friendly Overloads and Span Support

CsWin32 generates multiple overloads:
- A raw overload matching the exact Win32 signature (with pointers)
- A "friendly" overload using `Span<T>`, `SafeHandle`, `out` parameters, etc.
- For methods with optional pointer parameters, overloads with those parameters omitted

---

## 5. Required .csproj Settings

### AllowUnsafeBlocks

**Automatically set by the NuGet package.** The CsWin32 package includes a `.props` file that sets `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`. You do not need to add this yourself. Just make sure you are not explicitly overriding it to `false`.

### Optional: AOT/Trimming Support

If you want AOT-compatible code generation (no runtime marshalling):

```xml
<PropertyGroup>
  <CsWin32RunAsBuildTask>true</CsWin32RunAsBuildTask>
  <DisableRuntimeMarshalling>true</DisableRuntimeMarshalling>
</PropertyGroup>
```

Or in NativeMethods.json:
```json
{
  "allowMarshaling": false
}
```

### Optional: NativeMethods.json Configuration

Create `NativeMethods.json` next to `NativeMethods.txt` for customization:

```json
{
  "$schema": "https://aka.ms/CsWin32.schema.json"
}
```

All available options with their defaults:

```json
{
  "$schema": "https://aka.ms/CsWin32.schema.json",
  "className": "PInvoke",
  "public": false,
  "emitSingleFile": false,
  "allowMarshaling": true,
  "useSafeHandles": true,
  "wideCharOnly": true,
  "multiTargetingFriendlyAPIs": false,
  "friendlyOverloads": {
    "enabled": true,
    "includePointerOverloads": false
  },
  "comInterop": {
    "preserveSigMethods": [],
    "useComSourceGenerators": false,
    "useIntPtrForComOutPointers": false
  }
}
```

For the WindowManagementApi project, a reasonable configuration would be:

```json
{
  "$schema": "https://aka.ms/CsWin32.schema.json",
  "public": false,
  "emitSingleFile": false,
  "useSafeHandles": true,
  "wideCharOnly": true,
  "friendlyOverloads": {
    "enabled": true
  }
}
```

The defaults are fine for most window management work. Keeping `public: false` means all generated P/Invoke types are `internal`, which is correct since consumers should use the library's public API surface, not raw Win32 calls.

---

## 6. How to Call the Generated Code

### Basic Pattern

```csharp
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.Graphics.Gdi;

// Call P/Invoke methods via the static PInvoke class:
HWND foreground = PInvoke.GetForegroundWindow();

// Or fully qualified:
Windows.Win32.PInvoke.GetForegroundWindow();
```

### Practical Examples for Window Management

#### Enumerating Windows
```csharp
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

var windows = new List<HWND>();
PInvoke.EnumWindows((hWnd, lParam) =>
{
    windows.Add(hWnd);
    return true;  // BOOL: true = continue enumeration
}, 0);
```

#### Getting Window Text
```csharp
int length = PInvoke.GetWindowTextLength(hWnd);
if (length > 0)
{
    // CsWin32 generates a Span-based friendly overload
    Span<char> buffer = stackalloc char[length + 1];
    int written = PInvoke.GetWindowText(hWnd, buffer);
    string title = buffer[..written].ToString();
}
```

#### Getting Window Class Name
```csharp
Span<char> buffer = stackalloc char[256];
int len = PInvoke.GetClassName(hWnd, buffer);
string className = buffer[..len].ToString();
```

#### Getting/Setting Window Position
```csharp
RECT rect;
PInvoke.GetWindowRect(hWnd, out rect);

// SetWindowPos with flags
PInvoke.SetWindowPos(hWnd, HWND.Null,
    100, 100, 800, 600,
    SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
```

#### Window State
```csharp
PInvoke.ShowWindow(hWnd, SHOW_WINDOW_CMD.SW_MAXIMIZE);

bool isMinimized = PInvoke.IsIconic(hWnd);
bool isMaximized = PInvoke.IsZoomed(hWnd);
bool isVisible = PInvoke.IsWindowVisible(hWnd);
```

#### Window Styles
```csharp
// Getting window styles
nint style = PInvoke.GetWindowLongPtr(hWnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
nint exStyle = PInvoke.GetWindowLongPtr(hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);

bool isToolWindow = ((WINDOW_EX_STYLE)exStyle).HasFlag(WINDOW_EX_STYLE.WS_EX_TOOLWINDOW);
```

#### Window Placement
```csharp
unsafe
{
    WINDOWPLACEMENT wp = default;
    wp.length = (uint)sizeof(WINDOWPLACEMENT);
    PInvoke.GetWindowPlacement(hWnd, &wp);
}
```

#### Process Information
```csharp
uint processId;
uint threadId = PInvoke.GetWindowThreadProcessId(hWnd, out processId);
```

#### Monitor APIs
```csharp
using Windows.Win32.Graphics.Gdi;

HMONITOR monitor = PInvoke.MonitorFromWindow(hWnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);

unsafe
{
    MONITORINFOEXW info = default;
    info.monitorInfo.cbSize = (uint)sizeof(MONITORINFOEXW);
    PInvoke.GetMonitorInfo(monitor, (MONITORINFO*)&info);

    RECT workArea = info.monitorInfo.rcWork;
    RECT monitorBounds = info.monitorInfo.rcMonitor;
}
```

#### DPI APIs
```csharp
uint dpi = PInvoke.GetDpiForWindow(hWnd);

// Monitor DPI
uint dpiX, dpiY;
PInvoke.GetDpiForMonitor(monitor,
    MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI,
    out dpiX, out dpiY);
```

#### DWM Window Attribute (for invisible border / cloaked detection)
```csharp
using Windows.Win32.Graphics.Dwm;

unsafe
{
    RECT extendedBounds;
    PInvoke.DwmGetWindowAttribute(hWnd,
        DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS,
        &extendedBounds,
        (uint)sizeof(RECT));

    // Cloaked detection
    uint cloaked;
    PInvoke.DwmGetWindowAttribute(hWnd,
        DWMWINDOWATTRIBUTE.DWMWA_CLOAKED,
        &cloaked,
        sizeof(uint));
    bool isCloaked = cloaked != 0;
}
```

#### SetWinEventHook (for real-time window events)
```csharp
using Windows.Win32.UI.Accessibility;

// The callback delegate
static void WinEventProc(HWINEVENTHOOK hWinEventHook, uint eventId,
    HWND hwnd, int idObject, int idChild,
    uint idEventThread, uint dwmsEventTime)
{
    // Handle events
}

// Hook into window events
HWINEVENTHOOK hook = PInvoke.SetWinEventHook(
    0x0003,  // EVENT_SYSTEM_FOREGROUND
    0x0003,
    HMODULE.Null,
    WinEventProc,
    0, 0,
    0x0002 | 0x0000);  // WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS

// Later, unhook
PInvoke.UnhookWinEvent(hook);
```

#### Focus Window (AttachThreadInput trick)
```csharp
uint foregroundThread = PInvoke.GetWindowThreadProcessId(
    PInvoke.GetForegroundWindow(), out _);
uint currentThread = PInvoke.GetCurrentThreadId();

if (foregroundThread != currentThread)
{
    PInvoke.AttachThreadInput(foregroundThread, currentThread, true);
    PInvoke.SetForegroundWindow(hWnd);
    PInvoke.BringWindowToTop(hWnd);
    PInvoke.AttachThreadInput(foregroundThread, currentThread, false);
}
else
{
    PInvoke.SetForegroundWindow(hWnd);
}
```

### Summary: Calling Convention

| Approach | Works? |
|----------|--------|
| `using Windows.Win32;` then `PInvoke.EnumWindows(...)` | Yes (recommended) |
| `Windows.Win32.PInvoke.EnumWindows(...)` | Yes (fully qualified) |
| No `using` needed for types in same namespace scope | Depends on your `using` directives |

The generated static class is always named `PInvoke` (configurable via `className` in NativeMethods.json) in the `Windows.Win32` namespace. Types (structs, enums, delegates) are in sub-namespaces like `Windows.Win32.Foundation`, `Windows.Win32.UI.WindowsAndMessaging`, `Windows.Win32.Graphics.Gdi`, etc.

---

## Quick Reference: File Layout

```
WindowManagement/
  WindowManagement.csproj
  NativeMethods.txt          <-- list of Win32 APIs (one per line)
  NativeMethods.json         <-- optional configuration
```

No other setup is required. The source generator activates automatically when the NuGet package is referenced and a `NativeMethods.txt` file exists in the project.
