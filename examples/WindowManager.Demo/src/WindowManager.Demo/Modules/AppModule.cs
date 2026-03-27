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
        builder.RegisterType<ContentDialogService>().As<IContentDialogService>().SingleInstance();

        // Main window
        builder.RegisterType<MainWindow>().AsSelf().SingleInstance();
        builder.RegisterType<MainWindowViewModel>().AsSelf().SingleInstance();

        // Pages + ViewModels
        builder.RegisterType<WindowsPage>().AsSelf().SingleInstance();
        builder.RegisterType<WindowsViewModel>().AsSelf().InstancePerDependency();

        builder.RegisterType<MonitorsPage>().AsSelf().SingleInstance();
        builder.RegisterType<MonitorsViewModel>().AsSelf().InstancePerDependency();

        builder.RegisterType<SnapPage>().AsSelf().SingleInstance();
        builder.RegisterType<SnapViewModel>().AsSelf().InstancePerDependency();

        builder.RegisterType<EventsPage>().AsSelf().SingleInstance();
        builder.RegisterType<EventsViewModel>().AsSelf().InstancePerDependency();
    }
}
