// Author = Dheeraj Ram
// Roll Number = 142301015

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using Networking.Packets;
using Networking.Queues;

namespace Networking.Sockets;

/// <summary>
/// Reads streaming data from a TCP connection, reconstructs framed packets, and places them in the receiving queue.
/// </summary>
public class SocketListener : IDisposable
{
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
    private ReceivingQueue _receivingQueue;
    private TcpClient _tcpClient;

    private const int BufferSize = 32 * 1024 * 1024;
    private static readonly byte[] s_bytes = new byte[BufferSize];

    //private byte[] _buffer = new byte[_bufferSize];
    private readonly byte[] _receivedBuffer = s_bytes;
    private readonly PacketSerializer _packetSerializer = new();

    /// <summary>
    /// Callback invoked when the socket stream disconnects or encounters a fatal error while running.
    /// </summary>
    public Action<TcpClient>? OnDisconnected { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SocketListener"/> class and starts reading from the client stream.
    /// </summary>
    /// <param name="receivingQueue">The target receiving queue for incoming packets.</param>
    /// <param name="tcpClient">The connected TCP client to listen on.</param>
    public SocketListener(ReceivingQueue receivingQueue, TcpClient tcpClient)
    {
        _isRunningLock = new object();
        _receivingQueue = receivingQueue;
        _tcpClient = tcpClient;
        _receivedBuffer = new byte[BufferSize];
        _isRunning = true;
        NetworkStream stream = tcpClient.GetStream();
        _listeningThread = new Thread(() => ListenStream(stream)) { IsBackground = true };
        _listeningThread.Start();
        Trace.TraceInformation("[Networking] SocketListener: Started socket listener thread.");
    }

    /// <summary>
    /// Continuously reads bytes from the network stream into an accumulation buffer.
    /// </summary>
    /// <param name="stream">The network stream to read from.</param>
    public void ListenStream(NetworkStream stream)
    {
        using MemoryStream incomingData = new MemoryStream();
        try
        {
            while (_isRunning && _tcpClient.Connected)
            {
                int bytesRead = stream.Read(_receivedBuffer, 0, BufferSize);
                if (bytesRead == 0)
                {
                    Trace.TraceInformation("[Networking] SocketListener: Host disconnected (0 bytes read).");
                    break;
                }
                incomingData.Seek(0, SeekOrigin.End);
                incomingData.Write(_receivedBuffer, 0, bytesRead);

                ProcessData(incomingData);


            }
        }
        catch (Exception e)
        {
            Trace.TraceError($"[Networking] Error in {GetType().Name}: {e.Message}");
        }
        finally
        {
            if (IsRunning)
            {
                OnDisconnected?.Invoke(_tcpClient);
            }
        }

    }

    /// <summary>
    /// Extracts framed packets from the buffer and enqueues them.
    /// </summary>
    /// <param name="input">The memory buffer containing incoming byte data.</param>
    public void ProcessData(MemoryStream input)
    {
        byte[] buffer = input.GetBuffer();
        int length = (int)input.Length;
        int offset = 0;
        while (length - offset >= 4)
        {
            int payloadLength = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(offset, 4));
            if (length - offset < 4 + payloadLength)
            {
                break;
            }

            byte[] packetData = new byte[4 + payloadLength];
            Buffer.BlockCopy(buffer, offset, packetData, 0, 4 + payloadLength);

            Packet? packet = _packetSerializer.BytesToPacket(packetData);
            if (packet != null)
            {
                _receivingQueue.Enqueue(packet);
            }
            offset += 4 + payloadLength;
        }

        if (offset > 0)
        {
            int remaining = length - offset;
            byte[] remainingBytes = new byte[remaining];
            Buffer.BlockCopy(buffer, offset, remainingBytes, 0, remaining);
            input.SetLength(0);
            input.Write(remainingBytes, 0, remaining);
        }
    }

    /// <summary>
    /// Closes the TCP connection and stops listening.
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

        _tcpClient?.Close();
    }


}
