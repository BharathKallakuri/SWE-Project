// Authot = Devapriya Pradeep
// Roll Number = 112301006

namespace Screenshare.Resilience;

/// <summary>
/// how busy the host's relay is. DispatcherLag is the number to watch: it is how long a new
/// work item waits behind everything already queued on the relay's single thread. near zero
/// is healthy; growing means decoding, encoding and fan-out no longer keep up, and senders
/// will start timing out and resending full frames.
/// </summary>
/// <param name="Streams">running streams</param>
/// <param name="Viewers">subscriptions across all streams</param>
/// <param name="LaggingViewers">viewers too far behind to receive deltas</param>
/// <param name="ViewersAwaitingFullFrame">viewers waiting for a full frame to start or resume from</param>
/// <param name="DispatcherLag">time a probe waited in the relay's queue</param>
public sealed record HostLoadSnapshot(
    int Streams,
    int Viewers,
    int LaggingViewers,
    int ViewersAwaitingFullFrame,
    TimeSpan DispatcherLag);
