using System.Windows.Controls;
using WindowManager.Demo.ViewModels;
using Wpf.Ui.Abstractions.Controls;

namespace WindowManager.Demo.Views;

public partial class SnapPage : Page, INavigableView<SnapViewModel>
{
    public SnapViewModel ViewModel { get; }

    public SnapPage(SnapViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}
