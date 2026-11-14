namespace Incidents;

/// <summary>
/// Represents location where an Incident occured.
/// </summary>
public record IncidentLocation(
    double Latitude,
    double Longitude,
    string? Address = null
);

/// <summary>
/// Represents a incident location filter
/// </summary>
public record IncidentLocationFilter(
    double Latitude,
    double Longitude,
    string? Address = null,
    double? Radius = null
);
