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
