// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Networking;

namespace Whiteboard.Test.Mocks.Networking;

/// <summary>
/// Factory for mock communicators
/// </summary>
public class MockCommunicatorFactory
{
    /// <summary>
    /// Singleton mock server communicator
    /// </summary>
    private readonly MockCommunicator _serverCommunicator;

    /// <summary>
    /// Singleton mock network
    /// </summary>
    private readonly MockNetwork _mockNetwork = new();

    /// <summary>
    /// Next mock port for allocation
    /// </summary>
    private int _nextPort;

    public MockCommunicatorFactory()
    {
        _serverCommunicator = new MockCommunicator(_mockNetwork, _nextPort++);
        _mockNetwork.AddCommunicator(_serverCommunicator);
    }

    /// <summary>
    /// Get the server communicator
    /// </summary>
    /// <returns>The server communicator</returns>
    public ICommunicator GetServerCommunicator()
    {
        return _serverCommunicator;
    }

    /// <summary>
    /// Create a new client communicator
    /// </summary>
    /// <returns>A new client communicator</returns>
    public ICommunicator CreateClientCommunicator()
    {
        MockCommunicator clientCommunicator = new MockCommunicator(_mockNetwork, _nextPort++);
        _mockNetwork.AddCommunicator(clientCommunicator);

        return clientCommunicator;
    }
}
