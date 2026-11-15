// Author = Srikar
// Roll Number = 142301013

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Networking.Packets;
using Networking.Queues;

namespace Networking.Sockets;

/// <summary>
/// Monitors the lossy sending queue and transmits outgoing packets via UDP.
/// </summary>
public class SendingQueueListenerLossy : IDisposable
{
    private readonly SendingQueue _sendingQueue;
    private readonly UdpClient _udpClient;
    private readonly BinarySerialization _binarySerializer = new();
    private readonly Thread _listeningThread;
    private readonly object _isRunningLock = new();
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
    /// Initializes a new instance of the <see cref="SendingQueueListenerLossy"/> class and begins transmission.
    /// </summary>
    /// <param name="sendingQueue">The sending queue containing outgoing lossy packets.</param>
    /// <param name="udpClient">The configured UDP client used for sending.</param>
    public SendingQueueListenerLossy(SendingQueue sendingQueue, UdpClient udpClient)
    {
        _sendingQueue = sendingQueue;
        _udpClient = udpClient;
        try
        {
            _udpClient.EnableBroadcast = true;
            _udpClient.MulticastLoopback = true;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"[Networking] SendingQueueListenerLossy: Could not configure broadcast/multicast options: {ex.Message}");
        }

        _isRunning = true;
        _listeningThread = new Thread(ListenOnQueue) { IsBackground = true };
        _listeningThread.Start();
    }

    /// <summary>
    /// Dequeues packets from the sending queue and transmits them as UDP datagrams.
    /// </summary>
    public void ListenOnQueue()
    {
        while (IsRunning)
        {
            if (_sendingQueue.IsEmpty())
            {
                _sendingQueue.WaitForItem(100);
                continue;
            }

            Packet packet = _sendingQueue.Dequeue()!;
            byte[] data = _binarySerializer.Serialize(packet);

            try
            {
                IPEndPoint dest = packet.Destination;
                if (dest.Address.IsIPv4MappedToIPv6)
                {
                    dest = new IPEndPoint(dest.Address.MapToIPv4(), dest.Port);
                }

                _udpClient.Send(data, data.Length, dest);
            }
            catch (Exception e)
            {
                Trace.TraceError($"[Networking] SendingQueueListenerLossy: Error sending UDP packet to {packet.Destination}: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Stops the lossy sender thread.
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
