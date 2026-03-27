using System.Collections.Specialized;
using System.Windows.Controls;
using WindowManager.Demo.ViewModels;
using Wpf.Ui.Abstractions.Controls;

namespace WindowManager.Demo.Views;

public partial class EventsPage : Page, INavigableView<EventsViewModel>
{
    public EventsViewModel ViewModel { get; }

    public EventsPage(EventsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();

        ViewModel.FilteredEvents.CollectionChanged += OnEventsChanged;
    }

    private void OnEventsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (ViewModel.AutoScroll && e.Action == NotifyCollectionChangedAction.Add && EventsList.Items.Count > 0)
        {
            EventsList.ScrollIntoView(EventsList.Items[0]);
        }
    }
}
