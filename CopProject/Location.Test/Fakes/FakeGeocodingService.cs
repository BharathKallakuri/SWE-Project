using LocationTracker.Interfaces;

namespace Location.Test.Fakes;

/// <summary>
/// Simple fake geocoder with fixed answers.
/// </summary>
internal sealed class FakeGeocodingService : IGeocodingService
{
    /// <summary>
    /// The address that GetAddressAsync returns.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// The coordinates that FindCoordinatesAsync returns.
    /// </summary>
    public (double Latitude, double Longitude)? Coordinates { get; set; }

    /// <summary>
    /// How many times GetAddressAsync was called.
    /// </summary>
    public int GetAddressCalls { get; private set; }

    public Task<string?> GetAddressAsync(double latitude, double longitude)
    {
        GetAddressCalls++;
        return Task.FromResult(Address);
    }
    
    public Task<(double Latitude, double Longitude)?> FindCoordinatesAsync(string address)
    {
        return Task.FromResult(Coordinates);
    }
}
