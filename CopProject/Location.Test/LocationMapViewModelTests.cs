using System.Reflection;
using Incidents;
using LocationTracker.Interfaces;
using LocationTracker.Models;
using LocationTracker.ViewModels;

namespace Location.Test;

[TestClass]
public sealed class LocationMapViewModelTests
{
    private FakeMapService _mapService = null!;
    private LocationMapViewModel _viewModel = null!;

    [TestInitialize]
    public void Setup()
    {
        _mapService = new FakeMapService();
        _viewModel = new LocationMapViewModel(_mapService);
    }

    [TestMethod]
    public async Task LoadAsyncMapsIncidentFields()
    {
        Incident incident = CreateIncident(
            "Road accident",
            "Two vehicles collided.",
            IncidentCategory.Accident,
            IncidentSeverity.High,
            new IncidentLocation(10.8, 76.7, "Main Road"));
        _mapService.Incidents = [incident];

        await _viewModel.LoadAsync();

        MapIncident result = _viewModel.Incidents.Single();
        Assert.AreEqual(incident.Id, result.Id);
        Assert.AreEqual("Road accident", result.Title);
        Assert.AreEqual("Two vehicles collided.", result.Description);
        Assert.AreEqual("Accident", result.Category);
        Assert.AreEqual("High", result.Severity);
        Assert.AreEqual("Reported", result.Status);
        Assert.AreEqual(10.8, result.Latitude);
        Assert.AreEqual(76.7, result.Longitude);
        Assert.AreEqual("Main Road", result.Address);
    }

    [TestMethod]
    public async Task LoadAsyncUsesEscapedAndSelectedFilters()
    {
        _viewModel.TitleText = "A+B";
        _viewModel.SelectedCategory = nameof(IncidentCategory.Accident);
        _viewModel.SelectedSeverity = nameof(IncidentSeverity.High);
        _viewModel.SelectedStatus = nameof(IncidentStatus.Reported);

        await _viewModel.LoadAsync();

        Assert.IsNotNull(_mapService.LastFilter);
        Assert.AreEqual("A\\+B", _mapService.LastFilter.Title);
        Assert.AreEqual(IncidentCategory.Accident, _mapService.LastFilter.Category);
        Assert.AreEqual(IncidentSeverity.High, _mapService.LastFilter.Severity);
        Assert.AreEqual(IncidentStatus.Reported, _mapService.LastFilter.Status);
    }

    [TestMethod]
    public async Task LoadAsyncReportsServiceError()
    {
        _mapService.LoadException = new InvalidOperationException("test failure");

        await _viewModel.LoadAsync();

        Assert.AreEqual("Could not load incidents.", _viewModel.ErrorMessage);
        Assert.IsEmpty(_viewModel.Incidents);
    }

    [TestMethod]
    public void ReversedDateRangeSetsError()
    {
        _viewModel.FromDate = new DateTimeOffset(2026, 12, 10, 0, 0, 0, TimeSpan.Zero);
        _viewModel.ToDate = new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero);

        Assert.AreEqual(
            "The start date must be before the end date.",
            _viewModel.ErrorMessage);
    }

    [TestMethod]
    public async Task SearchAddressRaisesLocationEvent()
    {
        _mapService.SearchResult = new LocationSearchResult(10.8, 76.7, "Main Road");
        TaskCompletionSource<LocationSearchResult> locationReceived = new();
        _viewModel.LocationSearchRequested += (_, location) =>
            locationReceived.TrySetResult(location);

        _viewModel.AddressSearchText = "Main Road";
        _viewModel.SearchAddressCommand.Execute(null);

        LocationSearchResult result = await locationReceived.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.AreEqual(10.8, result.Latitude);
        Assert.AreEqual(76.7, result.Longitude);
        Assert.AreEqual("Main Road", result.Address);
        Assert.AreEqual("Main Road", _mapService.LastAddress);
    }

    [TestMethod]
    public async Task SearchAddressNotFoundSetsError()
    {
        _viewModel.AddressSearchText = "Unknown place";
        _viewModel.SearchAddressCommand.Execute(null);

        await Task.Delay(10);

        Assert.AreEqual("Address not found.", _viewModel.ErrorMessage);
    }

    [TestMethod]
    public async Task SearchAddressWithBlankTextSetsError()
    {
        _viewModel.SearchAddressCommand.Execute(null);

        await Task.Delay(10);

        Assert.AreEqual("Enter an address to search.", _viewModel.ErrorMessage);
        Assert.IsNull(_mapService.LastAddress);
    }

    private static Incident CreateIncident(
        string title,
        string description,
        IncidentCategory category,
        IncidentSeverity severity,
        IncidentLocation location)
    {
        return (Incident)Activator.CreateInstance(
            typeof(Incident),
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            [
                title,
                description,
                "test-user",
                category,
                severity,
                location,
                null
            ],
            null)!;
    }

    private sealed class FakeMapService : IMapService
    {
        public List<Incident> Incidents { get; set; } = [];
        public IncidentFilter? LastFilter { get; private set; }
        public string? LastAddress { get; private set; }
        public LocationSearchResult? SearchResult { get; set; }
        public Exception? LoadException { get; set; }

        public Task<List<Incident>> GetIncidentsForMapAsync(IncidentFilter? filter = null)
        {
            LastFilter = filter;
            if (LoadException is not null)
            {
                throw LoadException;
            }

            return Task.FromResult(Incidents);
        }

        public Task<IncidentLocation?> GetLocationForClickAsync(
            double latitude,
            double longitude)
        {
            return Task.FromResult<IncidentLocation?>(null);
        }

        public Task<LocationSearchResult?> SearchAddressAsync(string address)
        {
            LastAddress = address;
            return Task.FromResult(SearchResult);
        }
    }
}
