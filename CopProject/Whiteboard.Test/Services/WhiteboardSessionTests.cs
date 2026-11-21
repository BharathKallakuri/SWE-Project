/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

using System.Net;
using Networking;
using Utils;
using Whiteboard.Services;
using Whiteboard.Test.Mocks.Networking;

namespace Whiteboard.Test.Services;

public class WhiteboardSessionTests
{
    private readonly ICommunicator _clientComm = new MockCommunicatorFactory().CreateClientCommunicator();
    private readonly ICommunicator _serverComm = new MockCommunicatorFactory().GetServerCommunicator();

    [Fact]
    public void StartHost_NullOrEmptyArguments_Throws()
    {
        Assert.Throws<ArgumentException>(() => WhiteboardSession.StartHost("", _serverComm, 1234));
        Assert.Throws<ArgumentNullException>(() => WhiteboardSession.StartHost("user1", null!, 1234));
    }

    [Fact]
    public void StartClient_NullOrEmptyArguments_Throws()
    {
        Assert.Throws<ArgumentException>(() => WhiteboardSession.StartClient("", _clientComm, "localhost", 1234));
        Assert.Throws<ArgumentNullException>(() => WhiteboardSession.StartClient("user1", null!, "localhost", 1234));
        Assert.Throws<ArgumentException>(() => WhiteboardSession.StartClient("user1", _clientComm, "", 1234));
    }

    [Fact]
    public void StartClient_ValidArguments_CreatesSession()
    {
        using WhiteboardSession session = WhiteboardSession.StartClient("user1", _clientComm, "127.0.0.1", 1234);

        Assert.NotNull(session);
        Assert.NotNull(session.Client);
        Assert.Null(session.Server);
    }

    [Fact]
    public void Start_DispatchesCorrectlyBasedOnIsHost()
    {
        using WhiteboardSession clientSession = WhiteboardSession.Start("user1", _clientComm, "127.0.0.1", 1234, isHost: false);
        Assert.NotNull(clientSession.Client);
        Assert.Null(clientSession.Server);
    }

    [Fact]
    public void UserJoinedAndLeft_TriggersEvents()
    {
        using WhiteboardSession session = WhiteboardSession.StartClient("user1", _clientComm, "127.0.0.1", 1234);

        ControllerUser user = new() { Username = "remoteUser" };
        bool joinedRaised = false;
        string? leftUsername = null;

        session.UserJoined += u => joinedRaised = (u.Username == "remoteUser");
        session.UserLeft += username => leftUsername = username;

        session.OnUserJoined(user);
        session.OnUserLeft(user);

        Assert.True(joinedRaised);
        Assert.Equal("remoteUser", leftUsername);
    }

    [Fact]
    public void UserJoinedAndLeft_NullUser_DoesNotThrow()
    {
        using WhiteboardSession session = WhiteboardSession.StartClient("user1", _clientComm, "127.0.0.1", 1234);

        session.OnUserJoined(null!);
        session.OnUserLeft(null!);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        WhiteboardSession session = WhiteboardSession.StartClient("user1", _clientComm, "127.0.0.1", 1234);

        session.Dispose();
        session.Dispose(); // second call should be no-op
    }

    [Fact]
    public void StartClient_ResolvesLocalhostAndInvalidHostToLoopback()
    {
        using WhiteboardSession localhostSession = WhiteboardSession.StartClient("user1", _clientComm, "localhost", 1234);
        using WhiteboardSession fallbackSession = WhiteboardSession.StartClient("user2", _clientComm, "not-an-ip-address", 1234);

        Assert.NotNull(localhostSession.Client);
        Assert.NotNull(fallbackSession.Client);
    }

    [Fact]
    public void UserJoinedAndLeft_WithoutSubscribers_DoesNotThrow()
    {
        using WhiteboardSession session = WhiteboardSession.StartClient("user1", _clientComm, "127.0.0.1", 1234);
        ControllerUser user = new() { Username = "solo" };

        session.OnUserJoined(user);
        session.OnUserLeft(user);
    }

    [Fact]
    public void Start_WhenIsHostTrue_CreatesServerAndClient()
    {
        using WhiteboardSession hostSession = WhiteboardSession.Start("host1", _serverComm, "127.0.0.1", 0, isHost: true);

        Assert.NotNull(hostSession.Client);
        Assert.NotNull(hostSession.Server);
    }

    [Fact]
    public void StartHost_UsesCommunicatorEndpointOrFallbacks()
    {
        EndpointCommunicator nullFallbackComm = new(null!, 0);
        using WhiteboardSession fallbackSession = WhiteboardSession.StartHost("host-fallback", nullFallbackComm, 12345);
        Assert.NotNull(fallbackSession.Server);

        EndpointCommunicator explicitComm = new(IPAddress.Loopback, 12346);
        using WhiteboardSession explicitSession = WhiteboardSession.StartHost("host-explicit", explicitComm, 9999);
        Assert.NotNull(explicitSession.Server);
    }

    private sealed class EndpointCommunicator(IPAddress listenIp, int listenPort) : ICommunicator
    {
        public IPAddress ListenIpAddress { get; } = listenIp;
        public int ListenPort { get; } = listenPort;

        public void AddSubscriber(int moduleId, IMessageListener listener) { }
        public void RemoveSubscriber(int moduleId) { }
        public void AddLossySubscriber(int moduleId, IMessageListenerLossy listener) { }
        public void RemoveLossySubscriber(int moduleId) { }
        public void SendMessage(IPEndPoint destination, byte[] data, int moduleId, int priority = 0) { }
        public void SendMessageLossy(IPEndPoint destination, byte[] data, int moduleId, int priority = 0) { }
        public void BroadcastMessage(byte[] data, int moduleId, int priority = 0) { }
        public void BroadcastMessageLossy(byte[] data, int moduleId, int priority = 0) { }
        public void JoinMulticastGroup(int groupId, IPEndPoint endPoint) { }
        public void LeaveMulticastGroup(int groupId, IPEndPoint endPoint) { }
        public void MulticastMessage(int groupId, byte[] data, int moduleId, int priority = 0) { }
        public void MulticastMessageLossy(int groupId, byte[] data, int moduleId, int priority = 0) { }
    }
}
