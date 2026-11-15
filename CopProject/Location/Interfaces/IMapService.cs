using Incidents;
using LocationTracker.Models;
namespace LocationTracker.Interfaces;

public interface IMapService
{
    Task<List<Incident>> GetIncidentsForMapAsync(IncidentFilter? filter = null);
    Task<IncidentLocation?> GetLocationForClickAsync(double latitude, double longitude);
    Task<LocationSearchResult?> SearchAddressAsync(string address);
}
