// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.SignalR.Client;

namespace Cloud.Networking;

/// <summary>
/// Inherits from <see cref ="IHubConnection" /> interface.
/// </summary>
/// 
/// <remarks>
/// Pass through proxy class which does not deviate from HubConnection at all
/// </remarks>
/// 
/// Reference:
/// https://stackoverflow.com/questions/65009412/how-to-mock-a-wrapped-microsoft-asp-net-core-client-signalr-hubconnection-class
internal sealed class HubConnectionProxy : IHubConnection
{
    private readonly HubConnection _connection;

    public HubConnectionProxy(HubConnection connection)
    {
        _connection = connection;
    }

    public HubConnectionState State => _connection.State;

    public event Func<Exception?, Task>? Reconnecting
    {
        add => _connection.Reconnecting += value;
        remove => _connection.Reconnecting -= value;
    }

    public event Func<string?, Task>? Reconnected
    {
        add => _connection.Reconnected += value;
        remove => _connection.Reconnected -= value;
    }

    public event Func<Exception?, Task>? Closed
    {
        add => _connection.Closed += value;
        remove => _connection.Closed -= value;
    }

    public IDisposable On<T>(string methodName, Action<T> handler)
    {
        return _connection.On(methodName, handler);
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        return _connection.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        return _connection.StopAsync(cancellationToken);
    }

    public Task InvokeAsync(string methodName, object? arg, CancellationToken cancellationToken = default)
    {
        return _connection.InvokeAsync(methodName, arg, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _connection.DisposeAsync();
    }
}
