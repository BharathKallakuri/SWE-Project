// Author = Simeon Kandan Sonar
// Roll Number = 112301031

using Microsoft.Extensions.Logging.Abstractions;

namespace Cloud.Networking.Server.Models;

/// <summary>
/// Stores the message in memory as a FIFO queue and MaxMessages limit.
/// </summary>
public sealed class InMemoryMessageStore : IMessageStore
{
    public const int DefaultMaxMessages = 10_000;

    public int MaxMessages { get; }
    private readonly object _gate = new();
    private readonly Queue<Message> _messages = new();
    private readonly ILogger<InMemoryMessageStore> _logger;

    /// <summary>
    /// Initializes the store with <paramref name="maxMessages"/> limit.
    /// </summary>
    /// <param name="maxMessages">A maximum number of messages limit</param>
    /// <param name="logger">Where storage activity is logged (optional)</param>
    public InMemoryMessageStore(int maxMessages = DefaultMaxMessages, ILogger<InMemoryMessageStore>? logger = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxMessages);
        MaxMessages = maxMessages;
        _logger = logger ?? NullLogger<InMemoryMessageStore>.Instance;
    }

    /// <inheritdoc />
    public event Action<Message>? MessageAdded;

    /// <inheritdoc />
    public Task AddAsync(Message message)
    {
        // FIFO Policy
        ArgumentNullException.ThrowIfNull(message);

        int dropped = 0;
        int stored;
        lock (_gate)
        {
            _messages.Enqueue(message);
            while (_messages.Count > MaxMessages)
            {
                _messages.Dequeue();
                dropped++;
            }

            stored = _messages.Count;
        }

        _logger.LogDebug("Stored message {MessageId}, {Count} message(s) in the store", message.Id, stored);
        if (dropped > 0)
        {
            /* messages are lost for good here, clients that sync later will not get them */
            _logger.LogWarning("The store is full ({MaxMessages}), dropped the {Dropped} oldest message(s)", MaxMessages, dropped);
        }

        // subscribers could be slow (hence outside) 
        MessageAdded?.Invoke(message);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Message>> GetSinceAsync(DateTimeOffset since)
    {
        Message[] newer;
        lock (_gate)
        {
            newer = _messages.Where(m => m.Timestamp > since).ToArray();
        }

        // expensive sorting (hence outside)
        IReadOnlyList<Message> result = newer.OrderBy(m => m.Timestamp).ToArray();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Message>> GetRecentAsync(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        lock (_gate)
        {
            // as we need newest first
            IReadOnlyList<Message> result = _messages
                .Skip(Math.Max(0, _messages.Count - count))
                .Reverse()
                .ToArray();

            return Task.FromResult(result);
        }
    }
}
