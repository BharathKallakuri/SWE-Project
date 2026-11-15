// Author = Simeon Kandan Sonar
// Roll Number = 112301031

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Cloud.Networking.Mocking;

/// <summary>
/// Pass-through Proxy which forwards to <see cref="Trace.TraceInformation(string)"/>,
/// <see cref="Trace.TraceWarning(string)"/> and <see cref="Trace.TraceError(string)"/>
/// </summary>
internal sealed class TracerProxy : ITracer
{
    private static readonly string s_projectName = "Cloud.Networking";

    public void Info(string message, [CallerMemberName] string member = "")
    {
        Trace.TraceInformation(Format(message, member));
    }

    public void WriteWarn(string message, [CallerMemberName] string member = "")
    {
        Trace.TraceWarning(Format(message, member));
    }

    public void WriteError(string message, [CallerMemberName] string member = "")
    {
        Trace.TraceError(Format(message, member));
    }

    private static string Format(string message, string member)
    {
        return $"[{s_projectName}] {member}: {message}";
    }
}
