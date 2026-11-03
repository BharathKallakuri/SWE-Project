// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;
using Networking;

namespace Whiteboard.Test.Mocks.Networking;

/// <inheritdoc/>
internal class MockCommunicator : ICommunicator
{
    /// <summary>
    /// The network this communicator connects to
    /// </summary>
    private readonly MockNetwork _mockNetwork;

    public int ListenPort { get; }
    public IPAddress ListenIpAddress { get; } = IPAddress.Loopback;

    /// <summary>
    /// Subscriber callback function
    /// </summary>
    private IMessageListener? _subscriber = null;

    /// <summary>
    /// Simulate a message received
    /// </summary>
    /// <param name="message">Message received</param>
    public void InvokeSubscriber(byte[] message)
    {
        _subscriber?.OnMessageReceived(message);
    }

    public MockCommunicator(MockNetwork network, int port)
    {
        _mockNetwork = network;
        ListenPort = port;
    }

    public void AddSubscriber(int module, IMessageListener subscriber)
    {
        _subscriber = subscriber;
    }

    public void RemoveSubscriber(int module)
    {
        _subscriber = null;
    }

    public void SendMessage(IPEndPoint dest, byte[] message, int module, int priority)
    {
        _mockNetwork.SendTo(dest, message);
    }

    public void BroadcastMessage(byte[] message, int module, int priority)
    {
        _mockNetwork.SendAll(this, message);
    }

    public void JoinMulticastGroup(int group, IPEndPoint clientIp)
    {
        throw new NotImplementedException();
    }

    public void LeaveMulticastGroup(int group, IPEndPoint clientIp)
    {
        throw new NotImplementedException();
    }

    public void MulticastMessage(int group, byte[] message, int module, int priority)
    {
        throw new NotImplementedException();
    }
}
