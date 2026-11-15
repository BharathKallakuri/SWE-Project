// Author: M Sreenath Karthick
// Roll Number: 112301042

using System;
using System.Text.RegularExpressions;

namespace Incidents;

/// <summary>
/// Helper class to filter incidents for a filter request.
/// </summary>
internal static class IncidentFilterHelper
{
    /// <summary>
    /// Checks if incident matches the filter
    /// </summary>
    public static bool Matches(
        Incident incident,
        IncidentFilter filter)
    {
        ArgumentNullException.ThrowIfNull(incident);
        ArgumentNullException.ThrowIfNull(filter);

        return MatchesTitle(incident, filter) &&
               MatchesStatus(incident, filter) &&
               MatchesCategory(incident, filter) &&
               MatchesSeverity(incident, filter) &&
               MatchesDate(incident, filter) &&
               MatchesCreator(incident, filter) &&
               MatchesLocation(incident, filter);
    }

    /// <summary>
    /// Checks if the incident matches the title filter.
    /// </summary>
    private static bool MatchesTitle(Incident incident, IncidentFilter filter)
    {
        if (string.IsNullOrWhiteSpace(filter.Title))
        {
            return true;
        }

        return Regex.IsMatch(
            incident.Title,
            filter.Title,
            RegexOptions.IgnoreCase,
            TimeSpan.FromMilliseconds(100));
    }

    /// <summary>
    /// Checks if the incident matches the status filter.
    /// </summary>
    private static bool MatchesStatus(Incident incident, IncidentFilter filter)
    {
        if (!filter.Status.HasValue)
        {
            return true; // by default
        }

        return incident.Status == filter.Status.Value;
    }

    /// <summary>
    /// Checks if the incident matches the category filter.
    /// </summary>
    private static bool MatchesCategory(Incident incident, IncidentFilter filter)
    {
        if (!filter.Category.HasValue)
        {
            return true; // by default
        }

        return incident.Category == filter.Category.Value;
    }

    /// <summary>
    /// Checks if the incident matches the severity filter.
    /// </summary>
    private static bool MatchesSeverity(Incident incident, IncidentFilter filter)
    {
        if (!filter.Severity.HasValue)
        {
            return true;
        }

        return incident.Severity == filter.Severity.Value;
    }

    /// <summary>
    /// Checks if the incident matches the date filter.
    /// </summary>
    private static bool MatchesDate(Incident incident, IncidentFilter filter)
    {
        if (filter.FromDateTime.HasValue && incident.CreationTime < filter.FromDateTime.Value)
        {
            return false;
        }

        if (filter.ToDateTime.HasValue && incident.CreationTime > filter.ToDateTime.Value)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if the incident matches the created by filter.
    /// </summary>
    private static bool MatchesCreator(Incident incident, IncidentFilter filter)
    {
        if (string.IsNullOrWhiteSpace(filter.CreatedBy))
        {
            return true;
        }

        return Regex.IsMatch(
            incident.CreatedBy,
            filter.CreatedBy,
            RegexOptions.IgnoreCase,
            TimeSpan.FromMilliseconds(100));
    }

    /// <summary>
    /// Checks if the incident matches the location filter.
    /// </summary>
    private static bool MatchesLocation(Incident incident, IncidentFilter filter)
    {
        if (filter.Location is null)
        {
            return true;
        }

        return MatchesIncidentLocation(incident.Location, filter.Location);
    }

    /// <summary>
    /// Checks if the incident address matchess with the filter address.
    /// </summary>
    private static bool MatchesAddress(IncidentLocation location, IncidentLocationFilter filter)
    {
        if (string.IsNullOrWhiteSpace(filter.Address))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(location.Address))
        {
            return false;
        }

        return location.Address.Contains(filter.Address, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the incident coordinates match the filter coordinates.
    /// </summary>
    private static bool MatchesCoordinates(IncidentLocation location, IncidentLocationFilter filter)
    {
        if (filter.Radius is null)
        {
            return location.Latitude == filter.Latitude &&
                   location.Longitude == filter.Longitude;
        }

        double distance = DistanceToKm(
            location.Latitude, location.Longitude,
            filter.Latitude, filter.Longitude);

        return distance <= filter.Radius.Value;
    }

    /// <summary>
    /// Checks if the incident matches the location filter.
    /// </summary>
    private static bool MatchesIncidentLocation(IncidentLocation location, IncidentLocationFilter filter)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(filter);

        return MatchesAddress(location, filter) &&
               MatchesCoordinates(location, filter);
    }

    /// <summary>
    /// Calculates the distance between two geographic coordinates using the Haversine formula.
    /// The code is from the following blog post:
    /// https://www.geeksforgeeks.org/dsa/haversine-formula-to-find-distance-between-two-points-on-a-sphere
    /// </summary>
    private static double DistanceToKm(
        double latitude1,
        double longitude1,
        double latitude2,
        double longitude2)
    {
        const double EarthRadiusKm = 6371.0;

        double lat1 = DegreesToRadians(latitude1);
        double lat2 = DegreesToRadians(latitude2);
        double deltaLat = DegreesToRadians(latitude2 - latitude1);
        double deltaLon = DegreesToRadians(longitude2 - longitude1);

        double a = Math.Pow(Math.Sin(deltaLat / 2), 2);
        a += Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(deltaLon / 2), 2);

        return EarthRadiusKm * 2 * Math.Asin(Math.Sqrt(a));
    }

    /// <summary>
    /// Converts Degree to Radians.
    /// </summary>
    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

}
