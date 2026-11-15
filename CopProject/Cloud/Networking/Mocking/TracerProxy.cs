// Author = Simeon Kandan Sonar
// Roll Number = 112301031

using System.Diagnostics;

namespace Cloud.Networking.Mocking;

/// <summary>
/// Pass-through Proxy which forwards to <see cref="Trace.WriteLine(string)"/>
/// </summary>
internal sealed class TracerProxy : ITracer
{
    public void WriteLine(string message)
    {
        Trace.WriteLine(message);
    }
}
