using System.Windows.Controls;
using WindowManager.Demo.ViewModels;
using Wpf.Ui.Abstractions.Controls;

namespace WindowManager.Demo.Views;

public partial class MonitorsPage : Page, INavigableView<MonitorsViewModel>
{
    public MonitorsViewModel ViewModel { get; }

    public MonitorsPage(MonitorsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}
