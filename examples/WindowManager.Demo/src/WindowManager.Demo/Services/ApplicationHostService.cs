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
