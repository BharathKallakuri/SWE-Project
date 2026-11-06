using System.Net;
using Networking;

namespace ScreenShare.Demo;

/// <summary>
/// passes everything through to the real communicator, counting the bytes ScreenShare
/// sends and receives so the demo can show its data rate
/// </summary>
internal sealed class CountingCommunicator : ICommunicator
{
    private readonly ICommunicator _inner;
    private readonly int _module;
    private long _sent;
    private long _received;

    public CountingCommunicator(ICommunicator inner, int module)
    {
        _inner = inner;
        _module = module;
    }

    public int ListenPort => _inner.ListenPort;

    public IPAddress ListenIpAddress => _inner.ListenIpAddress;

    /// <summary>returns the bytes sent and received since the last call</summary>
    public (long Sent, long Received) TakeCounts() =>
        (Interlocked.Exchange(ref _sent, 0), Interlocked.Exchange(ref _received, 0));

    public void AddSubscriber(int module, IMessageListener subscriber)
    {
        _inner.AddSubscriber(module, module == _module ? new CountingListener(subscriber, this) : subscriber);
    }

    public void RemoveSubscriber(int module) => _inner.RemoveSubscriber(module);

    public void SendMessage(IPEndPoint iPEndPoint, byte[] message, int module, int priority)
    {
        Count(module, ref _sent, message.Length);
        _inner.SendMessage(iPEndPoint, message, module, priority);
    }

    public void BroadcastMessage(byte[] message, int module, int priority)
    {
        // counted once, although the host sends a copy to every client
        Count(module, ref _sent, message.Length);
        _inner.BroadcastMessage(message, module, priority);
    }

    public void JoinMulticastGroup(int multicastGroup, IPEndPoint clientIp) => _inner.JoinMulticastGroup(multicastGroup, clientIp);

    public void LeaveMulticastGroup(int multicastGroup, IPEndPoint clientIp) => _inner.LeaveMulticastGroup(multicastGroup, clientIp);

    public void MulticastMessage(int multicastGroup, byte[] message, int module, int priority) =>
        _inner.MulticastMessage(multicastGroup, message, module, priority);

    private void Count(int module, ref long counter, int bytes)
    {
        if (module == _module)
        {
            Interlocked.Add(ref counter, bytes);
        }
    }

    private sealed class CountingListener : IMessageListener
    {
        private readonly IMessageListener _inner;
        private readonly CountingCommunicator _owner;

        public CountingListener(IMessageListener inner, CountingCommunicator owner)
        {
            _inner = inner;
            _owner = owner;
        }

        public void OnMessageReceived(byte[] message)
        {
            Interlocked.Add(ref _owner._received, message.Length);
            _inner.OnMessageReceived(message);
        }
    }
}
