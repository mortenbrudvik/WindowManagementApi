# WindowManager.Demo Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a polished WPF utility app (mini FancyZones) showcasing the WindowManagementApi library, with 4 tabs: Windows, Monitors, Snap, and Events.

**Architecture:** Standalone WPF-UI app in `examples/WindowManager.Demo/` using Autofac DI with `WindowManagementModule`, CommunityToolkit.Mvvm for ViewModels, and R3 for reactive event streams. FluentWindow with Mica backdrop and NavigationView sidebar for tab navigation.

**Tech Stack:** .NET 10, WPF-UI 4.2+, Autofac 9+, CommunityToolkit.Mvvm 8.4+, R3 (via WindowManagementApi), WindowManagementApi (project reference)

**Reference docs:**
- Spec: `docs/superpowers/specs/2026-03-27-window-manager-demo-design.md`
- WPF template: `C:\code\template-projects\desktop-development-template\wpf-development-template.md`
- WPF-UI guide: `C:\code\template-projects\desktop-development-template\wpf-ui-design-guide.md`

---

## File Map

```
examples/
└── WindowManager.Demo/
    ├── src/
    │   └── WindowManager.Demo/
    │       ├── WindowManager.Demo.csproj       # Task 1
    │       ├── app.manifest                     # Task 1
    │       ├── App.xaml                          # Task 2
    │       ├── App.xaml.cs                       # Task 2
    │       ├── MainWindow.xaml                   # Task 3
    │       ├── MainWindow.xaml.cs                # Task 3
    │       ├── Modules/
    │       │   └── AppModule.cs                  # Task 2
    │       ├── Services/
    │       │   └── ApplicationHostService.cs     # Task 2
    │       ├── Models/
    │       │   ├── WindowItem.cs                 # Task 4
    │       │   └── EventEntry.cs                 # Task 7
    │       ├── ViewModels/
    │       │   ├── MainWindowViewModel.cs        # Task 3
    │       │   ├── WindowsViewModel.cs           # Task 4
    │       │   ├── MonitorsViewModel.cs          # Task 5
    │       │   ├── SnapViewModel.cs              # Task 6
    │       │   └── EventsViewModel.cs            # Task 7
    │       └── Views/
    │           ├── WindowsPage.xaml              # Task 4
    │           ├── WindowsPage.xaml.cs            # Task 4
    │           ├── MonitorsPage.xaml              # Task 5
    │           ├── MonitorsPage.xaml.cs            # Task 5
    │           ├── SnapPage.xaml                  # Task 6
    │           ├── SnapPage.xaml.cs                # Task 6
    │           ├── EventsPage.xaml                # Task 7
    │           └── EventsPage.xaml.cs              # Task 7
    ├── Directory.Build.props                     # Task 1
    ├── .editorconfig                             # Task 1
    ├── WindowManager.Demo.sln                    # Task 1
    └── CLAUDE.md                                 # Task 8
```

---

### Task 1: Solution Infrastructure

**Files:**
- Create: `examples/WindowManager.Demo/WindowManager.Demo.sln`
- Create: `examples/WindowManager.Demo/Directory.Build.props`
- Create: `examples/WindowManager.Demo/.editorconfig`
- Create: `examples/WindowManager.Demo/src/WindowManager.Demo/WindowManager.Demo.csproj`
- Create: `examples/WindowManager.Demo/src/WindowManager.Demo/app.manifest`

- [ ] **Step 1: Create solution directory structure**

```bash
mkdir -p examples/WindowManager.Demo/src/WindowManager.Demo
```

- [ ] **Step 2: Create Directory.Build.props**

Create `examples/WindowManager.Demo/Directory.Build.props`:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0-windows10.0.26100.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>14</LangVersion>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

- [ ] **Step 3: Create .editorconfig**

Create `examples/WindowManager.Demo/.editorconfig`:

```ini
root = true

[*]
indent_style = space
indent_size = 4
end_of_line = crlf
charset = utf-8-bom
trim_trailing_whitespace = true
insert_final_newline = true

[*.{xml,xaml,csproj,props,targets}]
indent_size = 2

[*.cs]
csharp_style_namespace_declarations = file_scoped:warning
csharp_style_var_for_built_in_types = false:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = false:suggestion
csharp_style_expression_bodied_methods = when_on_single_line:suggestion
csharp_style_expression_bodied_constructors = false:suggestion
csharp_style_expression_bodied_properties = true:suggestion
csharp_style_expression_bodied_accessors = true:suggestion
csharp_style_expression_bodied_lambdas = true:suggestion
csharp_style_prefer_switch_expression = true:suggestion
csharp_style_prefer_pattern_matching = true:suggestion
csharp_style_prefer_null_check_over_type_check = true:suggestion
dotnet_style_coalesce_expression = true:suggestion
dotnet_style_null_propagation = true:suggestion
dotnet_naming_rule.public_members_must_be_pascal_case.symbols = public_symbols
dotnet_naming_rule.public_members_must_be_pascal_case.style = pascal_case_style
dotnet_naming_rule.public_members_must_be_pascal_case.severity = warning
dotnet_naming_symbols.public_symbols.applicable_kinds = property,method,event
dotnet_naming_symbols.public_symbols.applicable_accessibilities = public
dotnet_naming_style.pascal_case_style.capitalization = pascal_case
dotnet_naming_rule.private_fields_must_be_underscore_camel.symbols = private_fields
dotnet_naming_rule.private_fields_must_be_underscore_camel.style = underscore_camel_style
dotnet_naming_rule.private_fields_must_be_underscore_camel.severity = warning
dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private
dotnet_naming_style.underscore_camel_style.capitalization = camel_case
dotnet_naming_style.underscore_camel_style.required_prefix = _
dotnet_sort_system_directives_first = true
csharp_using_directive_placement = outside_namespace:warning
```

- [ ] **Step 4: Create the .csproj**

Create `examples/WindowManager.Demo/src/WindowManager.Demo/WindowManager.Demo.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <UseWPF>true</UseWPF>
    <ApplicationManifest>app.manifest</ApplicationManifest>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="WPF-UI" Version="4.*" />
    <PackageReference Include="WPF-UI.DependencyInjection" Version="4.*" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="10.*" />
    <PackageReference Include="Autofac" Version="9.*" />
    <PackageReference Include="Autofac.Extensions.DependencyInjection" Version="11.*" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\..\src\WindowManagement\WindowManagement.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 5: Create app.manifest for per-monitor v2 DPI awareness**

Create `examples/WindowManager.Demo/src/WindowManager.Demo/app.manifest`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
  <assemblyIdentity version="1.0.0.0" name="WindowManager.Demo"/>
  <application xmlns="urn:schemas-microsoft-com:asm.v3">
    <windowsSettings>
      <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2</dpiAwareness>
      <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">true/pm</dpiAware>
    </windowsSettings>
  </application>
  <compatibility xmlns="urn:schemas-microsoft-com:compatibility.v1">
    <application>
      <supportedOS Id="{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}" />
    </application>
  </compatibility>
</assembly>
```

- [ ] **Step 6: Create solution file**

```bash
cd examples/WindowManager.Demo
dotnet new sln --name WindowManager.Demo
dotnet sln add src/WindowManager.Demo/WindowManager.Demo.csproj
```

- [ ] **Step 7: Verify build**

```bash
cd examples/WindowManager.Demo
dotnet build WindowManager.Demo.sln
```

