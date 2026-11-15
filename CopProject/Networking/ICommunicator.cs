// Author = Bharath
// Roll Number = 112301013

using System.Net;

namespace Networking;

/// <summary>
/// Defines communication operations for sending, receiving, broadcasting, and multicasting messages.
/// </summary>
public interface ICommunicator
{
    /// <summary>
    /// Gets the port used for listening.
    /// </summary>
    int ListenPort { get; }

    /// <summary>
    /// Gets the IP address used for listening.
    /// </summary>
    IPAddress ListenIpAddress { get; }

    /// <summary>
    /// Registers a listener for reliable messages sent to a specific module.
    /// </summary>
    /// <param name="module">The module identifier.</param>
    /// <param name="subscriber">The message listener callback.</param>
    void AddSubscriber(int module, IMessageListener subscriber);

    /// <summary>
    /// Unregisters the listener for a specific module.
    /// </summary>
    /// <param name="module">The module identifier to remove.</param>
    void RemoveSubscriber(int module);

    /// <summary>
    /// Registers a listener for lossy (UDP) messages sent to a specific module.
    /// </summary>
    /// <param name="module">The module identifier.</param>
    /// <param name="subscriber">The lossy message listener callback.</param>
    void AddLossySubscriber(int module, IMessageListenerLossy subscriber);

    /// <summary>
    /// Unregisters the lossy message listener for a specific module.
    /// </summary>
    /// <param name="module">The module identifier to remove.</param>
    void RemoveLossySubscriber(int module);

    /// <summary>
    /// Sends a reliable message to a specific endpoint.
    /// </summary>
    /// <param name="iPEndPoint">Destination endpoint.</param>
    /// <param name="message">Payload data to send.</param>
    /// <param name="module">Target module identifier.</param>
    /// <param name="priority">Message priority level.</param>
    void SendMessage(IPEndPoint iPEndPoint, byte[] message, int module, int priority);

    /// <summary>
    /// Sends a lossy (UDP) message to a specific endpoint.
    /// </summary>
    /// <param name="iPEndPoint">Destination endpoint.</param>
    /// <param name="message">Payload data to send.</param>
    /// <param name="module">Target module identifier.</param>
    /// <param name="priority">Message priority level.</param>
    void SendMessageLossy(IPEndPoint iPEndPoint, byte[] message, int module, int priority);

    /// <summary>
    /// Broadcasts a reliable message to all connected clients for a given module.
    /// </summary>
    /// <param name="message">Payload data to broadcast.</param>
    /// <param name="module">Target module identifier.</param>
    /// <param name="priority">Message priority level.</param>
    void BroadcastMessage(byte[] message, int module, int priority);

    /// <summary>
    /// Broadcasts a lossy (UDP) message to all clients for a given module.
    /// </summary>
    /// <param name="message">Payload data to broadcast.</param>
    /// <param name="module">Target module identifier.</param>
    /// <param name="priority">Message priority level.</param>
    void BroadcastMessageLossy(byte[] message, int module, int priority);

    /// <summary>
    /// Subscribes a client to a multicast group.
    /// </summary>
    /// <param name="multicastGroup">Multicast group identifier.</param>
    /// <param name="clientIp">Endpoint of the joining client.</param>
    void JoinMulticastGroup(int multicastGroup, IPEndPoint clientIp);

    /// <summary>
    /// Unsubscribes a client from a multicast group.
    /// </summary>
    /// <param name="multicastGroup">Multicast group identifier.</param>
    /// <param name="clientIp">Endpoint of the leaving client.</param>
    void LeaveMulticastGroup(int multicastGroup, IPEndPoint clientIp);

    /// <summary>
    /// Sends a reliable message to all members of a multicast group.
    /// </summary>
    /// <param name="multicastGroup">Multicast group identifier.</param>
    /// <param name="message">Payload data to send.</param>
    /// <param name="module">Target module identifier.</param>
    /// <param name="priority">Message priority level.</param>
    void MulticastMessage(int multicastGroup, byte[] message, int module, int priority);

    /// <summary>
    /// Sends a lossy (UDP) message to all members of a multicast group.
    /// </summary>
    /// <param name="multicastGroup">Multicast group identifier.</param>
    /// <param name="message">Payload data to send.</param>
    /// <param name="module">Target module identifier.</param>
    /// <param name="priority">Message priority level.</param>
    void MulticastMessageLossy(int multicastGroup, byte[] message, int module, int priority);
}
