// Author = Dheeraj Ram
// Roll Number = 142301015

using Networking.Packets;

namespace Networking.Queues;

/// <summary>
/// Defines packet queue operations with synchronization support.
/// </summary>
public interface IQueue
{
    /// <summary>
    /// Adds a packet to the queue.
    /// </summary>
    /// <param name="packet">The packet to enqueue.</param>
    public void Enqueue(Packet packet);

    /// <summary>
    /// Removes and returns the next packet from the queue.
    /// </summary>
    /// <returns>The dequeued packet, or null if empty.</returns>
    public Packet? Dequeue();

    /// <summary>
    /// Returns the next packet without removing it from the queue.
    /// </summary>
    /// <returns>The next packet, or null if empty.</returns>
    public Packet? Peek();

    /// <summary>
    /// Removes all packets from the queue.
    /// </summary>
    public void Clear();

    /// <summary>
    /// Gets the number of packets in the queue.
    /// </summary>
    /// <returns>The current packet count.</returns>
    public int Size();

    /// <summary>
    /// Checks whether the queue contains any packets.
    /// </summary>
    /// <returns>True if the queue is empty; otherwise, false.</returns>
    public bool IsEmpty();

    /// <summary>
    /// Waits until a packet is enqueued or the timeout expires.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    /// <returns>True if signaled within the timeout; otherwise, false.</returns>
    public bool WaitForItem(int timeoutMs);
}
