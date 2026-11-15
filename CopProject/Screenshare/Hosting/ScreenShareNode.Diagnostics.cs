using System.Diagnostics;

namespace ScreenShare;

// load figures for the host. a partial of ScreenShareNode, so none of the existing files change.
public sealed partial class ScreenShareNode
{
    /// <summary>
    /// measures how busy the host's relay is; null on a participant that is not the host.
    /// the figures are read on the relay's own thread, so they are consistent, and the time the
    /// request waits there is reported as DispatcherLag. poll it every few seconds, not per frame.
    /// </summary>
    /// <exception cref="TimeoutException">the relay did not get to the request within 5 seconds, which is itself the finding</exception>
    public Task<HostLoadSnapshot?> GetHostLoadAsync()
    {
        ScreenShareHost? host = _host;

        if (host == null || _disposed)
        {
            return Task.FromResult<HostLoadSnapshot?>(null);
        }

        var completion = new TaskCompletionSource<HostLoadSnapshot?>(TaskCreationOptions.RunContinuationsAsynchronously);
        long queuedAt = Stopwatch.GetTimestamp();

        _dispatcher.Post(() =>
        {
            TimeSpan lag = Stopwatch.GetElapsedTime(queuedAt);
            completion.TrySetResult(host.CaptureLoad(lag));
        });

        return completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }
}
