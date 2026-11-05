using Incidents;
using Location.Test.Fakes;
using LocationTracker.Models;
using LocationTracker.Services;

namespace Location.Test;

/// <summary>
/// Tests for the MapService using fake dependencies.
/// </summary>
[TestClass]
public class MapServiceTests
{
    private FakeIncidentController _incidents = null!;
    private FakeGeocodingService _geocoding = null!;
    private MapService _service = null!;

    /// <summary>
    /// Sets up a map service with fakes before each test.
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        _incidents = new FakeIncidentController();
        _geocoding = new FakeGeocodingService();
        _service = new MapService(_incidents, _geocoding);
    }

    /// <summary>
    /// Without a filter the service should return whatever the controller returns.
    /// </summary>
    [TestMethod]
    public void GetIncidentsNoFilter()
    {
        _incidents.Incidents = [FakeIncidentController.MakeIncident()];

        List<Incident> result = _service.GetIncidentsForMap();

        Assert.HasCount(1, result);
        Assert.IsNull(_incidents.LastFilter);
    }

    /// <summary>
    /// A filter should be passed straight through to the controller.
    /// </summary>
    [TestMethod]
    public void GetIncidentsWithFilter()
    {
        IncidentFilter filter = new("Theft", null, null, null, null, null, null, null);

        _service.GetIncidentsForMap(filter);

        Assert.AreSame(filter, _incidents.LastFilter);
    }

    /// <summary>
    /// Looking up an incident by id should return the controller's incident.
    /// </summary>
    [TestMethod]
    public void GetIncidentById()
    {
        Incident incident = FakeIncidentController.MakeIncident();
        _incidents.IncidentById = incident;

        Incident result = _service.GetIncidentById(Guid.NewGuid());

        Assert.AreSame(incident, result);
    }

    /// <summary>
    /// Recent incidents should use a filter with only FromDateTime set.
    /// </summary>
    [TestMethod]
    public void GetRecentIncidents()
    {
        _service.GetRecentIncidents(TimeSpan.FromHours(2));

        Assert.IsNotNull(_incidents.LastFilter);
        Assert.IsNotNull(_incidents.LastFilter.FromDateTime);
        Assert.IsNull(_incidents.LastFilter.Location);
    }

    /// <summary>
    /// Nearby search should build a location filter with the given values.
    /// </summary>
    [TestMethod]
    public void GetNearbyIncidentsValidInput()
    {
        _service.GetNearbyIncidents(10.8, 76.7, 5);

        IncidentLocation? location = _incidents.LastFilter?.Location;
        Assert.IsNotNull(location);
        Assert.AreEqual(10.8, location.Latitude);
        Assert.AreEqual(76.7, location.Longitude);
        Assert.AreEqual(5.0, location.Radius);
    }

    /// <summary>
    /// Invalid coordinates should throw.
    /// </summary>
    [TestMethod]
    public void GetNearbyIncidentsInvalidCoordinates()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _service.GetNearbyIncidents(91, 0, 5));
    }

    /// <summary>
    /// A negative radius should throw.
    /// </summary>
    [TestMethod]
    public void GetNearbyIncidentsNegativeRadius()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _service.GetNearbyIncidents(10, 10, -1));
    }

    /// <summary>
    /// A map click on valid coordinates should return a location with the geocoded address.
    /// </summary>
    /// <returns>A task for the asynchronous test.</returns>
    [TestMethod]
    public async Task GetLocationForClickAsyncValidCoordinates()
    {
        _geocoding.Address = "Palakkad Junction";

        IncidentLocation? result = await _service.GetLocationForClickAsync(10.7993, 76.6376);

        Assert.IsNotNull(result);
        Assert.AreEqual(10.7993, result.Latitude);
        Assert.AreEqual("Palakkad Junction", result.Address);
    }

    /// <summary>
    /// A click on invalid coordinates should return null and skip geocoding.
    /// </summary>
    /// <returns>A task for the asynchronous test.</returns>
    [TestMethod]
    public async Task GetLocationForClickAsyncInvalidCoordinates()
    {
        IncidentLocation? result = await _service.GetLocationForClickAsync(95, 0);

        Assert.IsNull(result);
        Assert.AreEqual(0, _geocoding.GetAddressCalls);
    }

    /// <summary>
    /// A found address should come back with its coordinates.
    /// </summary>
    /// <returns>A task for the asynchronous test.</returns>
    [TestMethod]
    public async Task SearchAddressAsyncAddressFound()
    {
        _geocoding.Coordinates = (10.8069, 76.7254);

        LocationSearchResult? result = await _service.SearchAddressAsync("IIT Palakkad");

        Assert.IsNotNull(result);
        Assert.AreEqual(10.8069, result.Latitude);
        Assert.AreEqual("IIT Palakkad", result.Address);
    }

    /// <summary>
    /// An unknown address should return null.
    /// </summary>
    /// <returns>A task for the asynchronous test.</returns>
    [TestMethod]
    public async Task SearchAddressAsyncAddressNotFound()
    {
        _geocoding.Coordinates = null;

        LocationSearchResult? result = await _service.SearchAddressAsync("Nowhere at all");

        Assert.IsNull(result);
    }
}
