// Author = Simeon Kandan Sonar
// Roll Number = 112301031

using System.Diagnostics;

namespace Cloud.Networking.Mocking;

/// <summary>
/// Abstraction over <see cref="Trace.WriteLine(string)"/> to be able to mock it.
/// </summary>
internal interface ITracer
{
    void WriteLine(string message);
}