Expected: Build succeeds with 0 errors. There will be no entry point yet (App.xaml.cs hasn't been created), but the project structure should compile.

- [ ] **Step 8: Commit**

```bash
git add examples/WindowManager.Demo/
git commit -m "scaffold: WindowManager.Demo solution infrastructure"
```

---

### Task 2: App Bootstrap (Autofac + IHost)

**Files:**
- Create: `examples/WindowManager.Demo/src/WindowManager.Demo/App.xaml`
- Create: `examples/WindowManager.Demo/src/WindowManager.Demo/App.xaml.cs`
- Create: `examples/WindowManager.Demo/src/WindowManager.Demo/Modules/AppModule.cs`
- Create: `examples/WindowManager.Demo/src/WindowManager.Demo/Services/ApplicationHostService.cs`

- [ ] **Step 1: Create App.xaml**

Create `examples/WindowManager.Demo/src/WindowManager.Demo/App.xaml`:

```xml
<Application
    x:Class="WindowManager.Demo.App"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
    DispatcherUnhandledException="OnDispatcherUnhandledException"
    Exit="OnExit"
    Startup="OnStartup">

    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ui:ThemesDictionary Theme="Dark" />
                <ui:ControlsDictionary />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>

</Application>
```

- [ ] **Step 2: Create App.xaml.cs**

Create `examples/WindowManager.Demo/src/WindowManager.Demo/App.xaml.cs`:

```csharp
using System.Windows;
using System.Windows.Threading;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WindowManager.Demo.Modules;
using WindowManager.Demo.Services;
using Wpf.Ui;
using Wpf.Ui.DependencyInjection;

namespace WindowManager.Demo;

public partial class App : Application
{
    private IHost? _host;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        _host = Host.CreateDefaultBuilder()
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureContainer<ContainerBuilder>(builder =>
            {
                builder.RegisterModule<AppModule>();
            })
            .ConfigureServices(services =>
            {
                services.AddNavigationViewPageProvider();
                services.AddHostedService<ApplicationHostService>();
            })
            .Build();

        _host.Start();
    }

    private void OnExit(object sender, ExitEventArgs e)
    {
        _host?.StopAsync().Wait();
        _host?.Dispose();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Prevent crash on unhandled exceptions — log to debug output
        System.Diagnostics.Debug.WriteLine($"Unhandled exception: {e.Exception}");
        e.Handled = true;
    }
}
```

- [ ] **Step 3: Create AppModule.cs**

Create `examples/WindowManager.Demo/src/WindowManager.Demo/Modules/AppModule.cs`:

```csharp
using Autofac;
using WindowManagement.DependencyInjection;
using WindowManager.Demo.ViewModels;
using WindowManager.Demo.Views;
using Wpf.Ui;

namespace WindowManager.Demo.Modules;

public class AppModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // WindowManagementApi
        builder.RegisterModule(new WindowManagementModule());

        // WPF-UI services
        builder.RegisterType<NavigationService>().As<INavigationService>().SingleInstance();
        builder.RegisterType<SnackbarService>().As<ISnackbarService>().SingleInstance();

        // Main window
        builder.RegisterType<MainWindow>().AsSelf().SingleInstance();
        builder.RegisterType<MainWindowViewModel>().AsSelf().SingleInstance();

        // Pages + ViewModels (singleton — always in memory, small app)
        builder.RegisterType<WindowsPage>().AsSelf().SingleInstance();
        builder.RegisterType<WindowsViewModel>().AsSelf().SingleInstance();

        builder.RegisterType<MonitorsPage>().AsSelf().SingleInstance();
        builder.RegisterType<MonitorsViewModel>().AsSelf().SingleInstance();

        builder.RegisterType<SnapPage>().AsSelf().SingleInstance();
        builder.RegisterType<SnapViewModel>().AsSelf().SingleInstance();

        builder.RegisterType<EventsPage>().AsSelf().SingleInstance();
        builder.RegisterType<EventsViewModel>().AsSelf().SingleInstance();
    }
}
```

- [ ] **Step 4: Create ApplicationHostService.cs**

Create `examples/WindowManager.Demo/src/WindowManager.Demo/Services/ApplicationHostService.cs`:

```csharp
using System.Windows;
using Microsoft.Extensions.Hosting;
using WindowManager.Demo.Views;

namespace WindowManager.Demo.Services;

public class ApplicationHostService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public ApplicationHostService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (Application.Current.Windows.OfType<MainWindow>().Any())
        {
            return Task.CompletedTask;
        }

        var mainWindow = (MainWindow)_serviceProvider.GetService(typeof(MainWindow))!;
        mainWindow.Show();
        mainWindow.NavigationView.Navigate(typeof(WindowsPage));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

- [ ] **Step 5: Verify build**

This will not build yet — `MainWindow`, `MainWindowViewModel`, pages, and ViewModels don't exist. That's expected. We'll build-verify after Task 3.

- [ ] **Step 6: Commit**

```bash
git add examples/WindowManager.Demo/src/WindowManager.Demo/App.xaml \
        examples/WindowManager.Demo/src/WindowManager.Demo/App.xaml.cs \
        examples/WindowManager.Demo/src/WindowManager.Demo/Modules/ \
        examples/WindowManager.Demo/src/WindowManager.Demo/Services/
git commit -m "feat: add App bootstrap with Autofac and IHost"
```

---

### Task 3: MainWindow with NavigationView

**Files:**
- Create: `examples/WindowManager.Demo/src/WindowManager.Demo/MainWindow.xaml`
- Create: `examples/WindowManager.Demo/src/WindowManager.Demo/MainWindow.xaml.cs`
- Create: `examples/WindowManager.Demo/src/WindowManager.Demo/ViewModels/MainWindowViewModel.cs`

- [ ] **Step 1: Create MainWindowViewModel.cs**

Create `examples/WindowManager.Demo/src/WindowManager.Demo/ViewModels/MainWindowViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace WindowManager.Demo.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _applicationTitle = "Window Manager Demo";
}
```

- [ ] **Step 2: Create MainWindow.xaml**

Create `examples/WindowManager.Demo/src/WindowManager.Demo/MainWindow.xaml`:

```xml
<ui:FluentWindow
    x:Class="WindowManager.Demo.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
    xmlns:views="clr-namespace:WindowManager.Demo.Views"
    Title="Window Manager Demo"
    Width="1100"
    Height="700"
    MinWidth="800"
    MinHeight="500"
    WindowBackdropType="Mica"
    WindowCornerPreference="Round"
    WindowStartupLocation="CenterScreen"
    ExtendsContentIntoTitleBar="True">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <ui:NavigationView
            x:Name="RootNavigation"
            Grid.Row="1"
            IsBackButtonVisible="Collapsed"
            IsPaneToggleVisible="True"
            PaneDisplayMode="Left">

            <ui:NavigationView.MenuItems>
                <ui:NavigationViewItem
                    Content="Windows"
                    TargetPageType="{x:Type views:WindowsPage}">
                    <ui:NavigationViewItem.Icon>
                        <ui:SymbolIcon Symbol="AppFolder24" />
                    </ui:NavigationViewItem.Icon>
                </ui:NavigationViewItem>

                <ui:NavigationViewItem
                    Content="Monitors"
                    TargetPageType="{x:Type views:MonitorsPage}">
                    <ui:NavigationViewItem.Icon>
                        <ui:SymbolIcon Symbol="Desktop24" />
                    </ui:NavigationViewItem.Icon>
                </ui:NavigationViewItem>

                <ui:NavigationViewItem
                    Content="Snap"
                    TargetPageType="{x:Type views:SnapPage}">
                    <ui:NavigationViewItem.Icon>
                        <ui:SymbolIcon Symbol="Grid24" />
                    </ui:NavigationViewItem.Icon>
                </ui:NavigationViewItem>

                <ui:NavigationViewItem
                    Content="Events"
                    TargetPageType="{x:Type views:EventsPage}">
                    <ui:NavigationViewItem.Icon>
                        <ui:SymbolIcon Symbol="Pulse24" />
                    </ui:NavigationViewItem.Icon>
                </ui:NavigationViewItem>
            </ui:NavigationView.MenuItems>

            <ui:NavigationView.ContentOverlay>
                <Grid>
                    <ui:SnackbarPresenter x:Name="SnackbarPresenter" />
                </Grid>
            </ui:NavigationView.ContentOverlay>

        </ui:NavigationView>

        <ui:TitleBar
            Title="Window Manager Demo"
            Grid.Row="0" />
    </Grid>

</ui:FluentWindow>
```

- [ ] **Step 3: Create MainWindow.xaml.cs**

Create `examples/WindowManager.Demo/src/WindowManager.Demo/MainWindow.xaml.cs`:

```csharp
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace WindowManager.Demo;

public partial class MainWindow : FluentWindow
{
    public NavigationView NavigationView => RootNavigation;

    public MainWindow(
        MainWindowViewModel viewModel,
        INavigationService navigationService,
        ISnackbarService snackbarService)
    {
        DataContext = viewModel;
        InitializeComponent();

        navigationService.SetNavigationControl(RootNavigation);
        snackbarService.SetSnackbarPresenter(SnackbarPresenter);
    }
}
```

Note: add `using WindowManager.Demo.ViewModels;` if `MainWindowViewModel` is not in scope due to the namespace difference. Since `MainWindow` is in the `WindowManager.Demo` namespace and `MainWindowViewModel` is in `WindowManager.Demo.ViewModels`, a using directive is needed:

```csharp
using Wpf.Ui;
using Wpf.Ui.Controls;
using WindowManager.Demo.ViewModels;

