using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using WindowManagement;
using WindowManager.Demo.Models;
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
        ViewModel.Monitors.CollectionChanged += (_, _) => DrawSnapZones();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.OnNavigatedTo();
        DrawSnapZones();
    }

    private void DrawSnapZones()
    {
        MonitorZonesPanel.Children.Clear();

        int maxWidth = 0;
        foreach (IMonitor monitor in ViewModel.Monitors)
        {
            if (monitor.Bounds.Width > maxWidth) maxWidth = monitor.Bounds.Width;
        }
        if (maxWidth == 0) return;

        var textBrush = (Brush)FindResource("TextFillColorPrimaryBrush");
        var strokeBrush = (Brush)FindResource("ControlStrokeColorDefaultBrush");
        var subtleBrush = (Brush)FindResource("SubtleFillColorSecondaryBrush");
        var accentBrush = (Brush)FindResource("AccentTextFillColorPrimaryBrush");
        var hoverBackground = new SolidColorBrush(((SolidColorBrush)accentBrush).Color with { A = 50 });

        foreach (IMonitor monitor in ViewModel.Monitors)
        {
            double scale = 280.0 / maxWidth;
            double w = monitor.Bounds.Width * scale;
            double h = monitor.Bounds.Height * scale;

            var monitorPanel = new StackPanel { Margin = new Thickness(0, 0, 16, 0) };

            monitorPanel.Children.Add(new TextBlock
            {
                Text = $"{monitor.DeviceName} — {monitor.Bounds.Width}x{monitor.Bounds.Height} @ {monitor.Dpi} DPI",
                FontSize = 11, Opacity = 0.7, Foreground = textBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            });

            var quadGrid = new Grid { Width = w, Height = h };
            quadGrid.RowDefinitions.Add(new RowDefinition());
            quadGrid.RowDefinitions.Add(new RowDefinition());
            quadGrid.ColumnDefinitions.Add(new ColumnDefinition());
            quadGrid.ColumnDefinitions.Add(new ColumnDefinition());

            AddZoneButton(quadGrid, monitor, SnapPosition.TopLeft, "Top\nLeft", 0, 0, textBrush, strokeBrush, subtleBrush, hoverBackground);
            AddZoneButton(quadGrid, monitor, SnapPosition.TopRight, "Top\nRight", 0, 1, textBrush, strokeBrush, subtleBrush, hoverBackground);
            AddZoneButton(quadGrid, monitor, SnapPosition.BottomLeft, "Bottom\nLeft", 1, 0, textBrush, strokeBrush, subtleBrush, hoverBackground);
            AddZoneButton(quadGrid, monitor, SnapPosition.BottomRight, "Bottom\nRight", 1, 1, textBrush, strokeBrush, subtleBrush, hoverBackground);

            monitorPanel.Children.Add(quadGrid);

            var halfPanel = new UniformGrid { Columns = 5, Width = w, Margin = new Thickness(0, 4, 0, 0) };
            AddHalfButton(halfPanel, monitor, SnapPosition.Left, "Left", textBrush, strokeBrush, subtleBrush, hoverBackground);
            AddHalfButton(halfPanel, monitor, SnapPosition.Right, "Right", textBrush, strokeBrush, subtleBrush, hoverBackground);
            AddHalfButton(halfPanel, monitor, SnapPosition.Top, "Top", textBrush, strokeBrush, subtleBrush, hoverBackground);
            AddHalfButton(halfPanel, monitor, SnapPosition.Bottom, "Bottom", textBrush, strokeBrush, subtleBrush, hoverBackground);
            AddHalfButton(halfPanel, monitor, SnapPosition.Fill, "Fill", textBrush, strokeBrush, subtleBrush, hoverBackground);

            monitorPanel.Children.Add(halfPanel);
            MonitorZonesPanel.Children.Add(monitorPanel);
        }
    }

    private void AddZoneButton(Grid grid, IMonitor monitor, SnapPosition position, string label,
        int row, int col, Brush textBrush, Brush strokeBrush, Brush subtleBrush, Brush hoverBrush)
    {
        var border = new Border
        {
            Background = subtleBrush,
            BorderBrush = strokeBrush,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(2),
            CornerRadius = new CornerRadius(3),
            Cursor = Cursors.Hand,
            Child = new TextBlock
            {
                Text = label, FontSize = 11, Foreground = textBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center, LineHeight = 16
            }
        };

        AutomationProperties.SetName(border, $"Snap {position} on {monitor.DeviceName}");

        border.MouseEnter += (s, _) => ((Border)s!).Background = hoverBrush;
        border.MouseLeave += (s, _) => ((Border)s!).Background = subtleBrush;
        border.MouseLeftButtonDown += async (_, _) =>
            await ViewModel.SnapToCommand.ExecuteAsync(new SnapRequest(monitor, position));

        Grid.SetRow(border, row);
        Grid.SetColumn(border, col);
        grid.Children.Add(border);
    }

    private void AddHalfButton(Panel panel, IMonitor monitor, SnapPosition position, string label,
        Brush textBrush, Brush strokeBrush, Brush subtleBrush, Brush hoverBrush)
    {
        var border = new Border
        {
            Background = subtleBrush,
            BorderBrush = strokeBrush,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(1),
            CornerRadius = new CornerRadius(3),
            Cursor = Cursors.Hand,
            Height = 28,
            Child = new TextBlock
            {
                Text = label, FontSize = 10, Foreground = textBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        AutomationProperties.SetName(border, $"Snap {position} on {monitor.DeviceName}");

        border.MouseEnter += (s, _) => ((Border)s!).Background = hoverBrush;
        border.MouseLeave += (s, _) => ((Border)s!).Background = subtleBrush;
        border.MouseLeftButtonDown += async (_, _) =>
            await ViewModel.SnapToCommand.ExecuteAsync(new SnapRequest(monitor, position));

        panel.Children.Add(border);
    }
}
