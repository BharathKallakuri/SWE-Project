// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.SignalR.Client;

namespace Cloud.Networking;

/// <summary>
/// Implements IMessenger using SignalR.
/// </summary>
public sealed class SignalRMessenger : IMessenger
{
    private readonly HubConnection _connection;

    /* events that functions can subscribe to */
    public event Action<Message>? MessageReceived;
    public event Action<Exception?>? Reconnecting;
    public event Action<string?>? Reconnected;
    public event Action<Exception?>? Closed;

    /// <summary>
    /// Gets the current state of the SignalR connection.
    /// </summary>
    public HubConnectionState State => _connection.State;

    /// <summary>
    /// Creates a new SignalR connection and registers the required handlers.
    /// </summary>
    public SignalRMessenger(string serverUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverUrl);

        _connection = new HubConnectionBuilder()
            .WithUrl(serverUrl)
            .WithAutomaticReconnect()
            .Build();

        // Listen for messages coming from the server.
        _connection.On<Message>(
            Contract.ReceiveMessage,
            message => MessageReceived?.Invoke(message));

        // Called when SignalR starts trying to reconnect.
        _connection.Reconnecting += exception => {
            Reconnecting?.Invoke(exception);
            return Task.CompletedTask;
        };

        // Called when the connection is successfully restored.
        _connection.Reconnected += connectionId => {
            Reconnected?.Invoke(connectionId);
            return Task.CompletedTask;
        };

        // Called when the connection is closed.
        _connection.Closed += exception => {
            Closed?.Invoke(exception);
            return Task.CompletedTask;
        };
    }

    /// <summary>
    /// Connects to the server.
    /// </summary>
    public async Task ConnectAsync()
    {
        await _connection.StartAsync();
    }

    /// <summary>
    /// Sends a message to the server (client doing RPC on server).
    /// </summary>
    public async Task SendMessageAsync(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        await _connection.InvokeAsync(Contract.SendMessage, content);
    }

    /// <summary>
    /// Disconnects from the server (connection can be reused).
    /// </summary>
    public async Task DisconnectAsync()
    {
        await _connection.StopAsync();
    }

    /// <summary>
    /// Disposes off the connection (connection cannot be reused).
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}
