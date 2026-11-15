// Author = Vishesh Srivastava, Ankit Gupta
// Roll Number = 112301037, 112301010

using Incidents;
using LocationTracker.Models;
namespace LocationTracker.Interfaces;

public interface IMapService
{
    Task<List<Incident>> GetIncidentsForMapAsync(IncidentFilter? filter = null);
    Task<IncidentLocation?> GetLocationForClickAsync(double latitude, double longitude);
    Task<LocationSearchResult?> SearchAddressAsync(string address);
}
