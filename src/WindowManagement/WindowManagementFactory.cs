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
