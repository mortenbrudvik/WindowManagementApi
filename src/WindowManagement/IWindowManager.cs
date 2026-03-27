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
