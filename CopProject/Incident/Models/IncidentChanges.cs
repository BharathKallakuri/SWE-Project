namespace Incidents;

/// <summary>
/// Describes the category change in an incident.
/// </summary>
internal record CategoryChange(
    string Description,
    DateTime ChangedTime,
    string ChangedBy
) : IChange;

/// <summary>
/// Describes the status change in an incident.
/// </summary>
internal record StatusChange(
    string Description,
    DateTime ChangedTime,
    string ChangedBy
) : IChange;

/// <summary>
/// Describes the severity change in an incident.
/// </summary>
internal record SeverityChange(
    string Description,
    DateTime ChangedTime,
    string ChangedBy
) : IChange;

/// <summary>
/// Describes the location change in an incident.
/// </summary>
internal record LocationChange(
    string Description,
    DateTime ChangedTime,
    string ChangedBy
) : IChange;

/// <summary>
/// Describes correlation change in an incident.
/// </summary>
internal record IncidentCorrelationChange(
    string Description,
    DateTime ChangedTime,
    string ChangedBy
) : IChange;

/// <summary>
/// Describes Evidence added in an incident.
/// </summary>
internal record IncidentEvidenceAdd(
    string Description,
    DateTime ChangedTime,
    string ChangedBy
) : IChange;
