// Author = Dheeraj Ram
// Roll Number = 142301015

using System.Collections.Concurrent;
using System.Diagnostics;
using Networking.Packets;

namespace Networking.Queues;

/// <summary>
/// Thread-safe FIFO packet queue with signaling support.
/// </summary>
public class Queue : IQueue, IDisposable
{
    private ConcurrentQueue<Packet> _queue;
    private readonly AutoResetEvent _itemAdded = new(false);
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="Queue"/> class.
    /// </summary>
    public Queue()
    {
        this._queue = new ConcurrentQueue<Packet>();
    }

    /// <summary>
    /// Enqueues a packet and signals waiting threads.
    /// </summary>
    /// <param name="packet">The packet to enqueue.</param>
    public virtual void Enqueue(Packet packet)
    {
        Trace.TraceInformation($"[Networking] Enqueuing to queue {GetType().Name}");
        _queue.Enqueue(packet);
        SignalItemAdded();
    }

    /// <summary>
    /// Dequeues the next packet in FIFO order.
    /// </summary>
    /// <returns>The dequeued packet, or null if empty.</returns>
    public virtual Packet? Dequeue()
    {
        Trace.TraceInformation($"[Networking] Dequeuing from queue {GetType().Name}");
        if (!_queue.TryDequeue(out Packet? top))
        {
            Trace.TraceWarning($"[Networking] Unable to dequeue from {GetType().Name}: queue is empty");
        }
        return top;
    }

    /// <summary>
    /// Returns the next packet without removing it.
    /// </summary>
    /// <returns>The next packet, or null if empty.</returns>
    public virtual Packet? Peek()
    {
        if (!_queue.TryPeek(out Packet? peek))
        {
            Trace.TraceWarning($"[Networking] Unable to peek from {GetType().Name}: queue is empty");
        }
        return peek;
    }

    /// <summary>
    /// Removes all packets and resets the signal.
    /// </summary>
    public virtual void Clear()
    {
        _queue.Clear();
        ResetItemAdded();
    }

    /// <summary>
    /// Waits until an item is available or the timeout expires.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    /// <returns>True if signaled; otherwise, false.</returns>
    public virtual bool WaitForItem(int timeoutMs)
    {
        try
        {
            return _itemAdded.WaitOne(timeoutMs);
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    /// <summary>
    /// Signals that a new item was added.
    /// </summary>
    protected void SignalItemAdded()
    {
        try
        {
            _itemAdded.Set();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    /// <summary>
    /// Resets the item-added signal.
    /// </summary>
    protected void ResetItemAdded()
    {
        try
        {
            _itemAdded.Reset();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    /// <summary>
    /// Returns the number of packets in the queue.
    /// </summary>
    /// <returns>The packet count.</returns>
    public virtual int Size()
    {
        return _queue.Count();
    }

    /// <summary>
    /// Checks whether the queue is empty.
    /// </summary>
    /// <returns>True if empty; otherwise, false.</returns>
    public virtual bool IsEmpty()
    {
        return _queue.IsEmpty;
    }

    /// <summary>
    /// Disposes synchronization handles.
    /// </summary>
    public virtual void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _itemAdded.Dispose();
        }
    }
}
