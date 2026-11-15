// Author = Dheeraj Ram
// Roll Number = 142301015

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Networking.Packets;
using Networking.Queues;

namespace Networking.Sockets;

/// <summary>
/// Monitors the receiving queue and dispatches reliable packets to registered module listeners.
/// </summary>
public class ReceivingQueueListener : IDisposable
{
    private ConcurrentDictionary<int, IMessageListener> _moduleToMessageListenerMap;
    private ReceivingQueue _receivingQueue;
    private Thread _listeningThread;

    private object _isRunningLock;
    private bool _isRunning;
    private bool IsRunning
    {
        get {
            lock (_isRunningLock)
            {
                return _isRunning;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReceivingQueueListener"/> class and starts listening.
    /// </summary>
    /// <param name="receivingQueue">The queue containing incoming packets.</param>
    /// <param name="moduleToMessageListenerMap">Map of module identifiers to registered listeners.</param>
    public ReceivingQueueListener(ReceivingQueue receivingQueue, ConcurrentDictionary<int, IMessageListener> moduleToMessageListenerMap)
    {
        this._moduleToMessageListenerMap = moduleToMessageListenerMap;
        this._receivingQueue = receivingQueue;
        _isRunningLock = new();
        _isRunning = true;
        _listeningThread = new Thread(ListenOnQueue) { IsBackground = true };
        _listeningThread.Start();
        Trace.TraceInformation("[Networking] ReceivingQueueListener: Started listening thread.");
    }

    /// <summary>
    /// Dequeues packets continuously and notifies registered listeners.
    /// </summary>
    public void ListenOnQueue()
    {
        while (IsRunning)
        {
            if (_receivingQueue.IsEmpty())
            {
                _receivingQueue.WaitForItem(100);
                continue;
            }
            Packet? packet = _receivingQueue.Dequeue();
            if (packet == null) { continue; }

            if (_moduleToMessageListenerMap.TryGetValue(packet.Module, out IMessageListener? messageHandler) && messageHandler != null)
            {
                try
                {
                    messageHandler.OnMessageReceived(packet.SerializedData);
                }
                catch (Exception e)
                {
                    Trace.TraceError($"[Networking] Message handler for module {packet.Module} threw: {e}");
                }
            }
            else
            {
                Trace.TraceWarning($"[Networking] Module {packet.Module} is not found. Unable to handle the data.");
            }
        }
    }

    /// <summary>
    /// Stops the queue listener thread.
    /// </summary>
    public void Dispose()
    {
        lock (_isRunningLock)
        {
            if (!_isRunning)
            {
                return;
            }

            _isRunning = false;
        }
    }
}
