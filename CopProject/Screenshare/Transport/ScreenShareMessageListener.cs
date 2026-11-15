// Author = Sanjaysiva A
// Roll Number = 112301030

using System.Diagnostics;
using Networking;

namespace ScreenShare;

internal sealed class ScreenShareMessageListener : IMessageListener, IMessageListenerLossy
{
    private readonly Action<IScreenShareMessage, byte[]> _onMessage;
    public ScreenShareMessageListener(Action<IScreenShareMessage, byte[]> onMessage)
    {
        ArgumentNullException.ThrowIfNull(onMessage);

        _onMessage = onMessage;
        Trace.TraceInformation("[Screenshare] ScreenShareMessageListener.constructor: Initialized message listener.");
    }

    public void OnMessageReceived(byte[] message)
    {
        if (ScreenShareCodec.Decode(message, out IScreenShareMessage? decoded))
        {
            Trace.TraceInformation("[Screenshare] ScreenShareMessageListener.OnMessageReceived: Successfully decoded message of kind {0} ({1} bytes).", decoded?.Kind, message?.Length ?? 0);
            try
            {
                _onMessage(decoded!, message);
            }
            catch (Exception ex)
            {
                Trace.TraceError("[Screenshare] ScreenShareMessageListener.OnMessageReceived: Failed to dispatch message of kind {0}: {1}", decoded?.Kind, ex);
                throw;
            }
        }
        else
        {
            Trace.TraceWarning("[Screenshare] ScreenShareMessageListener.OnMessageReceived: Dropped a bad message of {0} bytes.", message?.Length ?? 0);
        }
    }

    public void OnMessageReceivedLossy(byte[] message)
    {
        Trace.TraceInformation("[Screenshare] ScreenShareMessageListener.OnMessageReceivedLossy: Received lossy message of {0} bytes.", message?.Length ?? 0);
        OnMessageReceived(message);
    }
}
