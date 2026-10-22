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
    public ReceivingQueueListener(ReceivingQueue receivingQueue, ConcurrentDictionary<int, IMessageListener> moduleToMessageListenerMap)
    {
        this._moduleToMessageListenerMap = moduleToMessageListenerMap;
        this._receivingQueue = receivingQueue;
        _isRunningLock = new();
        _isRunning = true;
        _listeningThread = new Thread(ListenOnQueue) { IsBackground = true };
        _listeningThread.Start();

    }

    public void ListenOnQueue()
    {
        while (IsRunning)
        {
            if (_receivingQueue.IsEmpty())
            {
                Thread.Sleep(100);
                continue;
            }
            Packet packet = _receivingQueue.Dequeue();
            if (packet == null) { continue; }

            IMessageListener messageHandler = null;

            messageHandler = _moduleToMessageListenerMap[packet.Module];

            if (messageHandler == null)
            {
                Trace.WriteLine($"[Networking] Module {packet.Module} is not found. Unable to handle the data.");
                continue;
            }
            try
            {
                messageHandler.OnMessageReceived(packet.SerializedData);
            }
            catch (Exception e)
            {
                Trace.WriteLine($"[Networking] Message handler for module {packet.Module} threw: {e}");
            }
        }
    }

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