namespace WindowManager.Demo;

public partial class MainWindow : FluentWindow
{
    public NavigationView NavigationView => RootNavigation;

    public MainWindow(
        MainWindowViewModel viewModel,
        INavigationService navigationService,
        ISnackbarService snackbarService)
    {
        DataContext = viewModel;
        InitializeComponent();

        navigationService.SetNavigationControl(RootNavigation);
        snackbarService.SetSnackbarPresenter(SnackbarPresenter);
    }
}
```

- [ ] **Step 4: Create stub pages so the app compiles**

Create minimal stub files for all 4 pages and 4 ViewModels so the solution builds. Each will be fully implemented in Tasks 4-7.

**WindowsViewModel.cs** (`ViewModels/WindowsViewModel.cs`):
```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace WindowManager.Demo.ViewModels;

public partial class WindowsViewModel : ObservableObject
{
}
```

**MonitorsViewModel.cs** (`ViewModels/MonitorsViewModel.cs`):
```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace WindowManager.Demo.ViewModels;

public partial class MonitorsViewModel : ObservableObject
{
}
```

**SnapViewModel.cs** (`ViewModels/SnapViewModel.cs`):
```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace WindowManager.Demo.ViewModels;

public partial class SnapViewModel : ObservableObject
{
}
```

**EventsViewModel.cs** (`ViewModels/EventsViewModel.cs`):
```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace WindowManager.Demo.ViewModels;

public partial class EventsViewModel : ObservableObject
{
}
```

**WindowsPage.xaml** (`Views/WindowsPage.xaml`):
```xml
<Page
    x:Class="WindowManager.Demo.Views.WindowsPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
    xmlns:local="clr-namespace:WindowManager.Demo.Views"
    d:DataContext="{d:DesignInstance local:WindowsPage, IsDesignTimeCreatable=False}"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d">
    <Grid>
        <ui:TextBlock Text="Windows" FontTypography="Title" HorizontalAlignment="Center" VerticalAlignment="Center" />
    </Grid>
</Page>
```

**WindowsPage.xaml.cs** (`Views/WindowsPage.xaml.cs`):
```csharp
using System.Windows.Controls;
using WindowManager.Demo.ViewModels;
using Wpf.Ui.Abstractions.Controls;

namespace WindowManager.Demo.Views;

public partial class WindowsPage : Page, INavigableView<WindowsViewModel>
{
    public WindowsViewModel ViewModel { get; }

    public WindowsPage(WindowsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}
```

Repeat the same pattern for `MonitorsPage`, `SnapPage`, and `EventsPage` (substituting the appropriate ViewModel type and placeholder text).

**MonitorsPage.xaml** (`Views/MonitorsPage.xaml`):
```xml
<Page
    x:Class="WindowManager.Demo.Views.MonitorsPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
    xmlns:local="clr-namespace:WindowManager.Demo.Views"
    d:DataContext="{d:DesignInstance local:MonitorsPage, IsDesignTimeCreatable=False}"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d">
    <Grid>
        <ui:TextBlock Text="Monitors" FontTypography="Title" HorizontalAlignment="Center" VerticalAlignment="Center" />
    </Grid>
</Page>
```

**MonitorsPage.xaml.cs** (`Views/MonitorsPage.xaml.cs`):
```csharp
using System.Windows.Controls;
using WindowManager.Demo.ViewModels;
using Wpf.Ui.Abstractions.Controls;

namespace WindowManager.Demo.Views;

public partial class MonitorsPage : Page, INavigableView<MonitorsViewModel>
{
    public MonitorsViewModel ViewModel { get; }

    public MonitorsPage(MonitorsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}
```

**SnapPage.xaml** (`Views/SnapPage.xaml`):
```xml
<Page
    x:Class="WindowManager.Demo.Views.SnapPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
    xmlns:local="clr-namespace:WindowManager.Demo.Views"
    d:DataContext="{d:DesignInstance local:SnapPage, IsDesignTimeCreatable=False}"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d">
    <Grid>
        <ui:TextBlock Text="Snap" FontTypography="Title" HorizontalAlignment="Center" VerticalAlignment="Center" />
    </Grid>
</Page>
```

**SnapPage.xaml.cs** (`Views/SnapPage.xaml.cs`):
```csharp
using System.Windows.Controls;
using WindowManager.Demo.ViewModels;
using Wpf.Ui.Abstractions.Controls;

namespace WindowManager.Demo.Views;

public partial class SnapPage : Page, INavigableView<SnapViewModel>
{
    public SnapViewModel ViewModel { get; }

    public SnapPage(SnapViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}
```

**EventsPage.xaml** (`Views/EventsPage.xaml`):
```xml
<Page
    x:Class="WindowManager.Demo.Views.EventsPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
    xmlns:local="clr-namespace:WindowManager.Demo.Views"
    d:DataContext="{d:DesignInstance local:EventsPage, IsDesignTimeCreatable=False}"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d">
    <Grid>
        <ui:TextBlock Text="Events" FontTypography="Title" HorizontalAlignment="Center" VerticalAlignment="Center" />
    </Grid>
</Page>
```

**EventsPage.xaml.cs** (`Views/EventsPage.xaml.cs`):
```csharp
using System.Windows.Controls;
using WindowManager.Demo.ViewModels;
using Wpf.Ui.Abstractions.Controls;

namespace WindowManager.Demo.Views;

public partial class EventsPage : Page, INavigableView<EventsViewModel>
{
    public EventsViewModel ViewModel { get; }

    public EventsPage(EventsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}
```

- [ ] **Step 5: Build and verify**

```bash
cd examples/WindowManager.Demo
dotnet build WindowManager.Demo.sln
```

Expected: Build succeeds with 0 errors, 0 warnings.

- [ ] **Step 6: Run the app to verify it launches**

```bash
cd examples/WindowManager.Demo
dotnet run --project src/WindowManager.Demo
```

Expected: A FluentWindow with Mica backdrop opens, showing a left-sidebar NavigationView with 4 tabs (Windows, Monitors, Snap, Events). The Windows tab is active with placeholder text. Clicking other tabs navigates between placeholder pages. Close the app manually.

- [ ] **Step 7: Commit**

```bash
git add examples/WindowManager.Demo/
git commit -m "feat: add MainWindow with NavigationView and stub pages"
```

---

### Task 4: Windows Tab (Window List + Details)

**Files:**
- Create: `examples/WindowManager.Demo/src/WindowManager.Demo/Models/WindowItem.cs`
- Modify: `examples/WindowManager.Demo/src/WindowManager.Demo/ViewModels/WindowsViewModel.cs`
- Modify: `examples/WindowManager.Demo/src/WindowManager.Demo/Views/WindowsPage.xaml`

- [ ] **Step 1: Create WindowItem model**

Create `examples/WindowManager.Demo/src/WindowManager.Demo/Models/WindowItem.cs`:

```csharp
using WindowManagement;

namespace WindowManager.Demo.Models;

public class WindowItem
{
    public IWindow Window { get; }

    public string Title => Window.Title;
    public string ProcessName => Window.ProcessName;
    public int ProcessId => Window.ProcessId;
    public string ClassName => Window.ClassName;
    public nint Handle => Window.Handle;
    public string HandleHex => $"0x{Handle:X8}";
    public WindowRect Bounds => Window.Bounds;
    public string BoundsText => $"{Bounds.X}, {Bounds.Y}, {Bounds.Width} x {Bounds.Height}";
    public WindowState State => Window.State;
    public bool IsTopmost => Window.IsTopmost;
    public string MonitorName => Window.Monitor.DeviceName;
    public int MonitorDpi => Window.Monitor.Dpi;
    public double MonitorScale => Window.Monitor.ScaleFactor;
    public string MonitorSummary => $"{MonitorName} ({Window.Monitor.Bounds.Width}x{Window.Monitor.Bounds.Height}, {MonitorDpi}DPI)";

