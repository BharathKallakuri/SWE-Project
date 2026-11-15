// Author: Shreesh Amit Chembeti
// Roll Number: 112301046

namespace Incidents;

/// <summary>
/// Represents status of an incident.
/// </summary>
public enum IncidentStatus
{
    Reported, InProgress, Resolved,
}

/// <summary>
/// Represents category of an incident.
/// </summary>
public enum IncidentCategory
{
    DrunkDriving, Theft, Accident, Assault, Vandalism, Murder, Other,
}

/// <summary>
/// Represents severity of an incident.
/// </summary>
public enum IncidentSeverity
{
    Low, Medium, High, Critical,
}
