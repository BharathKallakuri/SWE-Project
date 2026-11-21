// Author = Srikar
// Roll Number = 142301013

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Networking.Packets;
using Networking.Queues;

namespace Networking.Sockets;

/// <summary>
/// Listens for incoming UDP datagrams, deserializes them into packets, and adds them to the receiving queue.
/// </summary>
public class UdpSocketListener : IDisposable
{
    private readonly ReceivingQueue _receivingQueue;
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
    /// Initializes a new instance of the <see cref="UdpSocketListener"/> class and starts the receive loop.
    /// </summary>
    /// <param name="receivingQueue">The queue to receive incoming packets.</param>
    /// <param name="udpClient">The UDP client bound for receiving.</param>
    public UdpSocketListener(ReceivingQueue receivingQueue, UdpClient udpClient)
    {
        _receivingQueue = receivingQueue;
        _udpClient = udpClient;
        _isRunning = true;
        _listeningThread = new Thread(ListenUdp) { IsBackground = true };
        _listeningThread.Start();
    }

    /// <summary>
    /// Continuously receives UDP datagrams and enqueues deserialized packets.
    /// </summary>
    public void ListenUdp()
    {
        while (IsRunning)
        {
            try
            {
                IPEndPoint? remoteEp = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = _udpClient.Receive(ref remoteEp);
                if (data == null || data.Length == 0)
                {
                    continue;
                }

                Packet? packet = _binarySerializer.Deserialize(data);
                if (packet != null)
                {
                    _receivingQueue.Enqueue(packet);
                }
            }
            catch (SocketException e) when (!IsRunning || e.SocketErrorCode == SocketError.Interrupted || e.SocketErrorCode == SocketError.OperationAborted)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception e)
            {
                if (!IsRunning)
                {
                    break;
                }
                Trace.TraceError($"[Networking] UdpSocketListener exception: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Stops listening and closes the UDP client.
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

        try
        {
            _udpClient.Close();
        }
        catch (Exception e)
        {
            Trace.TraceError($"[Networking] UdpSocketListener.Dispose: Exception closing UdpClient: {e.Message}");
        }
    }
}
