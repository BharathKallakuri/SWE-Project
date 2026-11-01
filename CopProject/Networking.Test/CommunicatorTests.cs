using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Networking.TCPCommunication;

namespace Networking.Test;

/// <summary>
/// Unit tests for CommunicatorServer and CommunicatorClient.
/// </summary>
[TestClass]
public class CommunicatorTests
{
    private sealed class TestMessageListener : IMessageListener
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

        public void Reset()
        {
            _signal.Reset();
            ReceivedData = null;
        }
    }

    private static int GetAvailablePort()
    {
        using TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    /// <summary>
    /// Checks that CommunicatorServer binds to the given port and loopback IP.
    /// </summary>
    [TestMethod]
    public void CommunicatorServer_Initialize_ListensOnConfiguredPort()
    {
        int port = GetAvailablePort();
        IPEndPoint endpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(endpoint);

        Assert.AreEqual(port, server.ListenPort);
        Assert.AreEqual(IPAddress.Loopback, server.ListenIpAddress);

        using TcpClient probe = new();
        probe.Connect(IPAddress.Loopback, port);
        Assert.IsTrue(probe.Connected);
    }

    /// <summary>
    /// Checks that a server created with a null endpoint finds an available port and IP.
    /// </summary>
    [TestMethod]
    public void CommunicatorServer_InitializeWithNull_AssignsValidIpAddress()
    {
        using CommunicatorServer server = new(null);

        Assert.IsNotNull(server.ListenIpAddress);
        Assert.AreNotEqual(IPAddress.Any, server.ListenIpAddress);
        Assert.IsGreaterThan(0, server.ListenPort);
    }

    /// <summary>
    /// Checks adding and removing a subscriber on CommunicatorServer.
    /// </summary>
    [TestMethod]
    public void CommunicatorServer_AddAndRemoveSubscriber_ExecutesWithoutError()
    {
        int port = GetAvailablePort();
        using CommunicatorServer server = new(new IPEndPoint(IPAddress.Loopback, port));
        TestMessageListener listener = new();

        server.AddSubscriber(10, listener);
        server.RemoveSubscriber(10);
    }

    /// <summary>
    /// Checks adding and removing a subscriber on CommunicatorClient.
    /// </summary>
    [TestMethod]
    public void CommunicatorClient_AddAndRemoveSubscriber_ExecutesWithoutError()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);
        using CommunicatorClient client = new(serverEndpoint);
        TestMessageListener listener = new();

        client.AddSubscriber(20, listener);
        client.RemoveSubscriber(20);
    }

    /// <summary>
    /// Checks that CommunicatorClient exposes a non-zero port and loopback IP when connected.
    /// </summary>
    [TestMethod]
    public void CommunicatorClient_ListenPortAndIp_ReturnsValidValuesWhenConnected()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);
        using CommunicatorClient client = new(serverEndpoint);

        Assert.IsGreaterThan(0, client.ListenPort);
        Assert.IsNotNull(client.ListenIpAddress);
        Assert.IsTrue(IPAddress.IsLoopback(client.ListenIpAddress));
    }

    /// <summary>
    /// Checks that client returns IPAddress.Any after disposal.
    /// </summary>
    [TestMethod]
    public void CommunicatorClient_ListenIpAddress_ReturnsAnyWhenDisposed()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);
        CommunicatorClient client = new(serverEndpoint);

        client.Dispose();

        Assert.AreEqual(IPAddress.Any, client.ListenIpAddress);
        Assert.AreEqual(0, client.ListenPort);
    }

    /// <summary>
    /// Checks sending a message from client to server.
    /// </summary>
    [TestMethod]
    public void SendMessage_ClientToServer_DeliversMessageToSubscriber()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);
        TestMessageListener serverListener = new();
        server.AddSubscriber(1, serverListener);

        using CommunicatorClient client = new(serverEndpoint);
        Thread.Sleep(150);

        byte[] payload = Encoding.UTF8.GetBytes("Client to Server Message");
        client.SendMessage(serverEndpoint, payload, 1, 1);

        Assert.IsTrue(serverListener.WaitForMessage(3000));
        CollectionAssert.AreEqual(payload, serverListener.ReceivedData);
    }

    /// <summary>
    /// Checks broadcasting a message from server to a connected client.
    /// </summary>
    [TestMethod]
    public void BroadcastMessage_ServerToClients_DeliversToSubscribedClient()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);

        using CommunicatorClient client = new(serverEndpoint);
        TestMessageListener clientListener = new();
        client.AddSubscriber(5, clientListener);
        Thread.Sleep(150);

        byte[] broadcastPayload = Encoding.UTF8.GetBytes("Server Broadcast Payload");
        server.BroadcastMessage(broadcastPayload, 5, 1);

        Assert.IsTrue(clientListener.WaitForMessage(3000));
        CollectionAssert.AreEqual(broadcastPayload, clientListener.ReceivedData);
    }

    /// <summary>
    /// Checks that sending to an unregistered module does not throw.
    /// </summary>
    [TestMethod]
    public void SendMessage_UnregisteredModule_DoesNotThrow()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);
        using CommunicatorClient client = new(serverEndpoint);
        Thread.Sleep(150);

        byte[] payload = Encoding.UTF8.GetBytes("Unregistered Module Message");
        client.SendMessage(serverEndpoint, payload, 999, 1);

        Thread.Sleep(200);
    }

    /// <summary>
    /// Checks that all connected clients receive a broadcast message.
    /// </summary>
    [TestMethod]
    public void BroadcastMessage_MultipleClients_BothReceiveBroadcast()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);

        using CommunicatorClient client1 = new(serverEndpoint);
        using CommunicatorClient client2 = new(serverEndpoint);

        TestMessageListener listener1 = new();
        TestMessageListener listener2 = new();
        client1.AddSubscriber(8, listener1);
        client2.AddSubscriber(8, listener2);

        Thread.Sleep(200);

        byte[] broadcastData = Encoding.UTF8.GetBytes("Multiclient Broadcast");
        server.BroadcastMessage(broadcastData, 8, 1);

        Assert.IsTrue(listener1.WaitForMessage(3000));
        Assert.IsTrue(listener2.WaitForMessage(3000));
        CollectionAssert.AreEqual(broadcastData, listener1.ReceivedData);
        CollectionAssert.AreEqual(broadcastData, listener2.ReceivedData);
    }

    /// <summary>
    /// Checks CommunicatorServer multicast message delivery to group members.
    /// </summary>
    [TestMethod]
    public void CommunicatorServer_MulticastMessage_DeliversOnlyToSubscribedGroupMembers()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);

        using CommunicatorClient client1 = new(serverEndpoint);
        using CommunicatorClient client2 = new(serverEndpoint);

        TestMessageListener listener1 = new();
        TestMessageListener listener2 = new();
        client1.AddSubscriber(15, listener1);
        client2.AddSubscriber(15, listener2);

        Thread.Sleep(200);

        IPEndPoint client1Ep = new(client1.ListenIpAddress, client1.ListenPort);
        server.JoinMulticastGroup(101, client1Ep);

        byte[] payload = Encoding.UTF8.GetBytes("Multicast Message Payload");
        server.MulticastMessage(101, payload, 15, 1);

        Assert.IsTrue(listener1.WaitForMessage(3000));
        CollectionAssert.AreEqual(payload, listener1.ReceivedData);
        Assert.IsFalse(listener2.WaitForMessage(500));

        listener1.Reset();
        server.LeaveMulticastGroup(101, client1Ep);
        server.MulticastMessage(101, payload, 15, 1);
        Assert.IsFalse(listener1.WaitForMessage(500));

        server.JoinMulticastGroup(202);
        server.LeaveMulticastGroup(202);
        server.MulticastMessage(999, payload, 15, 1);
    }

    /// <summary>
    /// Checks CommunicatorServer client removal for existing and non-existing clients.
    /// </summary>
    [TestMethod]
    public void CommunicatorServer_RemoveClient_HandlesPresentAndMissingClients()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);

        IPEndPoint nonExisting = new(IPAddress.Loopback, 12345);
        server.RemoveClient(nonExisting);

        using TcpClient connectedTcp = new();
        connectedTcp.Connect(IPAddress.Loopback, port);
        IPEndPoint clientEp = (IPEndPoint)connectedTcp.Client.LocalEndPoint!;

        server.AddClient(clientEp, connectedTcp);
        server.RemoveClient(clientEp);
    }

    /// <summary>
    /// Checks CommunicatorServer initialization failure when port is already in use.
    /// </summary>
    [TestMethod]
    public void CommunicatorServer_PortAlreadyInUse_CatchesExceptionAndDisposesSafely()
    {
        int port = GetAvailablePort();
        using TcpListener blocker = new(IPAddress.Loopback, port);
        blocker.Start();

        CommunicatorServer failedServer = new(new IPEndPoint(IPAddress.Loopback, port));
        failedServer.Dispose();
    }

    /// <summary>
    /// Checks that multiple Dispose calls on CommunicatorServer and CommunicatorClient are safe.
    /// </summary>
    [TestMethod]
    public void Communicator_MultipleDispose_DoesNotThrow()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        CommunicatorServer server = new(serverEndpoint);
        CommunicatorClient client = new(serverEndpoint);

        server.Dispose();
        server.Dispose();

        client.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Checks CommunicatorClient initialization failure with unreachable endpoint.
    /// </summary>
    [TestMethod]
    public void CommunicatorClient_UnreachableServer_CatchesException()
    {
        IPEndPoint closedEndpoint = new(IPAddress.Loopback, 1);
        CommunicatorClient client = new(closedEndpoint);

        Assert.AreEqual(0, client.ListenPort);
        Assert.AreEqual(IPAddress.Any, client.ListenIpAddress);
        client.Dispose();

        Assert.AreEqual(0, client.ListenPort);
        Assert.AreEqual(IPAddress.Any, client.ListenIpAddress);
    }
}
