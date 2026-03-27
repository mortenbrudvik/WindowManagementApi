using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WindowManagement;
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

        ViewModel.Monitors.CollectionChanged += (_, _) => DrawMonitors();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.OnNavigatedTo();
        DrawMonitors();
    }

    private void DrawMonitors()
    {
        MonitorCanvas.Children.Clear();
        double scale = ViewModel.CanvasScale;

        foreach (IMonitor monitor in ViewModel.Monitors)
        {
            double x = (monitor.Bounds.X + ViewModel.OffsetX) * scale;
            double y = (monitor.Bounds.Y + ViewModel.OffsetY) * scale;
            double w = monitor.Bounds.Width * scale;
            double h = monitor.Bounds.Height * scale;

            bool isSelected = monitor.Handle == ViewModel.SelectedMonitor?.Handle;

            var rect = new Border
            {
                Width = w,
                Height = h,
                BorderBrush = isSelected
                    ? new SolidColorBrush(Color.FromRgb(59, 130, 246))
                    : new SolidColorBrush(Color.FromRgb(60, 60, 80)),
                BorderThickness = new Thickness(isSelected ? 3 : 2),
                Background = isSelected
                    ? new SolidColorBrush(Color.FromArgb(30, 59, 130, 246))
                    : new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)),
                CornerRadius = new CornerRadius(4),
                Cursor = Cursors.Hand,
                Child = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = monitor.DeviceName,
                            FontSize = 12,
                            FontWeight = FontWeights.SemiBold,
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Center
                        },
                        new TextBlock
                        {
                            Text = $"{monitor.Bounds.Width}x{monitor.Bounds.Height}",
                            FontSize = 10,
                            Opacity = 0.7,
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Center
                        },
                        new TextBlock
                        {
                            Text = monitor.IsPrimary ? "Primary" : $"{monitor.Dpi} DPI",
                            FontSize = 10,
                            Opacity = 0.6,
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Center
                        }
                    }
                }
            };

            rect.MouseLeftButtonDown += (_, _) =>
            {
                ViewModel.SelectedMonitor = monitor;
                DrawMonitors();
            };

            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            MonitorCanvas.Children.Add(rect);
        }
    }
}
