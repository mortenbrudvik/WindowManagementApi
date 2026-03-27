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
