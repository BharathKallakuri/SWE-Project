// Author = Simeon Kandan Sonar
// Roll Number = 112301031

using Cloud.Networking.Mocking;

namespace Cloud.Networking.Decorators;

/// <summary>
/// Decorator for <see cref="IMessenger"/> that adds diagnostic tracing.
/// any exceptionis traced and then rethrown, so callers still see failures.
/// </summary>
internal sealed class DiagnosticMessenger : IMessenger
{
    private readonly IMessenger _messenger;
    private readonly ITracer _tracer;

    public ConnectionState State => _messenger.State;

    public event Action<Message>? MessageReceived;
    public event Action<Exception?>? Reconnecting;
    public event Action<string?>? Reconnected;
    public event Action<Exception?>? Closed;

    /// <summary>
    /// Creates an instance of <see cref="DiagnosticMessenger"/> class.
    /// </summary>
    /// <param name="messenger">The messenger to decorate</param>
    /// <param name="tracer">Where the trace lines are written</param>
    public DiagnosticMessenger(IMessenger messenger, ITracer tracer)
    {
        ArgumentNullException.ThrowIfNull(messenger);
        ArgumentNullException.ThrowIfNull(tracer);

        _messenger = messenger;
        _tracer = tracer;

        _messenger.MessageReceived += OnMessageReceived;
        _messenger.Reconnecting += OnReconnecting;
        _messenger.Reconnected += OnReconnected;
        _messenger.Closed += OnClosed;
    }

    /// <summary>
    /// Like <see cref="IMessenger.ConnectAsync"/> but traces the attempt and its outcome
    /// </summary>
    public async Task ConnectAsync()
    {
        try
        {
            _tracer.Info("-- connecting");
            await _messenger.ConnectAsync();
            _tracer.Info("-- connected");
        }
        catch (Exception ex)
        {
            _tracer.WriteError($"-- connection failed: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Like <see cref="IMessenger.SendMessageAsync"/> but traces the outcome
    /// </summary>
    public async Task SendMessageAsync(string content)
    {
        try
        {
            await _messenger.SendMessageAsync(content);
            _tracer.Info("-- message sent");
        }
        catch (Exception ex)
        {
            _tracer.WriteError($"-- message sending failed: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Like <see cref="IMessenger.SyncAsync"/> but traces how many messages were synced
    /// </summary>
    public async Task<IReadOnlyList<Message>> SyncAsync(DateTimeOffset since)
    {
        try
        {
            IReadOnlyList<Message> missed = await _messenger.SyncAsync(since);
            _tracer.Info($"-- synced {missed.Count} message(s)");
            return missed;
        }
        catch (Exception ex)
        {
            _tracer.WriteError($"-- sync failed: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Like <see cref="IMessenger.DisconnectAsync"/> but traces the outcome
    /// </summary>
    public async Task DisconnectAsync()
    {
        try
        {
            await _messenger.DisconnectAsync();
            _tracer.Info("-- disconnected");
        }
        catch (Exception ex)
        {
            _tracer.WriteError($"-- disconnection failed: {ex}");
            throw;
        }
    }

    private void OnMessageReceived(Message message)
    {
        _tracer.Info($"-- message received from {message.SenderId}");
        MessageReceived?.Invoke(message);
    }

    private void OnReconnecting(Exception? exception)
    {
        _tracer.WriteWarn($"-- reconnecting: {exception?.Message}");
        Reconnecting?.Invoke(exception);
    }

    private void OnReconnected(string? connectionId)
    {
        _tracer.Info($"-- reconnected: {connectionId}");
        Reconnected?.Invoke(connectionId);
    }

    private void OnClosed(Exception? exception)
    {
        _tracer.WriteWarn($"-- connection closed: {exception?.Message}");
        Closed?.Invoke(exception);
    }

    /// <summary>
    /// Like <see cref="IAsyncDisposable.DisposeAsync"/> but traces a failure
    /// </summary>
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
            _tracer.WriteError($"-- disposal failed: {ex}");
            throw;
        }
    }
}