    public WindowItem(IWindow window)
    {
        Window = window;
    }
}
```

- [ ] **Step 2: Implement WindowsViewModel**

Replace `examples/WindowManager.Demo/src/WindowManager.Demo/ViewModels/WindowsViewModel.cs`:

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using R3;
using WindowManagement;
using WindowManager.Demo.Models;

namespace WindowManager.Demo.ViewModels;

public partial class WindowsViewModel : ObservableObject, IDisposable
{
    private readonly IWindowManager _windowManager;
    private readonly IDisposable _subscriptions;

    [ObservableProperty]
    private WindowItem? _selectedWindow;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ObservableCollection<WindowItem> Windows { get; } = [];

    public WindowsViewModel(IWindowManager windowManager)
    {
        _windowManager = windowManager;

        _subscriptions = Disposable.Combine(
            windowManager.Created.Subscribe(_ => RefreshWindows()),
            windowManager.Destroyed.Subscribe(_ => RefreshWindows())
        );
    }

    public void OnNavigatedTo()
    {
        RefreshWindows();
    }

    [RelayCommand]
    private void RefreshWindows()
    {
        var previous = SelectedWindow?.Handle;
        Windows.Clear();

        foreach (IWindow window in _windowManager.GetAll())
        {
            var item = new WindowItem(window);

            if (!string.IsNullOrEmpty(SearchText)
                && !item.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                && !item.ProcessName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Windows.Add(item);
        }

        if (previous.HasValue)
        {
            SelectedWindow = Windows.FirstOrDefault(w => w.Handle == previous.Value);
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        RefreshWindows();
    }

    public void Dispose()
    {
        _subscriptions.Dispose();
    }
}
```

- [ ] **Step 3: Implement WindowsPage.xaml**

Replace `examples/WindowManager.Demo/src/WindowManager.Demo/Views/WindowsPage.xaml`:

```xml
<Page
    x:Class="WindowManager.Demo.Views.WindowsPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
    xmlns:local="clr-namespace:WindowManager.Demo.Views"
    d:DataContext="{d:DesignInstance local:WindowsPage, IsDesignTimeCreatable=False}"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d"
    Loaded="OnLoaded">

    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="300" />
            <ColumnDefinition Width="*" />
        </Grid.ColumnDefinitions>

        <!-- Left panel: Window list -->
        <Grid Grid.Column="0">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="*" />
                <RowDefinition Height="Auto" />
            </Grid.RowDefinitions>

            <StackPanel Grid.Row="0" Margin="12,12,12,0">
                <ui:TextBox
                    PlaceholderText="Search windows..."
                    Text="{Binding ViewModel.SearchText, UpdateSourceTrigger=PropertyChanged}"
                    Icon="{ui:SymbolIcon Search24}"
                    Margin="0,0,0,8" />
                <ui:Button
                    Content="Refresh"
                    Appearance="Secondary"
                    Icon="{ui:SymbolIcon ArrowSync24}"
                    Command="{Binding ViewModel.RefreshWindowsCommand}"
                    HorizontalAlignment="Stretch" />
            </StackPanel>

            <ListBox
                Grid.Row="1"
                Margin="8,8,8,0"
                ItemsSource="{Binding ViewModel.Windows}"
                SelectedItem="{Binding ViewModel.SelectedWindow}"
                ScrollViewer.HorizontalScrollBarVisibility="Disabled">
                <ListBox.ItemTemplate>
                    <DataTemplate>
                        <StackPanel Margin="4">
                            <TextBlock Text="{Binding Title}" FontWeight="SemiBold" TextTrimming="CharacterEllipsis" />
                            <TextBlock Opacity="0.6" FontSize="11">
                                <Run Text="{Binding ProcessName, Mode=OneWay}" />
                                <Run Text=" · " />
                                <Run Text="{Binding State, Mode=OneWay}" />
                                <Run Text=" · " />
                                <Run Text="{Binding MonitorName, Mode=OneWay}" />
                            </TextBlock>
                        </StackPanel>
                    </DataTemplate>
                </ListBox.ItemTemplate>
            </ListBox>

            <TextBlock
                Grid.Row="2"
                Margin="12,4,12,8"
                FontSize="11"
                Opacity="0.6">
                <Run Text="{Binding ViewModel.Windows.Count, Mode=OneWay}" />
                <Run Text=" windows" />
            </TextBlock>
        </Grid>

        <!-- Right panel: Details -->
        <ScrollViewer Grid.Column="1" Margin="16,12,16,12" VerticalScrollBarVisibility="Auto">
            <StackPanel Visibility="{Binding ViewModel.SelectedWindow, Converter={StaticResource NullToVisibilityConverter}}">
                <ui:TextBlock
                    Text="{Binding ViewModel.SelectedWindow.Title}"
                    FontTypography="Subtitle"
                    TextWrapping="Wrap"
                    Margin="0,0,0,16" />

                <UniformGrid Columns="2" Margin="0,0,0,8">
                    <StackPanel Margin="0,0,16,12">
                        <TextBlock Text="Process" FontSize="11" Opacity="0.6" />
                        <TextBlock Text="{Binding ViewModel.SelectedWindow.ProcessName}" FontSize="13" />
                    </StackPanel>
                    <StackPanel Margin="0,0,16,12">
                        <TextBlock Text="Handle" FontSize="11" Opacity="0.6" />
                        <TextBlock Text="{Binding ViewModel.SelectedWindow.HandleHex}" FontSize="13" FontFamily="Consolas" />
                    </StackPanel>
                    <StackPanel Margin="0,0,16,12">
                        <TextBlock Text="State" FontSize="11" Opacity="0.6" />
                        <TextBlock Text="{Binding ViewModel.SelectedWindow.State}" FontSize="13" />
                    </StackPanel>
                    <StackPanel Margin="0,0,16,12">
                        <TextBlock Text="PID" FontSize="11" Opacity="0.6" />
                        <TextBlock Text="{Binding ViewModel.SelectedWindow.ProcessId}" FontSize="13" />
                    </StackPanel>
                    <StackPanel Margin="0,0,16,12">
                        <TextBlock Text="Bounds" FontSize="11" Opacity="0.6" />
                        <TextBlock Text="{Binding ViewModel.SelectedWindow.BoundsText}" FontSize="13" FontFamily="Consolas" />
                    </StackPanel>
                    <StackPanel Margin="0,0,16,12">
                        <TextBlock Text="Class Name" FontSize="11" Opacity="0.6" />
                        <TextBlock Text="{Binding ViewModel.SelectedWindow.ClassName}" FontSize="13" TextTrimming="CharacterEllipsis" />
                    </StackPanel>
                    <StackPanel Margin="0,0,16,12">
                        <TextBlock Text="Monitor" FontSize="11" Opacity="0.6" />
                        <TextBlock Text="{Binding ViewModel.SelectedWindow.MonitorSummary}" FontSize="13" TextWrapping="Wrap" />
                    </StackPanel>
                    <StackPanel Margin="0,0,16,12">
                        <TextBlock Text="Topmost" FontSize="11" Opacity="0.6" />
                        <TextBlock Text="{Binding ViewModel.SelectedWindow.IsTopmost}" FontSize="13" />
                    </StackPanel>
                </UniformGrid>
            </StackPanel>
        </ScrollViewer>
    </Grid>
</Page>
```

**Note:** The `NullToVisibilityConverter` is used to hide the details panel when nothing is selected. You will need to either:
- Add a `NullToVisibilityConverter` class and register it as a static resource in `App.xaml`, or
- Use a `DataTrigger` style on the `StackPanel` to toggle visibility when `SelectedWindow` is null.

A simple approach is to add to `App.xaml` resources after the merged dictionaries:

```xml
<!-- Inside App.xaml Application.Resources > ResourceDictionary, after MergedDictionaries -->
<BooleanToVisibilityConverter x:Key="BoolToVisibility" />
```

And change the binding to use a `DataTrigger` instead:

```xml
<StackPanel>
    <StackPanel.Style>
        <Style TargetType="StackPanel">
            <Setter Property="Visibility" Value="Visible" />
            <Style.Triggers>
                <DataTrigger Binding="{Binding ViewModel.SelectedWindow}" Value="{x:Null}">
                    <Setter Property="Visibility" Value="Collapsed" />
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </StackPanel.Style>
    <!-- ... contents ... -->
</StackPanel>
```

Use the `DataTrigger` approach — it avoids needing a custom converter.

- [ ] **Step 4: Update WindowsPage.xaml.cs with OnLoaded**

Replace `examples/WindowManager.Demo/src/WindowManager.Demo/Views/WindowsPage.xaml.cs`:

```csharp
using System.Windows;
using System.Windows.Controls;
using WindowManager.Demo.ViewModels;
using Wpf.Ui.Abstractions.Controls;

namespace WindowManager.Demo.Views;

public partial class WindowsPage : Page, INavigableView<WindowsViewModel>
{
    public WindowsViewModel ViewModel { get; }

    public WindowsPage(WindowsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.OnNavigatedTo();
    }
}
```

- [ ] **Step 5: Build and verify**

```bash
cd examples/WindowManager.Demo
dotnet build WindowManager.Demo.sln
```

Expected: Build succeeds.

- [ ] **Step 6: Run and verify the Windows tab**

```bash
cd examples/WindowManager.Demo
dotnet run --project src/WindowManager.Demo
```

Expected: Windows tab shows a scrollable list of open windows. Clicking a window shows its details in the right panel. Search filters the list. Refresh button reloads.

- [ ] **Step 7: Commit**

```bash
git add examples/WindowManager.Demo/
git commit -m "feat: implement Windows tab with window list and details"
```

---

### Task 5: Monitors Tab

**Files:**
- Modify: `examples/WindowManager.Demo/src/WindowManager.Demo/ViewModels/MonitorsViewModel.cs`
- Modify: `examples/WindowManager.Demo/src/WindowManager.Demo/Views/MonitorsPage.xaml`
- Modify: `examples/WindowManager.Demo/src/WindowManager.Demo/Views/MonitorsPage.xaml.cs`

- [ ] **Step 1: Implement MonitorsViewModel**

Replace `examples/WindowManager.Demo/src/WindowManager.Demo/ViewModels/MonitorsViewModel.cs`:

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using R3;
using WindowManagement;

namespace WindowManager.Demo.ViewModels;

public partial class MonitorsViewModel : ObservableObject, IDisposable
{
    private readonly IMonitorService _monitorService;
    private readonly IDisposable _subscriptions;

