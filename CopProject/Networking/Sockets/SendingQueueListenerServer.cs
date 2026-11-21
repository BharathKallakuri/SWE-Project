// Author = Bharath
// Roll Number = 112301013

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Networking.Packets;
using Networking.Queues;

namespace Networking.Sockets;

/// <summary>
/// Monitors the server sending queue and transmits packets to connected clients over TCP.
/// </summary>
public class SendingQueueListenerServer : IDisposable
{
    private SendingQueue _sendingQueue;

    private bool _isRunning;
    private object _isRunningLock;

    private bool IsRunning
    {
        get {
            lock (_isRunningLock)
            {
                return _isRunning;
            }
        }
    }
    private Thread _listeningThread;

    // Map for client to TCPClient
    private ConcurrentDictionary<IPEndPoint, TcpClient> _clientToTcpListenerMap;

    private PacketSerializer _packetSerializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="SendingQueueListenerServer"/> class and begins processing the queue.
    /// </summary>
    /// <param name="sendingQueue">The sending queue containing outgoing packets.</param>
    /// <param name="clientToTcpListenerMap">Map of client endpoints to active TCP connections.</param>
    public SendingQueueListenerServer(SendingQueue sendingQueue, ConcurrentDictionary<IPEndPoint, TcpClient> clientToTcpListenerMap)
    {
        _isRunningLock = new object();
        _sendingQueue = sendingQueue;
        _clientToTcpListenerMap = clientToTcpListenerMap;
        _isRunning = true;
        _packetSerializer = new();
        _listeningThread = new Thread(ListenOnQueue) { IsBackground = true };
        _listeningThread.Start();
        Trace.TraceInformation("[Networking] SendingQueueListenerServer: Started server sending queue listener thread.");
    }

    /// <summary>
    /// Dequeues packets and dispatches them to their destination clients.
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
            byte[] data = _packetSerializer.PacketToBytes(packet);

            SendDataToClient(data, packet.Destination);

        }
    }

    /// <summary>
    /// Transmits data to a specific client endpoint with automatic retries on transient failures.
    /// </summary>
    /// <param name="data">The framed packet bytes to send.</param>
    /// <param name="destination">The destination client endpoint.</param>
    public void SendDataToClient(byte[] data, IPEndPoint destination)
    {
        if (destination != null && destination.Address.IsIPv4MappedToIPv6)
        {
            destination = new IPEndPoint(destination.Address.MapToIPv4(), destination.Port);
        }

        if (destination == null)
        {
            return;
        }

        // If destination socket is missing or disconnected, check if the client reconnected with a new port on the same IP
        if (!_clientToTcpListenerMap.TryGetValue(destination, out TcpClient? socket) || socket == null || !socket.Connected)
        {
            IPEndPoint? activeEp = _clientToTcpListenerMap.Keys.FirstOrDefault(ep => ep.Address.Equals(destination.Address) && !ep.Equals(destination));
            if (activeEp != null && _clientToTcpListenerMap.TryGetValue(activeEp, out TcpClient? activeSocket) && activeSocket.Connected)
            {
                Trace.TraceInformation($"[Networking] SendingQueueListenerServer: Migrating destination {destination} -> {activeEp} (reconnected client on same IP).");
                destination = activeEp;
                socket = activeSocket;
            }
        }

        if (socket == null)
        {
            Trace.TraceWarning($"[Networking] SendingQueueListenerServer.SendDataToClient: Destination client {destination} not found in client map.");
            return;
        }

        int maxAttempts = 3;
        int delay = 50;  // fast retry to avoid blocking the queue for other clients

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                NetworkStream stream = socket.GetStream();
                stream.Write(data, 0, data.Length);
                Trace.TraceInformation($"[Networking] SendingQueueListenerServer: Sent {data.Length} bytes to {destination} (attempt {attempt}).");
                return;
            }
            catch (Exception e)
            {
                Trace.TraceWarning($"[Networking] SendingQueueListenerServer.SendDataToClient: Unable to send data to client {destination} (Attempt {attempt}/{maxAttempts}). Retrying in {delay}ms. Exception: {e.Message}");
                if (attempt < maxAttempts)
                {
                    Thread.Sleep(delay);
                }
            }
        }
        _clientToTcpListenerMap.TryRemove(destination, out _);
        Trace.TraceError($"[Networking] SendingQueueListenerServer.SendDataToClient: Max retry attempts reached. Client {destination} disconnected and removed.");
    }

    /// <summary>
    /// Stops the server sender thread.
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
