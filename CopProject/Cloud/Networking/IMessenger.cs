// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Cloud.Networking;

/// <summary>
/// Clients use this interface to communicate with server.
/// </summary>
public interface IMessenger : IAsyncDisposable
{
    /// <summary>
    /// Connects to the server.
    /// </summary>
    Task ConnectAsync();

    /// <summary>
    /// Sends content of message to the server.
    /// </summary>
    Task SendMessageAsync(string content);

    /// <summary>
    /// Disconnects from the server, but does not dispose the connection.
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Notify the subscribers when a message is received.
    /// </summary>
    event Action<Message>? MessageReceived;
}