    [ObservableProperty]
    private IMonitor? _selectedMonitor;

    public ObservableCollection<IMonitor> Monitors { get; } = [];

    // Scale factor for drawing monitors on the canvas.
    // Computed from the combined bounds of all monitors.
    [ObservableProperty]
    private double _canvasScale = 0.15;

    // Offset to translate monitor coordinates so the leftmost/topmost monitor starts at (0,0).
    [ObservableProperty]
    private double _offsetX;

    [ObservableProperty]
    private double _offsetY;

    public MonitorsViewModel(IMonitorService monitorService)
    {
        _monitorService = monitorService;

        _subscriptions = Disposable.Combine(
            monitorService.Connected.Subscribe(_ => RefreshMonitors()),
            monitorService.Disconnected.Subscribe(_ => RefreshMonitors()),
            monitorService.SettingsChanged.Subscribe(_ => RefreshMonitors())
        );
    }

    public void OnNavigatedTo()
    {
        RefreshMonitors();
    }

    [RelayCommand]
    private void RefreshMonitors()
    {
        Monitors.Clear();

        int minX = int.MaxValue, minY = int.MaxValue;
        int maxRight = int.MinValue, maxBottom = int.MinValue;

        foreach (IMonitor monitor in _monitorService.All)
        {
            Monitors.Add(monitor);

            if (monitor.Bounds.X < minX) minX = monitor.Bounds.X;
            if (monitor.Bounds.Y < minY) minY = monitor.Bounds.Y;
            if (monitor.Bounds.Right > maxRight) maxRight = monitor.Bounds.Right;
            if (monitor.Bounds.Bottom > maxBottom) maxBottom = monitor.Bounds.Bottom;
        }

        OffsetX = -minX;
        OffsetY = -minY;

        int totalWidth = maxRight - minX;
        int totalHeight = maxBottom - minY;

        // Target ~500px wide canvas
        if (totalWidth > 0)
        {
            CanvasScale = 500.0 / totalWidth;
            // Ensure height also fits
            double heightScale = 350.0 / totalHeight;
            if (heightScale < CanvasScale) CanvasScale = heightScale;
        }

        SelectedMonitor ??= Monitors.FirstOrDefault(m => m.IsPrimary)
                            ?? Monitors.FirstOrDefault();
    }

    public void Dispose()
    {
        _subscriptions.Dispose();
    }
}
```

- [ ] **Step 2: Implement MonitorsPage.xaml**

Replace `examples/WindowManager.Demo/src/WindowManager.Demo/Views/MonitorsPage.xaml`:

```xml
<Page
    x:Class="WindowManager.Demo.Views.MonitorsPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
    xmlns:local="clr-namespace:WindowManager.Demo.Views"
    d:DataContext="{d:DesignInstance local:MonitorsPage, IsDesignTimeCreatable=False}"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d"
    Loaded="OnLoaded">

    <Grid Margin="24,16">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>

        <ui:TextBlock Text="Monitor Layout" FontTypography="Subtitle" Margin="0,0,0,16" />

        <!-- Monitor canvas — drawn programmatically in code-behind -->
        <Canvas
            x:Name="MonitorCanvas"
            Grid.Row="1"
            Background="Transparent"
            MinHeight="200" />

        <!-- Selected monitor details -->
        <ui:Card Grid.Row="2" Margin="0,16,0,0">
            <ui:Card.Style>
                <Style TargetType="ui:Card">
                    <Setter Property="Visibility" Value="Visible" />
                    <Style.Triggers>
                        <DataTrigger Binding="{Binding ViewModel.SelectedMonitor}" Value="{x:Null}">
                            <Setter Property="Visibility" Value="Collapsed" />
                        </DataTrigger>
                    </Style.Triggers>
                </Style>
            </ui:Card.Style>
            <UniformGrid Columns="3">
                <StackPanel Margin="0,0,16,0">
                    <TextBlock Text="Device" FontSize="11" Opacity="0.6" />
                    <TextBlock Text="{Binding ViewModel.SelectedMonitor.DeviceName}" FontSize="13" />
                </StackPanel>
                <StackPanel Margin="0,0,16,0">
                    <TextBlock Text="Display" FontSize="11" Opacity="0.6" />
                    <TextBlock Text="{Binding ViewModel.SelectedMonitor.DisplayName}" FontSize="13" />
                </StackPanel>
                <StackPanel Margin="0,0,16,0">
                    <TextBlock Text="Primary" FontSize="11" Opacity="0.6" />
                    <TextBlock Text="{Binding ViewModel.SelectedMonitor.IsPrimary}" FontSize="13" />
                </StackPanel>
                <StackPanel Margin="0,8,16,0">
                    <TextBlock Text="Resolution" FontSize="11" Opacity="0.6" />
                    <TextBlock FontSize="13" FontFamily="Consolas">
                        <Run Text="{Binding ViewModel.SelectedMonitor.Bounds.Width, Mode=OneWay}" />
                        <Run Text=" x " />
                        <Run Text="{Binding ViewModel.SelectedMonitor.Bounds.Height, Mode=OneWay}" />
                    </TextBlock>
                </StackPanel>
                <StackPanel Margin="0,8,16,0">
                    <TextBlock Text="DPI" FontSize="11" Opacity="0.6" />
                    <TextBlock FontSize="13">
                        <Run Text="{Binding ViewModel.SelectedMonitor.Dpi, Mode=OneWay}" />
                        <Run Text=" (" />
                        <Run Text="{Binding ViewModel.SelectedMonitor.ScaleFactor, Mode=OneWay, StringFormat=P0}" />
                        <Run Text=")" />
                    </TextBlock>
                </StackPanel>
                <StackPanel Margin="0,8,16,0">
                    <TextBlock Text="Work Area" FontSize="11" Opacity="0.6" />
                    <TextBlock FontSize="13" FontFamily="Consolas">
                        <Run Text="{Binding ViewModel.SelectedMonitor.WorkArea.Width, Mode=OneWay}" />
                        <Run Text=" x " />
                        <Run Text="{Binding ViewModel.SelectedMonitor.WorkArea.Height, Mode=OneWay}" />
                    </TextBlock>
                </StackPanel>
            </UniformGrid>
        </ui:Card>
    </Grid>
</Page>
```

- [ ] **Step 3: Implement MonitorsPage.xaml.cs with canvas drawing**

Replace `examples/WindowManager.Demo/src/WindowManager.Demo/Views/MonitorsPage.xaml.cs`:

```csharp
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WindowManagement;
using WindowManager.Demo.ViewModels;
using Wpf.Ui.Abstractions.Controls;

namespace WindowManager.Demo.Views;

public partial class MonitorsPage : Page, INavigableView<MonitorsViewModel>
{
    public MonitorsViewModel ViewModel { get; }

