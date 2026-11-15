// Author = Bharath
// Roll Number = 112301013

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Networking.Packets;
using Networking.Queues;

namespace Networking.Sockets;

/// <summary>
/// Monitors the client sending queue and transmits packets to the server over TCP.
/// </summary>
public class SendingQueueListenerClient : IDisposable
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

    private PacketSerializer _packetSerializer;
    private Thread _listeningThread;

    TcpClient _clientSocket;

    /// <summary>
    /// Initializes a new instance of the <see cref="SendingQueueListenerClient"/> class and starts the sending thread.
    /// </summary>
    /// <param name="sendingQueue">The sending queue containing outgoing packets.</param>
    /// <param name="clientSocket">The connected TCP client.</param>
    public SendingQueueListenerClient(SendingQueue sendingQueue, TcpClient clientSocket)
    {
        _isRunningLock = new object();
        this._sendingQueue = sendingQueue;
        this._clientSocket = clientSocket;
        _isRunning = true;
        _packetSerializer = new();
        _listeningThread = new(ListenOnQueue) { IsBackground = true };
        _listeningThread.Start();
        Trace.TraceInformation("[Networking] SendingQueueListenerClient: Started listening thread.");
    }

    /// <summary>
    /// Dequeues packets and writes framed byte data to the network stream.
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

            try
            {
                NetworkStream stream = _clientSocket.GetStream();
                stream.Write(data, 0, data.Length);
                Trace.TraceInformation($"[Networking] SendingQueueListenerClient: Sent {data.Length} bytes (Priority {packet.Priority}, Module {packet.Module}) to server.");
            }
            catch (Exception e)
            {
                Trace.TraceError($"[Networking] SendingQueueListenerClient.ListenOnQueue: Error sending data to server. Exception: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Stops the sending thread.
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
