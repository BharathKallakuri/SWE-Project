namespace Incident_module;

public record IncidentFilter(
    string? Title = null,
    IncidentStatus? Status = null,
    IncidentCategory? Category = null,
    IncidentSeverity? Severity = null,
    DateTime? FromDateTime = null,
    DateTime? ToDateTime = null,
    Guid? CreatedBy = null,
    IncidentLocationFilter? Location = null
);
