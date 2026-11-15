// Author = Srikar
// Roll Number = 142301013

namespace Networking;

/// <summary>
/// Listener interface for receiving lossy (UDP) messages.
/// </summary>
public interface IMessageListenerLossy
{
    /// <summary>
    /// Handles a received lossy message.
    /// </summary>
    /// <param name="message">The received message payload.</param>
    void OnMessageReceivedLossy(byte[] message);
}
