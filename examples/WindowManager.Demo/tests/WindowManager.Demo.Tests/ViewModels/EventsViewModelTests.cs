using FluentAssertions;
using NSubstitute;
using R3;
using WindowManagement;
using WindowManager.Demo.Models;
using WindowManager.Demo.ViewModels;

namespace WindowManager.Demo.Tests.ViewModels;

public class EventsViewModelTests : IDisposable
{
    private readonly IWindowManager _windowManager = Substitute.For<IWindowManager>();
    private readonly IMonitorService _monitorService = Substitute.For<IMonitorService>();
    private readonly EventsViewModel _sut;

    public EventsViewModelTests()
    {
        _windowManager.Monitors.Returns(_monitorService);
        _windowManager.Created.Returns(Observable.Empty<WindowEventArgs>());
        _windowManager.Destroyed.Returns(Observable.Empty<WindowEventArgs>());
        _windowManager.Moved.Returns(Observable.Empty<WindowMovedEventArgs>());
        _windowManager.Resized.Returns(Observable.Empty<WindowMovedEventArgs>());
        _windowManager.StateChanged.Returns(Observable.Empty<WindowStateEventArgs>());
        _monitorService.Connected.Returns(Observable.Empty<MonitorEventArgs>());
        _monitorService.Disconnected.Returns(Observable.Empty<MonitorEventArgs>());
        _monitorService.SettingsChanged.Returns(Observable.Empty<MonitorEventArgs>());

        _sut = new EventsViewModel(_windowManager);
    }

    public void Dispose() => _sut.Dispose();

    [Fact]
    public void ClearEvents__ClearsBothCollections()
    {
        _sut.Events.Add(CreateEntry("Window Created"));
        _sut.FilteredEvents.Add(CreateEntry("Window Created"));

        _sut.ClearEventsCommand.Execute(null);

        _sut.Events.Should().BeEmpty();
        _sut.FilteredEvents.Should().BeEmpty();
    }

    [Fact]
    public void ShowWindowCreated__Toggled_RebuildFiltered()
    {
        var created = CreateEntry("Window Created");
        var moved = CreateEntry("Window Moved");
        _sut.Events.Add(created);
        _sut.Events.Add(moved);
        _sut.FilteredEvents.Add(created);
        _sut.FilteredEvents.Add(moved);

        _sut.ShowWindowCreated = false;

        _sut.FilteredEvents.Should().HaveCount(1);
        _sut.FilteredEvents[0].EventType.Should().Be("Window Moved");
    }

    [Fact]
    public void ShowWindowDestroyed__Toggled_RebuildFiltered()
    {
        var destroyed = CreateEntry("Window Destroyed");
        var moved = CreateEntry("Window Moved");
        _sut.Events.Add(destroyed);
        _sut.Events.Add(moved);
        _sut.FilteredEvents.Add(destroyed);
        _sut.FilteredEvents.Add(moved);

        _sut.ShowWindowDestroyed = false;

        _sut.FilteredEvents.Should().HaveCount(1);
        _sut.FilteredEvents[0].EventType.Should().Be("Window Moved");
    }

    [Fact]
    public void ShowWindowMoved__Toggled_RebuildFiltered()
    {
        var moved = CreateEntry("Window Moved");
        var created = CreateEntry("Window Created");
        _sut.Events.Add(moved);
        _sut.Events.Add(created);
        _sut.FilteredEvents.Add(moved);
        _sut.FilteredEvents.Add(created);

        _sut.ShowWindowMoved = false;

        _sut.FilteredEvents.Should().HaveCount(1);
        _sut.FilteredEvents[0].EventType.Should().Be("Window Created");
    }

    [Fact]
    public void ShowWindowResized__Toggled_RebuildFiltered()
    {
        var resized = CreateEntry("Window Resized");
        _sut.Events.Add(resized);
        _sut.FilteredEvents.Add(resized);

        _sut.ShowWindowResized = false;

        _sut.FilteredEvents.Should().BeEmpty();
    }

    [Fact]
    public void ShowStateChanged__Toggled_RebuildFiltered()
    {
        var state = CreateEntry("State Changed");
        _sut.Events.Add(state);
        _sut.FilteredEvents.Add(state);

        _sut.ShowStateChanged = false;

        _sut.FilteredEvents.Should().BeEmpty();
    }

    [Fact]
    public void ShowMonitorEvents__Toggled_FiltersAllMonitorTypes()
    {
        var connected = CreateEntry("Monitor Connected");
        var disconnected = CreateEntry("Monitor Disconnected");
        var settings = CreateEntry("Monitor Settings");
        var window = CreateEntry("Window Created");
        _sut.Events.Add(connected);
        _sut.Events.Add(disconnected);
        _sut.Events.Add(settings);
        _sut.Events.Add(window);
        _sut.FilteredEvents.Add(connected);
        _sut.FilteredEvents.Add(disconnected);
        _sut.FilteredEvents.Add(settings);
        _sut.FilteredEvents.Add(window);

        _sut.ShowMonitorEvents = false;

        _sut.FilteredEvents.Should().HaveCount(1);
        _sut.FilteredEvents[0].EventType.Should().Be("Window Created");
    }

    [Fact]
    public void FilterToggle__ReEnabling_RestoresEvents()
    {
        var created = CreateEntry("Window Created");
        _sut.Events.Add(created);
        _sut.FilteredEvents.Add(created);

        _sut.ShowWindowCreated = false;
        _sut.FilteredEvents.Should().BeEmpty();

        _sut.ShowWindowCreated = true;
        _sut.FilteredEvents.Should().HaveCount(1);
    }

    [Fact]
    public void AutoScroll__DefaultsToTrue()
    {
        _sut.AutoScroll.Should().BeTrue();
    }

    private static EventEntry CreateEntry(string eventType) => new()
    {
        Timestamp = DateTime.Now,
        EventType = eventType,
        Details = $"Test {eventType}"
    };
}
