// Author = Vishesh Srivastava, Ankit Gupta
// Roll Number = 112301037, 112301010

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
    public async Task GetIncidentsNoFilter()
    {
        _incidents.Incidents = [FakeIncidentController.MakeIncident()];

        List<Incident> result = await _service.GetIncidentsForMapAsync();

        Assert.HasCount(1, result);
        Assert.IsNull(_incidents.LastFilter);
    }

    /// <summary>
    /// A filter should be passed straight through to the controller.
    /// </summary>
    [TestMethod]
    public async Task GetIncidentsWithFilter()
    {
        IncidentFilter filter = new("Theft", null, null, null, null, null, null, null);

        await _service.GetIncidentsForMapAsync(filter);

        Assert.AreSame(filter, _incidents.LastFilter);
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
