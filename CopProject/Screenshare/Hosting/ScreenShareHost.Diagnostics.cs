// Authot = Devapriya Pradeep
// Roll Number = 112301006

namespace ScreenShare;

// load figures for the relay. a partial of ScreenShareHost, so none of the existing files change.
// must run on the dispatcher thread, like everything else that touches the host's state;
// ScreenShareNode.GetHostLoadAsync does that.
internal sealed partial class ScreenShareHost
{
    internal HostLoadSnapshot CaptureLoad(TimeSpan dispatcherLag)
    {
        int viewers = 0;
        int lagging = 0;
        int awaitingFullFrame = 0;

        foreach (HostedStream stream in _streams.Values)
        {
            foreach (ViewerState viewer in stream.Viewers.Values)
            {
                viewers++;

                if (IsLagging(viewer))
                {
                    lagging++;
                }

                if (viewer.NeedsFullFrame)
                {
                    awaitingFullFrame++;
                }
            }
        }

        return new HostLoadSnapshot(_streams.Count, viewers, lagging, awaitingFullFrame, dispatcherLag);
    }
}
