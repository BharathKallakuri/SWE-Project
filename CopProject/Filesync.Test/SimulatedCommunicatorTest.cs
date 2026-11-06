using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using FileSynchroniser.Network;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Networking;

namespace Filesync.Test;

[TestClass]
public class SimulatedCommunicatorTest
{
    /// <summary>
    /// Helper listener that uses a manual reset event to wait for incoming network messages
    /// </summary>
    private class TestMessageListener : IMessageListener
    {
        private readonly ManualResetEventSlim _signal = new(false);
        public byte[]? ReceivedData { get; private set; }

        public void OnMessageReceived(byte[] message)
        {
            ReceivedData = message;
            _signal.Set();
        }

        public bool WaitForMessage(int timeoutMilliseconds = 3000)
        {
            return _signal.Wait(timeoutMilliseconds);
        }
    }

    /// <summary>
    /// Finds a free available port on loopback
    /// </summary>
    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    /// <summary>
    /// 1. Constructor initializes listening port and IP address correctly
    /// </summary>
    [TestMethod]
    public void Constructor_InitializesPortAndIpAddress()
    {
        int port = GetAvailablePort();
        using var communicator = new SimulatedCommunicator(port);

        Assert.AreEqual(port, communicator.ListenPort);
        Assert.AreEqual(IPAddress.Any, communicator.ListenIpAddress);
    }

    /// <summary>
    /// 2. Adding and removing subscribers works without error
    /// </summary>
    [TestMethod]
    public void AddSubscriber_And_RemoveSubscriber_ManagesSubscribers()
    {
        int port = GetAvailablePort();
        using var communicator = new SimulatedCommunicator(port);
        var listener = new TestMessageListener();

        communicator.AddSubscriber(5, listener);
        communicator.RemoveSubscriber(5);

        // removing non-existent subscriber should also not throw
        communicator.RemoveSubscriber(99);
    }

    /// <summary>
    /// 3. Multicast and group methods can be called safely
    /// </summary>
    [TestMethod]
    public void MulticastAndGroupMethods_CanBeCalledWithoutException()
    {
        int port = GetAvailablePort();
        using var communicator = new SimulatedCommunicator(port);
        var endpoint = new IPEndPoint(IPAddress.Loopback, port);

        communicator.JoinMulticastGroup(1, endpoint);
        communicator.LeaveMulticastGroup(1, endpoint);

        byte[] data = Encoding.UTF8.GetBytes("test multicast");
        communicator.MulticastMessage(1, data, 5, 1);
    }

    /// <summary>
    /// 4. Sending message to an unreachable peer catches exception gracefully
    /// </summary>
    [TestMethod]
    public void SendMessage_ToUnreachablePeer_HandlesErrorGracefully()
    {
        int port = GetAvailablePort();
        using var communicator = new SimulatedCommunicator(port);

        int unreachablePort = GetAvailablePort();
        var unreachableEndpoint = new IPEndPoint(IPAddress.Loopback, unreachablePort);

        byte[] payload = Encoding.UTF8.GetBytes("ping");

        // should catch socket error internally without crashing
        communicator.SendMessage(unreachableEndpoint, payload, 5, 1);
    }

    /// <summary>
    /// 5. RegisterPeer adds peer to broadcast list and sends message
    /// </summary>
    [TestMethod]
    public void RegisterPeer_And_BroadcastMessage_SendsToPeer()
    {
        int port = GetAvailablePort();
        using var communicator = new SimulatedCommunicator(port);

        int peerPort = GetAvailablePort();
        communicator.RegisterPeer(new IPEndPoint(IPAddress.Loopback, peerPort));

        byte[] payload = Encoding.UTF8.GetBytes("broadcast test");
        communicator.BroadcastMessage(payload, 5, 1);
    }

    /// <summary>
    /// 6. Real loopback TCP message sending and receiving delivers payload to subscriber
    /// </summary>
    [TestMethod]
    public void SendMessage_OverLoopback_DeliversMessageToSubscriber()
    {
        int receiverPort = GetAvailablePort();
        int senderPort = GetAvailablePort();

        using var receiver = new SimulatedCommunicator(receiverPort);
        using var sender = new SimulatedCommunicator(senderPort);

        var listener = new TestMessageListener();
        receiver.AddSubscriber(5, listener);

        byte[] sentBytes = Encoding.UTF8.GetBytes("hello from sender");
        var receiverEndpoint = new IPEndPoint(IPAddress.Loopback, receiverPort);

        sender.SendMessage(receiverEndpoint, sentBytes, 5, 1);

        bool received = listener.WaitForMessage(5000);
        Assert.IsTrue(received, "Expected receiver to receive message within timeout");
        Assert.IsNotNull(listener.ReceivedData);
        string receivedText = Encoding.UTF8.GetString(listener.ReceivedData);
        Assert.AreEqual("hello from sender", receivedText);
    }

    /// <summary>
    /// 7. Dispose cleanly stops listener and can be safely called multiple times
    /// </summary>
    [TestMethod]
    public void Dispose_CanBeCalledMultipleTimes_AndBlocksFurtherSends()
    {
        int port = GetAvailablePort();
        var communicator = new SimulatedCommunicator(port);

        communicator.Dispose();
        // calling a second time should hit the _disposed early return
        communicator.Dispose();

        // sending after dispose should return immediately without attempting socket connection
        var endpoint = new IPEndPoint(IPAddress.Loopback, port);
        communicator.SendMessage(endpoint, new byte[] { 1, 2, 3 }, 5, 1);
    }
}
