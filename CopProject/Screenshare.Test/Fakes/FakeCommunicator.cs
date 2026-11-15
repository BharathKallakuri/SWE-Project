using System.Net;
using Networking;
using Networking.Packets;

namespace Screenshare.Tests.Fakes;

/// <summary>
/// an in-memory "network" that a server and its clients share. it follows the same rules as
/// the real Networking module:
/// - a client can only send to the server
/// - only the server can send to a client, and broadcast reaches every client but not the server
/// - lossy (UDP) messages reach only listeners added with AddLossySubscriber; the reliable
///   listener never sees them
/// - a lossy multicast reaches every member that joined the group, the sender included;
///   with nobody in the group it is lost, and the sender is not told
/// - a lossy message is one UDP datagram: once Networking wraps it (JSON, base64), anything
///   over 65,507 bytes is lost, and the sender is not told
/// messages are delivered straight away on the caller's thread, so tests are repeatable.
/// </summary>
internal sealed class FakeNetwork
{
    /// <summary>the largest UDP datagram Windows will send</summary>
    public const int MaxDatagramBytes = 65507;

    private readonly PacketSerializer _wire = new();
    private readonly object _lock = new();
    private readonly Dictionary<IPEndPoint, FakeCommunicator> _members = new();
    private readonly Dictionary<int, HashSet<IPEndPoint>> _groups = new();
    private readonly List<SentMessage> _log = new();
    private readonly List<SentMessage> _dropped = new();
    private int _nextPort = 5001;

    public FakeNetwork()
    {
        Server = new FakeCommunicator(this, new IPEndPoint(IPAddress.Loopback, 5000), isServer: true);
        Register(Server);
    }

    /// <summary>gets the host's communicator</summary>
    public FakeCommunicator Server { get; }

    /// <summary>gets every message that was delivered, in order</summary>
    public IReadOnlyList<SentMessage> Log
    {
        get { lock (_lock) { return _log.ToList(); } }
    }

    /// <summary>
    /// gets every message that broke a Networking rule and was not delivered;
    /// a correct ScreenShare leaves this empty
    /// </summary>
    public IReadOnlyList<SentMessage> Dropped
    {
        get { lock (_lock) { return _dropped.ToList(); } }
    }

    /// <summary>connects a new client, with its own port</summary>
    public FakeCommunicator AddClient()
    {
        lock (_lock)
        {
            var client = new FakeCommunicator(this, new IPEndPoint(IPAddress.Loopback, _nextPort++), isServer: false);
            Register(client);
            return client;
        }
    }

    /// <summary>delivers raw bytes to one member, as if the server sent them; for malformed-message tests</summary>
    public void Inject(FakeCommunicator to, byte[] bytes, int module)
    {
        to.Deliver(module, bytes);
    }

    internal void Send(FakeCommunicator from, IPEndPoint to, byte[] bytes, int module, int priority, bool lossy = false)
    {
        FakeCommunicator? target;
        var sent = new SentMessage(from.Endpoint, to, module, priority, bytes, lossy);
        bool fits = !lossy || FitsInDatagram(bytes, to, module, priority);

        lock (_lock)
        {
            _members.TryGetValue(to, out target);

            // clients can only reach the server; the server can reach any client
            bool allowed = fits && target != null && target != from && (from.IsServer || target.IsServer);
            if (!allowed)
            {
                _dropped.Add(sent);
                return;
            }

            _log.Add(sent);
        }

        // outside the lock, since the receiver may send straight back
        target!.Deliver(module, bytes, lossy);
    }

    internal void Broadcast(FakeCommunicator from, byte[] bytes, int module, int priority, bool lossy = false)
    {
        if (!from.IsServer)
        {
            // ScreenShare clients never broadcast; record it so a test can catch it
            lock (_lock)
            {
                _dropped.Add(new SentMessage(from.Endpoint, null, module, priority, bytes, lossy));
            }
            return;
        }

        List<FakeCommunicator> clients;
        lock (_lock)
        {
            clients = _members.Values.Where(m => !m.IsServer).ToList();
        }

        foreach (FakeCommunicator client in clients)
        {
            Send(from, client.Endpoint, bytes, module, priority, lossy);
        }
    }

    internal void JoinGroup(int group, IPEndPoint member)
    {
        lock (_lock)
        {
            if (!_groups.TryGetValue(group, out HashSet<IPEndPoint>? members))
            {
                members = new HashSet<IPEndPoint>();
                _groups[group] = members;
            }

            members.Add(member);
        }
    }

    internal void LeaveGroup(int group, IPEndPoint member)
    {
        lock (_lock)
        {
            if (_groups.TryGetValue(group, out HashSet<IPEndPoint>? members))
            {
                members.Remove(member);
            }
        }
    }

