// Author = Simeon Kandan Sonar
// Roll Number = 112301031 

using System.Collections.Concurrent;
using System.Diagnostics;
using Cloud.Networking.Mocking;

namespace Cloud.Test;

/// <summary>
/// Tests the TracerProxy class
/// </summary>
[TestClass]
public class TestTracerProxy
{
    private sealed class CapturingListener : TraceListener
    {
        public ConcurrentQueue<string> Lines { get; } = new();

        public override void Write(string? message)
        {
        }

        public override void WriteLine(string? message)
        {
            Lines.Enqueue(message ?? string.Empty);
        }
    }

    /// <summary>Tests that WriteLine forwards the message to the registered trace listeners.</summary>
    [TestMethod]
    public void WriteLineForwardsToTraceListeners()
    {
        CapturingListener listener = new();
        Trace.Listeners.Add(listener);

        try
        {
            string message = $"tracer-proxy-test-{Guid.NewGuid()}";

            new TracerProxy().WriteLine(message);

            Assert.IsTrue(listener.Lines.Contains(message));
        }
        finally
        {
            Trace.Listeners.Remove(listener);
        }
    }
}
