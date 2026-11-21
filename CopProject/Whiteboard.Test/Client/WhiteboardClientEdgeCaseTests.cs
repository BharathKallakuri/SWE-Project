/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

using System.Net;
using Networking;
using Whiteboard.Client;
using Whiteboard.Network.Messages;
using Whiteboard.Network.Serializations;

namespace Whiteboard.Test.Client;

public class WhiteboardClientEdgeCaseTests
{
    [Fact]
    public void RequestSync_RethrowsWhenCommunicatorSendFails()
    {
        ThrowingCommunicator communicator = new();
        WhiteboardClient client = new(communicator, new IPEndPoint(IPAddress.Loopback, 9000), "alice");
        communicator.ThrowOnSend = true;

        Assert.Throws<InvalidOperationException>(client.RequestSync);
    }

    [Fact]
    public void OnMessageReceived_ThrowsOnCorruptPayload()
    {
        ThrowingCommunicator communicator = new();
        WhiteboardClient client = new(communicator, new IPEndPoint(IPAddress.Loopback, 9000), "alice");

        Assert.ThrowsAny<Exception>(() => client.OnMessageReceived([0xFF, 0xFE, 0x00]));
    }

    [Fact]
    public void OnMessageReceived_IgnoresUnsupportedServerMessage()
    {
        ThrowingCommunicator communicator = new();
        WhiteboardClient client = new(communicator, new IPEndPoint(IPAddress.Loopback, 9000), "alice");

        byte[] emptyPayload = new ServerMessageByteSerializer().Serialize(null!);
        client.OnMessageReceived(emptyPayload);

        Assert.Empty(client.Canvas.Items);
    }

    private sealed class ThrowingCommunicator : ICommunicator
    {
        public bool ThrowOnSend { get; set; }
        public IPAddress ListenIpAddress => IPAddress.Loopback;
        public int ListenPort => 0;

        public void AddSubscriber(int moduleId, IMessageListener listener) { }
        public void RemoveSubscriber(int moduleId) { }
        public void AddLossySubscriber(int moduleId, IMessageListenerLossy listener) { }
        public void RemoveLossySubscriber(int moduleId) { }

        public void SendMessage(IPEndPoint destination, byte[] data, int moduleId, int priority = 0)
        {
            if (ThrowOnSend)
            {
                throw new InvalidOperationException("send failed");
            }
        }

        public void SendMessageLossy(IPEndPoint destination, byte[] data, int moduleId, int priority = 0) { }

        public void BroadcastMessage(byte[] data, int moduleId, int priority = 0)
        {
            throw new InvalidOperationException("broadcast failed");
        }

        public void BroadcastMessageLossy(byte[] data, int moduleId, int priority = 0) { }
        public void JoinMulticastGroup(int groupId, IPEndPoint endPoint) { }
        public void LeaveMulticastGroup(int groupId, IPEndPoint endPoint) { }
        public void MulticastMessage(int groupId, byte[] data, int moduleId, int priority = 0) { }
        public void MulticastMessageLossy(int groupId, byte[] data, int moduleId, int priority = 0) { }
    }
}
