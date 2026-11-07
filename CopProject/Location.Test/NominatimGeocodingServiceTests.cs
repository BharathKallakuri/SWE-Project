using System.Net;
using Location.Test.Fakes;
using LocationTracker.Services;

namespace Location.Test;

/// <summary>
/// Tests for the Nominatim geocoding service with a fake HTTP handler.
/// </summary>
[TestClass]
public class NominatimGeocodingServiceTests
{
    private static NominatimGeocodingService CreateService(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        FakeHttpMessageHandler handler = new(responseJson, statusCode);
        return new NominatimGeocodingService(new HttpClient(handler));
    }

    /// <summary>
    /// A response with a display name should return that name.
    /// </summary>
    /// <returns>A task for the asynchronous test.</returns>
    [TestMethod]
    public async Task GetAddressAsyncDisplayNamePresent()
    {
        NominatimGeocodingService service = CreateService("""{ "display_name": "IIT Palakkad, Kerala, India" }""");

        string? address = await service.GetAddressAsync(10.8069, 76.7254);

        Assert.AreEqual("IIT Palakkad, Kerala, India", address);
    }

    /// <summary>
    /// A response without a display name should return null.
    /// </summary>
    /// <returns>A task for the asynchronous test.</returns>
    [TestMethod]
    public async Task GetAddressAsyncDisplayNameMissing()
    {
        NominatimGeocodingService service = CreateService("""{ "error": "Unable to geocode" }""");

        string? address = await service.GetAddressAsync(0, 0);

        Assert.IsNull(address);
    }

    /// <summary>
    /// Invalid coordinates should return null without sending any request.
    /// </summary>
    /// <returns>A task for the asynchronous test.</returns>
    [TestMethod]
    public async Task GetAddressAsyncInvalidCoordinates()
    {
        FakeHttpMessageHandler handler = new("""{ "display_name": "unused" }""");
        NominatimGeocodingService service = new(new HttpClient(handler));

        string? address = await service.GetAddressAsync(91, 0);

        Assert.IsNull(address);
        Assert.AreEqual(0, handler.RequestCount);
    }

    /// <summary>
    /// The first search result should be used.
    /// </summary>
    /// <returns>A task for the asynchronous test.</returns>
    [TestMethod]
    public async Task FindCoordinatesAsyncResultFound()
    {
        NominatimGeocodingService service = CreateService("""[ { "lat": "10.8069", "lon": "76.7254" } ]""");

        (double Latitude, double Longitude)? result = await service.FindCoordinatesAsync("IIT Palakkad");

        Assert.AreEqual((10.8069, 76.7254), result);
    }

    /// <summary>
    /// An empty result list should return null.
    /// </summary>
    /// <returns>A task for the asynchronous test.</returns>
    [TestMethod]
    public async Task FindCoordinatesAsyncNoResults()
    {
        NominatimGeocodingService service = CreateService("[]");

        (double Latitude, double Longitude)? result = await service.FindCoordinatesAsync("Nowhere");

        Assert.IsNull(result);
    }

    /// <summary>
    /// A blank address should return null without sending any request.
    /// </summary>
    /// <returns>A task for the asynchronous test.</returns>
    [TestMethod]
    public async Task FindCoordinatesAsyncBlankAddress()
    {
        FakeHttpMessageHandler handler = new("[]");
        NominatimGeocodingService service = new(new HttpClient(handler));

        (double Latitude, double Longitude)? result = await service.FindCoordinatesAsync("   ");

        Assert.IsNull(result);
        Assert.AreEqual(0, handler.RequestCount);
    }

    /// <summary>
    /// A server error should surface as an exception.
    /// </summary>
    /// <returns>A task for the asynchronous test.</returns>
    [TestMethod]
    public async Task GetAddressAsyncServerError()
    {
        NominatimGeocodingService service = CreateService(string.Empty, HttpStatusCode.InternalServerError);

        await Assert.ThrowsExactlyAsync<HttpRequestException>(() => service.GetAddressAsync(10, 10));
    }
}
