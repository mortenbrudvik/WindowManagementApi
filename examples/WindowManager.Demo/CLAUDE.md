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