    public MonitorsPage(MonitorsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();

        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(MonitorsViewModel.Monitors)
                or nameof(MonitorsViewModel.CanvasScale))
            {
                DrawMonitors();
            }
        };

        ViewModel.Monitors.CollectionChanged += (_, _) => DrawMonitors();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.OnNavigatedTo();
        DrawMonitors();
    }

    private void DrawMonitors()
    {
        MonitorCanvas.Children.Clear();
        double scale = ViewModel.CanvasScale;

        foreach (IMonitor monitor in ViewModel.Monitors)
        {
            double x = (monitor.Bounds.X + ViewModel.OffsetX) * scale;
            double y = (monitor.Bounds.Y + ViewModel.OffsetY) * scale;
            double w = monitor.Bounds.Width * scale;
            double h = monitor.Bounds.Height * scale;

            bool isSelected = monitor.Handle == ViewModel.SelectedMonitor?.Handle;

            var rect = new Border
            {
                Width = w,
                Height = h,
                BorderBrush = isSelected
                    ? new SolidColorBrush(Color.FromRgb(59, 130, 246))
                    : new SolidColorBrush(Color.FromRgb(60, 60, 80)),
                BorderThickness = new Thickness(isSelected ? 3 : 2),
                Background = isSelected
                    ? new SolidColorBrush(Color.FromArgb(30, 59, 130, 246))
                    : new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)),
                CornerRadius = new CornerRadius(4),
                Cursor = Cursors.Hand,
                Tag = monitor,
                Child = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = monitor.DeviceName,
                            FontSize = 12,
                            FontWeight = FontWeights.SemiBold,
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Center
                        },
                        new TextBlock
                        {
                            Text = $"{monitor.Bounds.Width}x{monitor.Bounds.Height}",
                            FontSize = 10,
                            Opacity = 0.7,
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Center
                        },
                        new TextBlock
                        {
                            Text = monitor.IsPrimary ? "Primary" : $"{monitor.Dpi} DPI",
                            FontSize = 10,
                            Opacity = 0.6,
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Center
                        }
                    }
                }
            };

            rect.MouseLeftButtonDown += (_, _) => ViewModel.SelectedMonitor = monitor;

            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            MonitorCanvas.Children.Add(rect);
        }
    }
}
```

- [ ] **Step 4: Build and verify**

```bash
cd examples/WindowManager.Demo
dotnet build WindowManager.Demo.sln
```

Expected: Build succeeds.

- [ ] **Step 5: Run and verify the Monitors tab**

```bash
cd examples/WindowManager.Demo
dotnet run --project src/WindowManager.Demo
```

Expected: Monitors tab shows scaled rectangles representing each connected monitor. Clicking a monitor highlights it and shows its details (device name, resolution, DPI, work area) in a card below.

- [ ] **Step 6: Commit**

```bash
git add examples/WindowManager.Demo/
git commit -m "feat: implement Monitors tab with visual layout and details"
```

---

### Task 6: Snap Tab

**Files:**
- Modify: `examples/WindowManager.Demo/src/WindowManager.Demo/ViewModels/SnapViewModel.cs`
- Modify: `examples/WindowManager.Demo/src/WindowManager.Demo/Views/SnapPage.xaml`
- Modify: `examples/WindowManager.Demo/src/WindowManager.Demo/Views/SnapPage.xaml.cs`

- [ ] **Step 1: Implement SnapViewModel**

Replace `examples/WindowManager.Demo/src/WindowManager.Demo/ViewModels/SnapViewModel.cs`:

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WindowManagement;
using WindowManager.Demo.Models;

namespace WindowManager.Demo.ViewModels;

public partial class SnapViewModel : ObservableObject
{
    private readonly IWindowManager _windowManager;

    [ObservableProperty]
    private WindowItem? _selectedWindow;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isStatusSuccess;

    public ObservableCollection<WindowItem> Windows { get; } = [];
    public ObservableCollection<IMonitor> Monitors { get; } = [];

    public SnapViewModel(IWindowManager windowManager)
    {
        _windowManager = windowManager;
    }

    public void OnNavigatedTo()
    {
        RefreshWindows();
        RefreshMonitors();
        PreSelectForeground();
    }

    [RelayCommand]
    private void RefreshWindows()
    {
        Windows.Clear();
        foreach (IWindow window in _windowManager.GetAll())
        {
            Windows.Add(new WindowItem(window));
        }
    }

    private void RefreshMonitors()
    {
        Monitors.Clear();
        foreach (IMonitor monitor in _windowManager.Monitors.All)
        {
            Monitors.Add(monitor);
        }
    }

    private void PreSelectForeground()
    {
        IWindow? foreground = _windowManager.GetForeground();
        if (foreground is not null)
        {
            SelectedWindow = Windows.FirstOrDefault(w => w.Handle == foreground.Handle);
        }
    }

    [RelayCommand]
    private async Task SnapToAsync(SnapRequest request)
    {
        if (SelectedWindow is null)
        {
            StatusMessage = "No window selected";
            IsStatusSuccess = false;
            return;
        }

        try
        {
            await _windowManager.SnapAsync(SelectedWindow.Window, request.Monitor, request.Position);
            StatusMessage = $"Snapped \"{SelectedWindow.Title}\" to {request.Position} on {request.Monitor.DeviceName}";
            IsStatusSuccess = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Snap failed: {ex.Message}";
            IsStatusSuccess = false;
            RefreshWindows();
        }
    }
}

public record SnapRequest(IMonitor Monitor, SnapPosition Position);
```

- [ ] **Step 2: Implement SnapPage.xaml**

Replace `examples/WindowManager.Demo/src/WindowManager.Demo/Views/SnapPage.xaml`:

```xml
<Page
    x:Class="WindowManager.Demo.Views.SnapPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
    xmlns:local="clr-namespace:WindowManager.Demo.Views"
    d:DataContext="{d:DesignInstance local:SnapPage, IsDesignTimeCreatable=False}"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d"
    Loaded="OnLoaded">

    <Grid Margin="24,16">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>

        <ui:TextBlock Text="Snap Window" FontTypography="Subtitle" Margin="0,0,0,12" />

        <!-- Window selector -->
        <StackPanel Grid.Row="1" Margin="0,0,0,16">
            <TextBlock Text="Target Window" FontSize="11" Opacity="0.6" Margin="0,0,0,4" />
            <ComboBox
                ItemsSource="{Binding ViewModel.Windows}"
                SelectedItem="{Binding ViewModel.SelectedWindow}"
                DisplayMemberPath="Title"
                HorizontalAlignment="Stretch"
                MaxWidth="500"
                HorizontalContentAlignment="Left" />
        </StackPanel>

        <!-- Monitor snap zones — drawn in code-behind -->
        <ScrollViewer Grid.Row="2" HorizontalScrollBarVisibility="Auto" VerticalScrollBarVisibility="Auto">
            <StackPanel x:Name="MonitorZonesPanel" Orientation="Horizontal" />
        </ScrollViewer>

        <!-- Status bar -->
        <ui:Card Grid.Row="3" Margin="0,12,0,0">
            <ui:Card.Style>
                <Style TargetType="ui:Card">
                    <Setter Property="Visibility" Value="Visible" />
                    <Style.Triggers>
                        <DataTrigger Binding="{Binding ViewModel.StatusMessage}" Value="">
                            <Setter Property="Visibility" Value="Collapsed" />
                        </DataTrigger>
                    </Style.Triggers>
                </Style>
            </ui:Card.Style>
            <TextBlock Text="{Binding ViewModel.StatusMessage}" FontSize="12" />
        </ui:Card>
    </Grid>
</Page>
```

- [ ] **Step 3: Implement SnapPage.xaml.cs with zone rendering**

Replace `examples/WindowManager.Demo/src/WindowManager.Demo/Views/SnapPage.xaml.cs`:

