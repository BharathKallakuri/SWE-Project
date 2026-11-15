// Author = Ankit Gupta
// Roll Number = 112301010

using System.Diagnostics;
using Incidents;
using LocationTracker.Interfaces;
using LocationTracker.Models;

namespace LocationTracker.Services;

public class MapService : IMapService
{
    private readonly IIncidentController _incidents;
    private readonly IGeocodingService _geocoding;

    public MapService(IIncidentController incidents, IGeocodingService geocoding)
    {
        _incidents = incidents;
        _geocoding = geocoding;
    }

    /// <summary>
    /// Gets the incidents for the map and optionally filtered by the given filter
    /// </summary>
    public Task<List<Incident>> GetIncidentsForMapAsync(IncidentFilter? filter = null)
    {
        return _incidents.GetAllIncidents(filter);
    }

    /// <summary>
    /// Gets the location for a map click.
    /// </summary>
    public async Task<IncidentLocation?> GetLocationForClickAsync(double latitude, double longitude)
    {
        if (!Coordinates.IsValid(latitude, longitude))
        {
            Trace.TraceWarning($"[Location] MapService.GetLocationForClickAsync: Invalid coordinates ({latitude}, {longitude}).");
            return null;
        }

        string? address = await _geocoding.GetAddressAsync(latitude, longitude);
        return new IncidentLocation(latitude, longitude, address);
    }

    /// <summary>
    /// Searches for a location by address.
    /// </summary>
    public async Task<LocationSearchResult?> SearchAddressAsync(string address)
    {
        (double Latitude, double Longitude)? coordinates = await _geocoding.FindCoordinatesAsync(address);
        if (coordinates == null)
        {
            Trace.TraceInformation($"[Location] MapService.SearchAddressAsync: Could not find coordinates for address {address}.");
            return null;
        }

        return new LocationSearchResult(coordinates.Value.Latitude, coordinates.Value.Longitude, address);
    }

}
