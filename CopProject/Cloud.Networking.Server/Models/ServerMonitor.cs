// Author = Simeon Kandan Sonar
// Roll Number = 112301031

using Microsoft.Extensions.Logging.Abstractions;

namespace Cloud.Networking.Server.Models;

/// <summary>
/// Monitors live server connections and message changed signals.
/// </summary>
public sealed class ServerMonitor : IServerMonitor
{
    private readonly object _gate = new();
    private readonly Dictionary<string, DateTimeOffset> _connections = [];
    private readonly IMessageStore _store;
    private readonly ILogger<ServerMonitor> _logger;

    /// <summary>
    /// invoked on addition of messages or addition or removal of connections
    /// </summary>
    public event Action? Changed;

    /// <summary>Any message added to the store notifies listeners.</summary>
    private void OnMessageAdded(Message message)
    {
        _logger.LogDebug("Message {MessageId} was added to the store, notifying listeners", message.Id);
        Changed?.Invoke();
    }

    /// <summary>Initializes the monitor and starts listening to the store for new messages.</summary>
    /// <param name="store">The store that owns the messages.</param>
    /// <param name="logger">Where connection changes are logged (optional)</param>
    public ServerMonitor(IMessageStore store, ILogger<ServerMonitor>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(store);

        _store = store;
        _logger = logger ?? NullLogger<ServerMonitor>.Instance;
        _store.MessageAdded += OnMessageAdded;
    }


    /// <inheritdoc />
    public IReadOnlyList<LiveConnection> Connections
    {
        get {
            lock (_gate)
            {
                return _connections
                    .Select(x => new LiveConnection(x.Key, x.Value))
                    .OrderBy(x => x.ConnectedAt)
                    .ToList();
            }
        }
    }

    /// <inheritdoc />
    public int ConnectionCount
    {
        get {
            lock (_gate)
            {
                return _connections.Count;
            }
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Message>> GetRecentMessagesAsync(int count)
    {
        return _store.GetRecentAsync(count);
    }

    /// <inheritdoc />
    public void Connected(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        int active;
        lock (_gate)
        {
            _connections[connectionId] = DateTimeOffset.UtcNow;
            active = _connections.Count;
        }

        _logger.LogInformation("Connection {ConnectionId} is now tracked, {Active} active connection(s)", connectionId, active);
        Changed?.Invoke();
    }

    /// <inheritdoc />
    public void Disconnected(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        bool removed;
        int active;
        lock (_gate)
        {
            removed = _connections.Remove(connectionId);
            active = _connections.Count;
        }

        if (removed)
        {
            _logger.LogInformation("Connection {ConnectionId} is no longer tracked, {Active} active connection(s)", connectionId, active);
        }
        else
        {
            _logger.LogWarning("Connection {ConnectionId} disconnected but was not being tracked", connectionId);
        }

        Changed?.Invoke();
    }

    /// <summary>Stops listening to the store.</summary>
    public void Dispose()
    {
        _store.MessageAdded -= OnMessageAdded;
    }
}
