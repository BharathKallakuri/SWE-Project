using System.Collections.ObjectModel;
using Incident_module;
using LocationTracker.Interfaces;

namespace LocationTracker.ViewModels;

public sealed record MapIncident(
    Guid Id,
    string Title,
    string Description,
    string Category,
    string Severity,
    string Status,
    double Latitude,
    double Longitude,
    string? Address
);

public sealed class LocationMapViewModel
{
    private readonly IMapService _mapService;

    public ObservableCollection<MapIncident> Incidents { get; } = [];

    public LocationMapViewModel(IMapService mapService)
    {
        _mapService = mapService;
    }

    public async Task LoadAsync(IncidentFilter? filter = null)
    {
        List<Incident> incidents = await _mapService.GetIncidentsForMapAsync(filter);

        Incidents.Clear();
        foreach (Incident incident in incidents)
        {
            Incidents.Add(new MapIncident(
                incident.Id,
                incident.Title,
                incident.Description,
                incident.Category.ToString(),
                incident.Severity.ToString(),
                incident.Status.ToString(),
                incident.Location.Latitude,
                incident.Location.Longitude,
                incident.Location.Address));
        }
    }

}
