// Author = Bharath
// Roll Number = 112301013

namespace Networking;

/// <summary>
/// Listener interface for receiving reliable messages.
/// </summary>
public interface IMessageListener
{
    /// <summary>
    /// Handles a received reliable message.
    /// </summary>
    /// <param name="message">The received message payload.</param>
    void OnMessageReceived(byte[] message);
}