    internal void MulticastLossy(FakeCommunicator from, int group, byte[] bytes, int module, int priority)
    {
        bool fits = FitsInDatagram(bytes, from.Endpoint, module, priority);
        List<FakeCommunicator> targets;
        lock (_lock)
        {
            targets = _groups.TryGetValue(group, out HashSet<IPEndPoint>? members)
                ? members.Where(_members.ContainsKey).Select(e => _members[e]).ToList()
                : new List<FakeCommunicator>();

            if (targets.Count == 0 || !fits)
            {
                // nobody joined the group, or the datagram is too big: it reaches no one
                _dropped.Add(new SentMessage(from.Endpoint, null, module, priority, bytes, Lossy: true));
                return;
            }

            foreach (FakeCommunicator target in targets)
            {
                _log.Add(new SentMessage(from.Endpoint, target.Endpoint, module, priority, bytes, Lossy: true));
            }
        }

        foreach (FakeCommunicator target in targets)
        {
            target.Deliver(module, bytes, lossy: true);
        }
    }

    // the size Networking really puts on the wire for this message
    private bool FitsInDatagram(byte[] bytes, IPEndPoint to, int module, int priority)
    {
        return _wire.PacketToBytes(new Packet(bytes, to, module, priority)).Length <= MaxDatagramBytes;
    }

    private void Register(FakeCommunicator member)
    {
        _members[member.Endpoint] = member;
    }
}

/// <summary>one message on the fake network; To is null for a rejected broadcast or an empty multicast</summary>
internal sealed record SentMessage(IPEndPoint From, IPEndPoint? To, int Module, int Priority, byte[] Bytes, bool Lossy = false);

/// <summary>
/// one member's connection to a FakeNetwork; implements the same interface ScreenShare uses
/// </summary>
internal sealed class FakeCommunicator : ICommunicator
{
    private readonly FakeNetwork _network;
    private readonly Dictionary<int, IMessageListener> _listeners = new();
    private readonly Dictionary<int, IMessageListenerLossy> _lossyListeners = new();
    private readonly object _lock = new();

    internal FakeCommunicator(FakeNetwork network, IPEndPoint endpoint, bool isServer)
    {
        _network = network;
        Endpoint = endpoint;
        IsServer = isServer;
    }

    /// <summary>gets the address other members use to reach this one</summary>
    public IPEndPoint Endpoint { get; }

    public bool IsServer { get; }

    public int ListenPort => Endpoint.Port;

    public IPAddress ListenIpAddress => Endpoint.Address;

    public void AddSubscriber(int module, IMessageListener subscriber)
    {
        lock (_lock)
        {
            _listeners[module] = subscriber;
        }
    }

    public void RemoveSubscriber(int module)
    {
        lock (_lock)
        {
            _listeners.Remove(module);
        }
    }

    public void SendMessage(IPEndPoint iPEndPoint, byte[] message, int module, int priority)
    {
        _network.Send(this, iPEndPoint, message, module, priority);
    }

    public void BroadcastMessage(byte[] message, int module, int priority)
    {
        _network.Broadcast(this, message, module, priority);
    }

    public void JoinMulticastGroup(int multicastGroup, IPEndPoint clientIp)
    {
        _network.JoinGroup(multicastGroup, clientIp);
    }

    public void LeaveMulticastGroup(int multicastGroup, IPEndPoint clientIp)
    {
        _network.LeaveGroup(multicastGroup, clientIp);
    }

    // ScreenShare sends nothing reliable to a multicast group
    public void MulticastMessage(int multicastGroup, byte[] message, int module, int priority)
    {
        throw new NotSupportedException();
    }

    public void AddLossySubscriber(int module, IMessageListenerLossy subscriber)
    {
        lock (_lock)
        {
            _lossyListeners[module] = subscriber;
        }
    }

    public void RemoveLossySubscriber(int module)
    {
        lock (_lock)
        {
            _lossyListeners.Remove(module);
        }
    }

    public void SendMessageLossy(IPEndPoint iPEndPoint, byte[] message, int module, int priority)
    {
        _network.Send(this, iPEndPoint, message, module, priority, lossy: true);
    }

    public void BroadcastMessageLossy(byte[] message, int module, int priority)
    {
        _network.Broadcast(this, message, module, priority, lossy: true);
    }

    public void MulticastMessageLossy(int multicastGroup, byte[] message, int module, int priority)
    {
        _network.MulticastLossy(this, multicastGroup, message, module, priority);
    }

    /// <summary>true while a lossy listener is registered for the module</summary>
    public bool HasLossyListener(int module)
    {
        lock (_lock)
        {
            return _lossyListeners.ContainsKey(module);
        }
    }

    internal void Deliver(int module, byte[] bytes, bool lossy = false)
    {
        IMessageListener? listener = null;
        IMessageListenerLossy? lossyListener = null;
        lock (_lock)
        {
            // like Networking, lossy messages only reach lossy listeners
            if (lossy)
            {
                _lossyListeners.TryGetValue(module, out lossyListener);
            }
            else
            {
                _listeners.TryGetValue(module, out listener);
            }
        }

        // a copy, like a real network, so the receiver can't change the sender's array
        listener?.OnMessageReceived(bytes.ToArray());
        lossyListener?.OnMessageReceivedLossy(bytes.ToArray());
    }
}
