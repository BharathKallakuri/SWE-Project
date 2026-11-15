// Author = Simeon Kandan Sonar
// Roll Number = 112301031 

using System.Collections.Concurrent;
using System.Diagnostics;
using Cloud.Networking.Mocking;

namespace Cloud.Test;

/// <summary>
/// Tests the TracerProxy class.
/// These tests add and remove listeners on the process-wide <see cref="Trace.Listeners"/> collection,
/// so they must not run in parallel with each other or with other tests that write traces.
/// NEED TO BE CHECKED PROPERLY THAT IT DOES NOT CONFLICT WITH OTHER TESTS
/// </summary>
[TestClass]
[DoNotParallelize]
public class TestTracerProxy
{
    private sealed class CapturingListener : TraceListener
    {
        public ConcurrentQueue<(TraceEventType Type, string Message)> Events { get; } = new();

        public override void Write(string? message)
        {
        }

        public override void WriteLine(string? message)
        {
        }

        public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? message)
        {
            Events.Enqueue((eventType, message ?? string.Empty));
        }

        public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? format, params object?[]? args)
        {
            Events.Enqueue((eventType, args is null ? format ?? string.Empty : string.Format(format ?? string.Empty, args)));
        }
    }

    private const string ProjectName = "Cloud.Networking";

    /// <summary>Runs the action with a capturing listener registered and returns the single event it traced.</summary>
    private static (TraceEventType Type, string Message) Capture(Action action)
    {
        CapturingListener listener = new();
        Trace.Listeners.Add(listener);

        try
        {
            action();
        }
        finally
        {
            Trace.Listeners.Remove(listener);
        }

        Assert.AreEqual(1, listener.Events.Count);
        listener.Events.TryPeek(out (TraceEventType Type, string Message) traced);
        return traced;
    }

    /// <summary>Tests that Info forwards an information event with the project and caller names.</summary>
    [TestMethod]
    public void InfoForwardsToTraceListeners()
    {
        string message = $"tracer-proxy-test-{Guid.NewGuid()}";

        (TraceEventType type, string traced) = Capture(() => new TracerProxy().Info(message));

        Assert.AreEqual(TraceEventType.Information, type);
        Assert.AreEqual($"[{ProjectName}] {nameof(InfoForwardsToTraceListeners)}: {message}", traced);
    }

    /// <summary>Tests that WriteWarn forwards a warning event with the project and caller names.</summary>
    [TestMethod]
    public void WriteWarnForwardsToTraceListeners()
    {
        string message = $"tracer-proxy-test-{Guid.NewGuid()}";

        (TraceEventType type, string traced) = Capture(() => new TracerProxy().WriteWarn(message));

        Assert.AreEqual(TraceEventType.Warning, type);
        Assert.AreEqual($"[{ProjectName}] {nameof(WriteWarnForwardsToTraceListeners)}: {message}", traced);
    }

    /// <summary>Tests that WriteError forwards an error event with the project and caller names.</summary>
    [TestMethod]
    public void WriteErrorForwardsToTraceListeners()
    {
        string message = $"tracer-proxy-test-{Guid.NewGuid()}";

        (TraceEventType type, string traced) = Capture(() => new TracerProxy().WriteError(message));

        Assert.AreEqual(TraceEventType.Error, type);
        Assert.AreEqual($"[{ProjectName}] {nameof(WriteErrorForwardsToTraceListeners)}: {message}", traced);
    }

    /// <summary>Tests that an explicitly passed member name is used instead of the caller's.</summary>
    [TestMethod]
    public void ExplicitMemberNameIsUsed()
    {
        (_, string traced) = Capture(() => new TracerProxy().Info("hello", "CustomMember"));

        Assert.AreEqual($"[{ProjectName}] CustomMember: hello", traced);
    }
}
