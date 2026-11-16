// Author = Sanjaysiva A
// Roll Number = 112301030

using System.Diagnostics;

namespace ScreenShare;

/// <summary>
/// Values for interacting with Networking module
/// </summary>

public static class ScreenShareModule
{
    public const int ModuleId = 8;

    public const int ControlPriority = 3;

    public const int DataPriority = 1;

    public static int PriorityFor(IScreenShareMessage message)
    {
        if (message == null)
        {
            Trace.TraceWarning("[Screenshare] ScreenShareModule.PriorityFor: null Message, defaulting to ControlPriority.");
            return ControlPriority;
        }

        int priority = message.Kind == ScreenShareMessageKind.Data ? DataPriority : ControlPriority;
        Trace.TraceInformation("[Screenshare] ScreenShareModule.PriorityFor: Assigned priority {0} for message kind {1}.", priority, message.Kind);
        return priority;
    }
}
