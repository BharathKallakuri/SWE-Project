// Author: Saideep.
// Roll no : 112301018.
namespace Controller;

/// <summary>
/// It represents the lifecycle state of a meeting session.
/// Transitions from Created -> Active -> Ended.
/// </summary>
public enum SessionState
{
    Created = 0,
    Active = 1,
    Ended = 2
}
