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
