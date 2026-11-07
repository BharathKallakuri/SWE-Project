// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Cloud.Networking;

/// <summary>
/// Decorator for IMessenger that adds diagnostic tracing.
/// </summary>
internal sealed class SilentDiagnosticMessenger : IMessenger
{
    private readonly IMessenger _messenger;

    public ConnectionState State => _messenger.State;

    public event Action<Message>? MessageReceived;
    public event Action<Exception?>? Reconnecting;
    public event Action<string?>? Reconnected;
    public event Action<Exception?>? Closed;

    public SilentDiagnosticMessenger(IMessenger messenger)
    {
        ArgumentNullException.ThrowIfNull(messenger);

        _messenger = messenger;

        _messenger.MessageReceived += OnMessageReceived;
        _messenger.Reconnecting += OnReconnecting;
        _messenger.Reconnected += OnReconnected;
        _messenger.Closed += OnClosed;
    }

    public async Task ConnectAsync()
    {
        try
        {
            Trace.WriteLine("-- connecting");
            await _messenger.ConnectAsync();
            Trace.WriteLine("-- connected");
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"-- connection failed: {ex}");
            //throw;
        }
    }

    public async Task SendMessageAsync(string content)
    {
        try
        {
            await _messenger.SendMessageAsync(content);
            Trace.WriteLine("-- message sent");
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"-- message sending failed: {ex}");
            //throw;
        }
    }

    public async Task<IReadOnlyList<Message>> SyncAsync(DateTimeOffset since)
    {
        try
        {
            IReadOnlyList<Message> missed = await _messenger.SyncAsync(since);
            Trace.WriteLine($"-- synced {missed.Count} message(s)");
            return missed;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"-- sync failed: {ex}");
            return Array.Empty<Message>();
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            await _messenger.DisconnectAsync();
            Trace.WriteLine("-- disconnected");
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"-- disconnection failed: {ex}");
            //throw;
        }
    }

    private void OnMessageReceived(Message message)
    {
        Trace.WriteLine($"-- message received from {message.SenderId}");
        MessageReceived?.Invoke(message);
    }

    private void OnReconnecting(Exception? exception)
    {
        Trace.WriteLine($"-- reconnecting: {exception?.Message}");
        Reconnecting?.Invoke(exception);
    }

    private void OnReconnected(string? connectionId)
    {
        Trace.WriteLine($"-- reconnected: {connectionId}");
        Reconnected?.Invoke(connectionId);
    }

    private void OnClosed(Exception? exception)
    {
        Trace.WriteLine($"-- connection closed: {exception?.Message}");
        Closed?.Invoke(exception);
    }

    public async ValueTask DisposeAsync()
    {
        _messenger.MessageReceived -= OnMessageReceived;
        _messenger.Reconnecting -= OnReconnecting;
        _messenger.Reconnected -= OnReconnected;
        _messenger.Closed -= OnClosed;

        try
        {
            await _messenger.DisposeAsync();
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"-- disposal failed: {ex}");
            //throw;
        }
    }
}
