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
