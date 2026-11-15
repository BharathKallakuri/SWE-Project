// Author: Shreesh Amit Chembeti
// Roll Number: 112301046

namespace Incidents;

/// <summary>
/// An interface for describing changes in an incident.
/// </summary>
public interface IChange
{
    string Description { get; }
    DateTime ChangedTime { get; }
    string ChangedBy { get; }
}
