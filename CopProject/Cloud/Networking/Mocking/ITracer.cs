// Author = Simeon Kandan Sonar
// Roll Number = 112301031

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Cloud.Networking.Mocking;

/// <summary>
/// Abstraction over <see cref="Trace"/> (information, warning and error output) to be able to mock it.
/// The calling member name is captured automatically and added to every message.
/// </summary>
internal interface ITracer
{
    void Info(string message, [CallerMemberName] string member = "");

    void WriteWarn(string message, [CallerMemberName] string member = "");

    void WriteError(string message, [CallerMemberName] string member = "");
}