```csharp
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WindowManagement;
using WindowManager.Demo.ViewModels;
using Wpf.Ui.Abstractions.Controls;

namespace WindowManager.Demo.Views;

public partial class SnapPage : Page, INavigableView<SnapViewModel>
{
    public SnapViewModel ViewModel { get; }

    public SnapPage(SnapViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();

        ViewModel.Monitors.CollectionChanged += (_, _) => DrawSnapZones();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.OnNavigatedTo();
        DrawSnapZones();
    }

    private void DrawSnapZones()
    {
        MonitorZonesPanel.Children.Clear();

        // Find bounds for proportional scaling
        int maxWidth = 0, maxHeight = 0;
        foreach (IMonitor monitor in ViewModel.Monitors)
        {
            if (monitor.Bounds.Width > maxWidth) maxWidth = monitor.Bounds.Width;
            if (monitor.Bounds.Height > maxHeight) maxHeight = monitor.Bounds.Height;
        }

        if (maxWidth == 0) return;

        foreach (IMonitor monitor in ViewModel.Monitors)
        {
            // Scale so the largest monitor is 280px wide
            double scale = 280.0 / maxWidth;
            double w = monitor.Bounds.Width * scale;
            double h = monitor.Bounds.Height * scale;

            var monitorPanel = new StackPanel { Margin = new Thickness(0, 0, 16, 0) };

            // Monitor label
            monitorPanel.Children.Add(new TextBlock
            {
                Text = $"{monitor.DeviceName} — {monitor.Bounds.Width}x{monitor.Bounds.Height} @ {monitor.Dpi} DPI",
                FontSize = 11,
                Opacity = 0.7,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            });

            // Quadrant grid (2x2)
            var quadGrid = new Grid
            {
                Width = w,
                Height = h,
            };

            quadGrid.RowDefinitions.Add(new RowDefinition());
            quadGrid.RowDefinitions.Add(new RowDefinition());
            quadGrid.ColumnDefinitions.Add(new ColumnDefinition());
            quadGrid.ColumnDefinitions.Add(new ColumnDefinition());

            AddZoneButton(quadGrid, monitor, SnapPosition.TopLeft, "Top\nLeft", 0, 0);
            AddZoneButton(quadGrid, monitor, SnapPosition.TopRight, "Top\nRight", 0, 1);
            AddZoneButton(quadGrid, monitor, SnapPosition.BottomLeft, "Bottom\nLeft", 1, 0);
            AddZoneButton(quadGrid, monitor, SnapPosition.BottomRight, "Bottom\nRight", 1, 1);

            monitorPanel.Children.Add(quadGrid);

            // Half and Fill buttons
            var halfPanel = new UniformGrid
            {
                Columns = 5,
                Width = w,
                Margin = new Thickness(0, 4, 0, 0)
            };

            AddHalfButton(halfPanel, monitor, SnapPosition.Left, "Left");
            AddHalfButton(halfPanel, monitor, SnapPosition.Right, "Right");
            AddHalfButton(halfPanel, monitor, SnapPosition.Top, "Top");
            AddHalfButton(halfPanel, monitor, SnapPosition.Bottom, "Bottom");
            AddHalfButton(halfPanel, monitor, SnapPosition.Fill, "Fill");

            monitorPanel.Children.Add(halfPanel);
            MonitorZonesPanel.Children.Add(monitorPanel);
        }
    }

    private void AddZoneButton(Grid grid, IMonitor monitor, SnapPosition position, string label, int row, int col)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(50, 50, 70)),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(2),
            CornerRadius = new CornerRadius(3),
            Cursor = Cursors.Hand,
            Child = new TextBlock
            {
                Text = label,
                FontSize = 11,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                LineHeight = 16
            }
        };

        border.MouseEnter += (s, _) =>
            ((Border)s!).Background = new SolidColorBrush(Color.FromArgb(50, 59, 130, 246));
        border.MouseLeave += (s, _) =>
            ((Border)s!).Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255));
        border.MouseLeftButtonDown += async (_, _) =>
            await ViewModel.SnapToCommand.ExecuteAsync(new SnapRequest(monitor, position));

        Grid.SetRow(border, row);
        Grid.SetColumn(border, col);
        grid.Children.Add(border);
    }

    private void AddHalfButton(Panel panel, IMonitor monitor, SnapPosition position, string label)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(50, 50, 70)),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(1),
            CornerRadius = new CornerRadius(3),
            Cursor = Cursors.Hand,
            Height = 28,
            Child = new TextBlock
            {
                Text = label,
                FontSize = 10,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        border.MouseEnter += (s, _) =>
            ((Border)s!).Background = new SolidColorBrush(Color.FromArgb(50, 59, 130, 246));
        border.MouseLeave += (s, _) =>
            ((Border)s!).Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255));
        border.MouseLeftButtonDown += async (_, _) =>
            await ViewModel.SnapToCommand.ExecuteAsync(new SnapRequest(monitor, position));

        panel.Children.Add(border);
    }
}
```

- [ ] **Step 4: Build and verify**

```bash
cd examples/WindowManager.Demo
dotnet build WindowManager.Demo.sln
```

Expected: Build succeeds.

- [ ] **Step 5: Run and verify the Snap tab**

```bash
cd examples/WindowManager.Demo
dotnet run --project src/WindowManager.Demo
```

Expected: Snap tab shows a dropdown of windows (pre-selects the foreground window). Below that, each monitor is rendered with clickable quadrant zones and half/fill buttons. Clicking a zone snaps the selected window to that position. Status bar shows confirmation.

- [ ] **Step 6: Commit**

```bash
git add examples/WindowManager.Demo/
git commit -m "feat: implement Snap tab with interactive snap zones"
```

---

### Task 7: Events Tab

**Files:**
- Create: `examples/WindowManager.Demo/src/WindowManager.Demo/Models/EventEntry.cs`
- Modify: `examples/WindowManager.Demo/src/WindowManager.Demo/ViewModels/EventsViewModel.cs`
- Modify: `examples/WindowManager.Demo/src/WindowManager.Demo/Views/EventsPage.xaml`
- Modify: `examples/WindowManager.Demo/src/WindowManager.Demo/Views/EventsPage.xaml.cs`

- [ ] **Step 1: Create EventEntry model**

Create `examples/WindowManager.Demo/src/WindowManager.Demo/Models/EventEntry.cs`:

```csharp
namespace WindowManager.Demo.Models;

public class EventEntry
{
    public DateTime Timestamp { get; init; }
    public string TimestampText => Timestamp.ToString("HH:mm:ss.fff");
    public string EventType { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
}
```

- [ ] **Step 2: Implement EventsViewModel**

Replace `examples/WindowManager.Demo/src/WindowManager.Demo/ViewModels/EventsViewModel.cs`:

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using R3;
using WindowManagement;
using WindowManager.Demo.Models;

namespace WindowManager.Demo.ViewModels;

public partial class EventsViewModel : ObservableObject, IDisposable
{
    private readonly IDisposable _subscriptions;

    [ObservableProperty]
    private bool _showWindowCreated = true;

    [ObservableProperty]
    private bool _showWindowDestroyed = true;

    [ObservableProperty]
    private bool _showWindowMoved = true;

    [ObservableProperty]
    private bool _showWindowResized = true;

    [ObservableProperty]
    private bool _showStateChanged = true;

    [ObservableProperty]
    private bool _showMonitorEvents = true;

    [ObservableProperty]
    private bool _autoScroll = true;

    public ObservableCollection<EventEntry> Events { get; } = [];
    public ObservableCollection<EventEntry> FilteredEvents { get; } = [];

    public EventsViewModel(IWindowManager windowManager)
    {
        _subscriptions = Disposable.Combine(
            windowManager.Created.Subscribe(e =>
                AddEvent("Window Created", $"{e.Title} ({e.ProcessName})")),
            windowManager.Destroyed.Subscribe(e =>
                AddEvent("Window Destroyed", $"{e.Title} ({e.ProcessName})")),
            windowManager.Moved.Subscribe(e =>
                AddEvent("Window Moved", $"{e.Title} — ({e.OldBounds.X},{e.OldBounds.Y}) -> ({e.NewBounds.X},{e.NewBounds.Y})")),
            windowManager.Resized.Subscribe(e =>
                AddEvent("Window Resized", $"{e.Title} — {e.OldBounds.Width}x{e.OldBounds.Height} -> {e.NewBounds.Width}x{e.NewBounds.Height}")),
            windowManager.StateChanged.Subscribe(e =>
                AddEvent("State Changed", $"{e.Title} — {e.OldState} -> {e.NewState}")),
            windowManager.Monitors.Connected.Subscribe(e =>
                AddEvent("Monitor Connected", $"{e.DeviceName} — {e.Bounds.Width}x{e.Bounds.Height}")),
            windowManager.Monitors.Disconnected.Subscribe(e =>
                AddEvent("Monitor Disconnected", e.DeviceName)),
            windowManager.Monitors.SettingsChanged.Subscribe(e =>
                AddEvent("Monitor Settings", $"{e.DeviceName} — {e.Bounds.Width}x{e.Bounds.Height}"))
        );
    }

