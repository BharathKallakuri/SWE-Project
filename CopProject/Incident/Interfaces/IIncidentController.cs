// Author: Shreesh Amit Chembeti
// Roll Number: 112301046

using System;
using System.Collections.Generic;

namespace Incidents;

/// <summary>
/// An interface for Incidents Module Controller.
/// This interface defines the contract for creating, retrieving, updating and correlating incidents.
/// </summary>
public interface IIncidentController
{
    Task<Incident> CreateIncident(CreateIncidentRequest request);

    /// <summary>
    /// Returns the incident with the specified id. If no such incident exists, an exception is thrown.
    /// </summary>
    Task<Incident> GetIncidentById(Guid id);

    /// <summary>
    /// Refreshes Incidents from the backend and updates the local cache.
    /// </summary>
    Task Refresh();

    /// <summary>
    /// Returns all incidents that satisfy the filter.
    /// </summary>
    Task<List<Incident>> GetAllIncidents(IncidentFilter? filter = null);

    /// <summary>
    /// Returns correlated incidents of an incident with the specified id.
    /// </summary>
    Task<List<Incident>> GetCorrelatedIncidents(Guid id);

    /// <summary>
    /// Add evidence to an incident.
    /// </summary>
    Task<bool> AddEvidence(Incident incident, List<(IncidentEvidence, byte[])> evidence);

    /// <summary>
    /// Update status of an incident.
    /// </summary>
    Task<bool> UpdateStatus(Incident incident, IncidentStatus newStatus);

    /// <summary>
    /// Update category of an incident.
    /// </summary>
    Task<bool> UpdateCategory(Incident incident, IncidentCategory newCategory);

    /// <summary>
    /// Update severity of an incident.
    /// </summary>
    Task<bool> UpdateSeverity(Incident incident, IncidentSeverity newSeverity);

    /// <summary>
    /// Update location of an incident.
    /// </summary>
    Task<bool> UpdateLocation(Incident incident, IncidentLocation location);

    /// <summary>
    /// Correlate two incidents with each other.
    /// </summary>
    Task<bool> CorrelateIncident(Incident incident1, Incident incident2);
}
