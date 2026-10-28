using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Networking;

namespace FileSync.ConsoleApp;

// simple tcp communicator implementation of ICommunicator for testing network sync
public class SimulatedCommunicator : ICommunicator, IDisposable
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<int, IMessageListener> _subscribers = new();
    private readonly ConcurrentBag<IPEndPoint> _knownPeers = new();
    private bool _disposed;

    public int ListenPort { get; }

    public SimulatedCommunicator(int port)
    {
        ListenPort = port;
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();

        // start background loop to listen for incoming peer messages
        _ = Task.Run(ListenLoopAsync);
    }

    // register a peer endpoint so broadcast knows where to send
    public void RegisterPeer(IPEndPoint peer)
    {
        _knownPeers.Add(peer);
    }

    public void AddSubscriber(int module, IMessageListener subscriber)
    {
        _subscribers[module] = subscriber;
    }

    public void RemoveSubscriber(int module)
    {
        _subscribers.TryRemove(module, out _);
    }

    public void SendMessage(IPEndPoint ipEndPoint, byte[] message, int module, int priority)
    {
        if (_disposed)
        {
            return;
        }
        try
        {
            // connect to the target peer and send the packet
            using var client = new TcpClient();
            client.Connect(ipEndPoint.Address, ipEndPoint.Port);
            using NetworkStream stream = client.GetStream();
            using var writer = new BinaryWriter(stream);

            // packet structure: module id, length, message bytes
            writer.Write(module);
            writer.Write(message.Length);
            writer.Write(message);
            writer.Flush();
        }
        catch (Exception ex)
        {
            // print error if peer is unreachable
            Console.WriteLine($"[network] send error to {ipEndPoint}: {ex.Message}");
        }
    }

    public void BroadcastMessage(byte[] message, int module, int priority)
    {
        // broadcast to all registered peers
        foreach (IPEndPoint peer in _knownPeers)
        {
            SendMessage(peer, message, module, priority);
        }
    }

    public void JoinMulticastGroup(int multicastGroup) { }
    public void LeaveMulticastGroup(int multicastGroup) { }
    public void MulticastMessage(int multicastGroup, byte[] message, int module, int priority)
    {
        BroadcastMessage(message, module, priority);
    }

    private async Task ListenLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                TcpClient client = await _listener.AcceptTcpClientAsync(_cts.Token);
                _ = Task.Run(() => HandleClient(client));
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (SocketException) { break; }
        }
    }

    private void HandleClient(TcpClient client)
    {
        using (client)
        using (NetworkStream stream = client.GetStream())
        using (var reader = new BinaryReader(stream))
        {
            try
            {
                while (client.Connected && stream.DataAvailable || stream.CanRead)
                {
                    int module = reader.ReadInt32();
                    int length = reader.ReadInt32();
                    byte[] payload = reader.ReadBytes(length);

                    // forward received bytes to the registered subscriber
                    if (_subscribers.TryGetValue(module, out IMessageListener? listener))
                    {
                        listener.OnMessageReceived(payload);
                    }
                    break;
                }
            }
            catch (EndOfStreamException) { }
            catch (IOException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"[network] handle client error: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _cts.Cancel();
        _listener.Stop();
        _cts.Dispose();
    }
}
