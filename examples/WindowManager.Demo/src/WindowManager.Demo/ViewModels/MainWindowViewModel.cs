using CommunityToolkit.Mvvm.ComponentModel;

namespace WindowManager.Demo.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _applicationTitle = "Window Manager Demo";
}
