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
        System.Diagnostics.Debug.WriteLine($"Unhandled exception: {e.Exception}");
        e.Handled = true;
    }
}
