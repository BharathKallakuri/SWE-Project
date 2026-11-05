using System.Runtime.CompilerServices;
using Incidents;

namespace Location.Test.Fakes;

/// <summary>
/// Simple fake incident controller.
/// </summary>
internal sealed class FakeIncidentController : IIncidentController
{
    /// <summary>
    /// The list that GetAllIncidents returns.
    /// </summary>
    public List<Incident> Incidents { get; set; } = [];

    /// <summary>
    /// The incident that GetIncidentById returns.
    /// </summary>
    public Incident? IncidentById { get; set; }

    /// <summary>
    /// The filter passed to the last GetAllIncidents call.
    /// </summary>
    public IncidentFilter? LastFilter { get; private set; }

    /// <summary>
    /// Makes an empty incident for tests.
    /// </summary>
    /// <returns>An empty incident.</returns>
    public static Incident MakeIncident()
    {
        return (Incident)RuntimeHelpers.GetUninitializedObject(typeof(Incident));
    }
    public List<Incident> GetAllIncidents(IncidentFilter? filter = null)
    {
        LastFilter = filter;
        return Incidents;
    }
    public Incident GetIncidentById(Guid id)
    {
        return IncidentById ?? throw new KeyNotFoundException($"No incident with id {id}.");
    }
    public Incident CreateIncident(CreateIncidentRequest request)
    {
        throw new NotSupportedException();
    }
    public List<Incident> GetCorrelatedIncidents(Guid id)
    {
        throw new NotSupportedException();
    }
    public bool AddEvidence(Incident incident, List<IncidentEvidence> evidence)
    {
        throw new NotSupportedException();
    }
    public bool UpdateStatus(Incident incident, IncidentStatus newStatus)
    {
        throw new NotSupportedException();
    }
    public bool UpdateCategory(Incident incident, IncidentCategory newCategory)
    {
        throw new NotSupportedException();
    }
    public bool UpdateSeverity(Incident incident, IncidentSeverity newSeverity)
    {
        throw new NotSupportedException();
    }
    public bool UpdateLocation(Incident incident, IncidentLocation location)
    {
        throw new NotSupportedException();
    }
    public bool CorrelateIncident(Incident incident1, Incident incident2)
    {
        throw new NotSupportedException();
    }
}
