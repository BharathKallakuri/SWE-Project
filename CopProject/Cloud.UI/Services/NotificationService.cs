// Author = Simeon Kandan Sonar
// Roll Number = 112301031 

using System.Diagnostics;
using Cloud.Networking;

namespace Cloud.UI.Services;

/// <summary>
/// Owns the messenger (the connection to the server) 
/// </summary>
public sealed class NotificationService : IAsyncDisposable
{
    private readonly Func<string, IMessenger> _createMessenger;

    private IMessenger? _messenger;
    private string _messengerUrl = string.Empty;

    /// <param name="createMessenger">Creates a messenger for a server URL, e.g. <c>url =&gt; MessengerFactory.GetMessenger(url)</c>.</param>
    public NotificationService(Func<string, IMessenger> createMessenger)
    {
        ArgumentNullException.ThrowIfNull(createMessenger);
        _createMessenger = createMessenger;
    }

    /// <summary>Raised on the calling thread when the old messenger is dropped for a different server.</summary>
    public event Action? ServerChanged;

    public event Action<Message>? MessageReceived;
    public event Action<Exception?>? Reconnecting;
    public event Action<string?>? Reconnected;
    public event Action<Exception?>? Closed;

    /// <summary>State of the current messenger; Disconnected when there is none.</summary>
    public ConnectionState State => _messenger?.State ?? ConnectionState.Disconnected;

    private IMessenger Messenger => _messenger ?? throw new InvalidOperationException("Not connected.");

    /// <summary>Accepts only absolute http/https URLs; <paramref name="url"/> is then the normalised form.</summary>
    public static bool TryParseUrl(string text, out string url)
    {
        if (Uri.TryCreate(text.Trim(), UriKind.Absolute, out Uri? uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            url = uri.AbsoluteUri;
            return true;
        }

        url = string.Empty;
        return false;
    }

    /// <summary>
    /// Makes the service ready to connect to <paramref name="url"/> from a clean state:
    /// same server: the messenger is reused; different server: it is replaced.
    /// An existing connection is closed.
    /// </summary>
    public async Task UseServerAsync(string url)
    {
        // Same server: reuse the messenger. Different server: replace it.
        IMessenger messenger = _messenger is not null && url == _messengerUrl
            ? _messenger
            : await SwitchServerAsync(url);

        if (messenger.State != ConnectionState.Disconnected)
        {
            Trace.TraceInformation($"Closing the existing connection before connecting to {url}.");
            await messenger.DisconnectAsync();
        }
    }

    /// <summary>Connects to the server chosen with <see cref="UseServerAsync"/>.</summary>
    public async Task ConnectAsync()
    {
        Trace.TraceInformation($"Connecting to {_messengerUrl}.");
        await Messenger.ConnectAsync();
        Trace.TraceInformation($"Connect finished; state is {State}.");
    }

    public async Task DisconnectAsync()
    {
        if (_messenger is not null)
        {
            Trace.TraceInformation($"Disconnecting from {_messengerUrl}.");
            await _messenger.DisconnectAsync();
        }
    }

    public async Task SendAsync(string text)
    {
        Trace.TraceInformation($"Sending a message ({text.Length} characters).");
        await Messenger.SendMessageAsync(text);
    }

    /// <summary>Messages newer than <paramref name="since"/> that the server has.</summary>
    public async Task<IReadOnlyList<Message>> SyncAsync(DateTimeOffset since)
    {
        Trace.TraceInformation($"Syncing messages newer than {since:O}.");
        IReadOnlyList<Message> missed = await Messenger.SyncAsync(since);
        Trace.TraceInformation($"Sync returned {missed.Count} message(s).");
        return missed;
    }

    public ValueTask DisposeAsync()
    {
        Trace.TraceInformation("Disposing the notification service.");
        return new ValueTask(DisposeMessengerAsync());
    }

    /// <summary>Drops the current messenger (and the old server's notifications) and creates one for <paramref name="url"/>.</summary>
    private async Task<IMessenger> SwitchServerAsync(string url)
    {
        Trace.TraceInformation($"Switching to server {url}.");
        await DisposeMessengerAsync();
        ServerChanged?.Invoke();

        IMessenger messenger = _createMessenger(url);
        messenger.MessageReceived += OnMessageReceived;
        messenger.Reconnecting += OnReconnecting;
        messenger.Reconnected += OnReconnected;
        messenger.Closed += OnClosed;

        _messenger = messenger;
        _messengerUrl = url;
        return messenger;
    }

    private async Task DisposeMessengerAsync()
    {
        IMessenger? messenger = _messenger;
        if (messenger is null)
        {
            return;
        }

        _messenger = null;
        messenger.MessageReceived -= OnMessageReceived;
        messenger.Reconnecting -= OnReconnecting;
        messenger.Reconnected -= OnReconnected;
        messenger.Closed -= OnClosed;

        try
        {
            await messenger.DisposeAsync();
        }
        catch (Exception ex)
        {
            // Best effort: the connection is being discarded anyway.
            Trace.TraceWarning($"Ignoring an error while disposing the messenger: {ex.Message}");
        }
    }

    private void OnMessageReceived(Message message)
    {
        Trace.TraceInformation($"Message {message.Id} received.");
        MessageReceived?.Invoke(message);
    }

    private void OnReconnecting(Exception? exception)
    {
        Trace.TraceWarning($"Connection lost, trying to reconnect: {exception?.Message}");
        Reconnecting?.Invoke(exception);
    }

    private void OnReconnected(string? connectionId)
    {
        Trace.TraceInformation($"Reconnected (connection id {connectionId}).");
        Reconnected?.Invoke(connectionId);
    }

    private void OnClosed(Exception? exception)
    {
        if (exception is not null)
        {
            Trace.TraceError($"Connection closed with an error: {exception.Message}");
        }
        else
        {
            Trace.TraceInformation("Connection closed.");
        }

        Closed?.Invoke(exception);
    }
}
