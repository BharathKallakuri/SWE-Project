// Author = Bharath
// Roll Number = 112301013

using System.Diagnostics;
using System.Net.Sockets;
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
    private readonly Func<bool>? _ensureConnected;
    private readonly Func<NetworkStream?>? _getStream;

    /// <summary>
    /// Initializes a new instance of the <see cref="SendingQueueListenerClient"/> class and starts the sending thread.
    /// </summary>
    /// <param name="sendingQueue">The sending queue containing outgoing packets.</param>
    /// <param name="clientSocket">The connected TCP client.</param>
    /// <param name="ensureConnected">Optional callback to verify or restore server connectivity.</param>
    /// <param name="getStream">Optional callback to retrieve the active network stream.</param>
    public SendingQueueListenerClient(
        SendingQueue sendingQueue,
        TcpClient clientSocket,
        Func<bool>? ensureConnected = null,
        Func<NetworkStream?>? getStream = null)
    {
        _isRunningLock = new object();
        this._sendingQueue = sendingQueue;
        this._clientSocket = clientSocket;
        this._ensureConnected = ensureConnected;
        this._getStream = getStream;
        _isRunning = true;
        _packetSerializer = new();
        _listeningThread = new(ListenOnQueue) { IsBackground = true };
        _listeningThread.Start();
        Trace.TraceInformation("[Networking] SendingQueueListenerClient: Started listening thread.");
    }

    /// <summary>
    /// Dequeues packets and writes framed byte data to the network stream with automatic reconnection retry.
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

            int maxAttempts = 30; // 30 attempts x 2000ms = 60 seconds of network resiliency
            int delay = 2000;

            for (int attempt = 1; attempt <= maxAttempts && IsRunning; attempt++)
            {
                try
                {
                    if (_ensureConnected != null && !_ensureConnected())
                    {
                        Thread.Sleep(delay);
                        continue;
                    }

                    NetworkStream? stream = _getStream != null ? _getStream() : _clientSocket.GetStream();
                    if (stream == null)
                    {
                        Thread.Sleep(delay);
                        continue;
                    }

                    stream.Write(data, 0, data.Length);
                    Trace.TraceInformation($"[Networking] SendingQueueListenerClient: Sent {data.Length} bytes (Priority {packet.Priority}, Module {packet.Module}) to server.");
                    break;
                }
                catch (Exception e)
                {
                    Trace.TraceWarning($"[Networking] SendingQueueListenerClient.ListenOnQueue: Send attempt {attempt}/{maxAttempts} failed: {e.Message}");
                    if (attempt < maxAttempts)
                    {
                        Thread.Sleep(delay);
                    }
                    else
                    {
                        Trace.TraceError($"[Networking] SendingQueueListenerClient.ListenOnQueue: 60-second retry window expired. Packet could not be delivered to server.");
                    }
                }
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