    private void AddEvent(string type, string details)
    {
        var entry = new EventEntry
        {
            Timestamp = DateTime.Now,
            EventType = type,
            Details = details
        };

        // Must dispatch to UI thread since R3 events may come from any thread
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            Events.Insert(0, entry);
            if (ShouldShow(entry))
            {
                FilteredEvents.Insert(0, entry);
            }
        });
    }

    private bool ShouldShow(EventEntry entry) => entry.EventType switch
    {
        "Window Created" => ShowWindowCreated,
        "Window Destroyed" => ShowWindowDestroyed,
        "Window Moved" => ShowWindowMoved,
        "Window Resized" => ShowWindowResized,
        "State Changed" => ShowStateChanged,
        "Monitor Connected" or "Monitor Disconnected" or "Monitor Settings" => ShowMonitorEvents,
        _ => true
    };

    [RelayCommand]
    private void ClearEvents()
    {
        Events.Clear();
        FilteredEvents.Clear();
    }

    partial void OnShowWindowCreatedChanged(bool value) => RebuildFiltered();
    partial void OnShowWindowDestroyedChanged(bool value) => RebuildFiltered();
    partial void OnShowWindowMovedChanged(bool value) => RebuildFiltered();
    partial void OnShowWindowResizedChanged(bool value) => RebuildFiltered();
    partial void OnShowStateChangedChanged(bool value) => RebuildFiltered();
    partial void OnShowMonitorEventsChanged(bool value) => RebuildFiltered();

    private void RebuildFiltered()
    {
        FilteredEvents.Clear();
        foreach (EventEntry entry in Events)
        {
            if (ShouldShow(entry))
            {
                FilteredEvents.Add(entry);
            }
        }
    }

    public void Dispose()
    {
        _subscriptions.Dispose();
    }
}
```

- [ ] **Step 3: Implement EventsPage.xaml**

Replace `examples/WindowManager.Demo/src/WindowManager.Demo/Views/EventsPage.xaml`:

```xml
<Page
    x:Class="WindowManager.Demo.Views.EventsPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
    xmlns:local="clr-namespace:WindowManager.Demo.Views"
    d:DataContext="{d:DesignInstance local:EventsPage, IsDesignTimeCreatable=False}"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d">

    <Grid Margin="24,16">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <DockPanel Margin="0,0,0,12">
            <ui:TextBlock Text="Live Events" FontTypography="Subtitle" DockPanel.Dock="Left" />
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
                <CheckBox Content="Auto-scroll" IsChecked="{Binding ViewModel.AutoScroll}" Margin="0,0,12,0" />
                <ui:Button Content="Clear" Appearance="Secondary" Icon="{ui:SymbolIcon Delete24}" Command="{Binding ViewModel.ClearEventsCommand}" />
            </StackPanel>
        </DockPanel>

        <!-- Filter checkboxes -->
        <WrapPanel Grid.Row="1" Margin="0,0,0,8">
            <CheckBox Content="Created" IsChecked="{Binding ViewModel.ShowWindowCreated}" Margin="0,0,12,4" />
            <CheckBox Content="Destroyed" IsChecked="{Binding ViewModel.ShowWindowDestroyed}" Margin="0,0,12,4" />
            <CheckBox Content="Moved" IsChecked="{Binding ViewModel.ShowWindowMoved}" Margin="0,0,12,4" />
            <CheckBox Content="Resized" IsChecked="{Binding ViewModel.ShowWindowResized}" Margin="0,0,12,4" />
            <CheckBox Content="State" IsChecked="{Binding ViewModel.ShowStateChanged}" Margin="0,0,12,4" />
            <CheckBox Content="Monitor" IsChecked="{Binding ViewModel.ShowMonitorEvents}" Margin="0,0,12,4" />
        </WrapPanel>

        <!-- Event list -->
        <ListView
            x:Name="EventsList"
            Grid.Row="2"
            ItemsSource="{Binding ViewModel.FilteredEvents}"
            ScrollViewer.CanContentScroll="True"
            VirtualizingPanel.IsVirtualizing="True">
            <ListView.View>
                <GridView>
                    <GridViewColumn Header="Time" Width="100" DisplayMemberBinding="{Binding TimestampText}" />
                    <GridViewColumn Header="Event" Width="140" DisplayMemberBinding="{Binding EventType}" />
                    <GridViewColumn Header="Details" Width="500" DisplayMemberBinding="{Binding Details}" />
                </GridView>
            </ListView.View>
        </ListView>
    </Grid>
</Page>
```

- [ ] **Step 4: Update EventsPage.xaml.cs with auto-scroll**

Replace `examples/WindowManager.Demo/src/WindowManager.Demo/Views/EventsPage.xaml.cs`:

```csharp
using System.Collections.Specialized;
using System.Windows.Controls;
using WindowManager.Demo.ViewModels;
using Wpf.Ui.Abstractions.Controls;

namespace WindowManager.Demo.Views;

public partial class EventsPage : Page, INavigableView<EventsViewModel>
{
    public EventsViewModel ViewModel { get; }

    public EventsPage(EventsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();

        ViewModel.FilteredEvents.CollectionChanged += OnEventsChanged;
    }

    private void OnEventsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (ViewModel.AutoScroll && e.Action == NotifyCollectionChangedAction.Add && EventsList.Items.Count > 0)
        {
            EventsList.ScrollIntoView(EventsList.Items[0]);
        }
    }
}
```

- [ ] **Step 5: Build and verify**

```bash
cd examples/WindowManager.Demo
dotnet build WindowManager.Demo.sln
```

Expected: Build succeeds.

- [ ] **Step 6: Run and verify the Events tab**

```bash
cd examples/WindowManager.Demo
dotnet run --project src/WindowManager.Demo
```

Expected: Events tab shows a live feed. Move or resize windows on the desktop — events appear in real time. Filter checkboxes toggle event types. Clear button empties the log. Auto-scroll keeps newest event visible.

- [ ] **Step 7: Commit**

```bash
git add examples/WindowManager.Demo/
git commit -m "feat: implement Events tab with live R3 event feed"
```

---

### Task 8: CLAUDE.md and Final Polish

**Files:**
- Create: `examples/WindowManager.Demo/CLAUDE.md`

- [ ] **Step 1: Create CLAUDE.md**

Create `examples/WindowManager.Demo/CLAUDE.md`:

```markdown
# CLAUDE.md

## Project

WindowManager.Demo — a WPF example app showcasing the WindowManagementApi library. Built with WPF-UI, Autofac, CommunityToolkit.Mvvm, and R3.

## Build & Run

```bash
dotnet build WindowManager.Demo.sln
dotnet run --project src/WindowManager.Demo
```

Requires a Windows desktop session with at least one monitor. The app manifest sets per-monitor v2 DPI awareness.

## Architecture

Single-project WPF app using:
- **WPF-UI**: FluentWindow, Mica backdrop, NavigationView sidebar
- **Autofac**: DI container with `WindowManagementModule` from the library
- **CommunityToolkit.Mvvm**: Source-generated ViewModels (`[ObservableProperty]`, `[RelayCommand]`)
- **R3**: Reactive event subscriptions for live event feed

4 tabs: Windows (list + details), Monitors (visual layout), Snap (interactive zones), Events (live feed).

## Key Patterns

- Pages implement `INavigableView<TViewModel>` with `DataContext = this`
- XAML binds through `{Binding ViewModel.Property}`
- ViewModels that subscribe to R3 observables implement `IDisposable`
- Snap zones are drawn programmatically in code-behind for each monitor
```

- [ ] **Step 2: Final build verification**

```bash
cd examples/WindowManager.Demo
dotnet build WindowManager.Demo.sln
```

Expected: Build succeeds with 0 errors, 0 warnings.

- [ ] **Step 3: Full app smoke test**

```bash
cd examples/WindowManager.Demo
dotnet run --project src/WindowManager.Demo
```

Verify all 4 tabs:
1. **Windows** — lists open windows, clicking shows details, search filters
2. **Monitors** — shows scaled monitor layout, clicking shows details
3. **Snap** — dropdown selects a window, clicking zones snaps it
4. **Events** — live feed updates when windows move/resize, filters work

- [ ] **Step 4: Commit**

```bash
git add examples/WindowManager.Demo/CLAUDE.md
git commit -m "docs: add CLAUDE.md for WindowManager.Demo example app"
```
