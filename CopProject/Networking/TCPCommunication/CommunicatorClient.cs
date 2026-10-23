using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Networking.Packets;
using Networking.Queues;
using Networking.Sockets;

namespace Networking.TCPCommunication;

public class CommunicatorClient : ICommunicator, IDisposable
{
    private SendingQueue _sendingQueue = new SendingQueue();
    private ReceivingQueue _receivingQueue = new ReceivingQueue();
    private SocketListener _socketListener;
    private SendingQueueListenerClient _sendingQueueListenerClient;
    private ReceivingQueueListener _receivingQueueListener;
    private TcpClient _tcpClient;
    private ConcurrentDictionary<int, IMessageListener> _moduleToMessageListenerMap = new ConcurrentDictionary<int, IMessageListener>();

    public CommunicatorClient(IPEndPoint serverIp)
    {
        _socketListener = new SocketListener(_receivingQueue, _tcpClient);
        _sendingQueueListenerClient = new SendingQueueListenerClient(_sendingQueue, _tcpClient);
        _receivingQueueListener = new ReceivingQueueListener(_receivingQueue, _moduleToMessageListenerMap);
        try
        {
            _tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
            _tcpClient.Connect(serverIp.Address, serverIp.Port);
        }
        catch (Exception e)
        {
            Trace.WriteLine($"[Networking] CommunicatorClient.CommunicatorClient: Failed to connect to server at {serverIp}. Exception: {e.Message}");
        }
    }
    public void Dispose()
    {
        throw new NotImplementedException();
    }
    public void SendMessage(IPEndPoint iPEndPoint, byte[] message, int module, int priority)
    {
        try
        {
            Packet packet = new Packet(message, iPEndPoint, module, priority);
            _sendingQueue.Enqueue(packet);
        }
        catch (Exception e)
        {
            Trace.WriteLine($"[Networking] CommunicatorClient.SendMessage: Failed to enqueue message for destination {iPEndPoint}. Exception: {e.Message}");
        }
    }

    [ExcludeFromCodeCoverage]
    public void AddSubscriber(int module, IMessageListener subscriber)
    {
        throw new NotImplementedException();
    }

    [ExcludeFromCodeCoverage]
    public void RemoveSubscriber(int module)
    {
        throw new NotImplementedException();
    }

    [ExcludeFromCodeCoverage]
    public void BroadcastMessage(byte[] message, int module, int priority)
    {
        throw new NotImplementedException();
    }

    [ExcludeFromCodeCoverage]
    public void JoinMulticastGroup(int multicastGroup)
    {
        throw new NotImplementedException();
    }

    [ExcludeFromCodeCoverage]
    public void LeaveMulticastGroup(int multicastGroup)
    {
        throw new NotImplementedException();
    }

    [ExcludeFromCodeCoverage]
    public void MulticastMessage(int multicastGroup, byte[] message, int module, int priority)
    {
        throw new NotImplementedException();
    }

    [ExcludeFromCodeCoverage]
    public int ListenPort => throw new NotImplementedException();

}
