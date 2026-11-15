// Author = Simeon Kandan Sonar
// Roll Number = 112301031

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cloud.Networking.Server.Models;

/// <summary>
/// A hub for broadcasting and syncing messages.
/// </summary>
public class MessageHub : Hub
{
    /// <summary>Identifier used for messages originating from the server.</summary>
    public const string ServerSenderId = "server";

    /// <summary>The longest message content (after trimming) that is accepted.</summary>
    public const int MaxContentLength = 2_000;

    private readonly IMessageStore _store;
    private readonly ILogger<MessageHub> _logger;

    /// <summary>
    /// Initializes the hub with <see cref="IMessageStore"/> instance.
    /// </summary>
    /// <param name="store">a <see cref="IMessageStore"/> instance.</param>
    /// <param name="logger">Where message traffic is logged (optional)</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="store"/> is null.</exception>
    public MessageHub(IMessageStore store, ILogger<MessageHub>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(store);

        _store = store;
        _logger = logger ?? NullLogger<MessageHub>.Instance;
    }

    /// <summary>
    /// Broadcasts messages to all clients connected through this hub
    /// </summary>
    /// <param name="content">the content of the message to be sent.</param>
    /// <returns>the <see cref="Message"/> that was sent</returns>
    /// <exception cref="HubException">Thrown when the <paramref name="content"/> is empty, whitespace or longer than <see cref="MaxContentLength"/>.</exception>
    public async Task<Message> SendMessage(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new HubException("Message cannot be empty.");
        }

        string trimmed = content.Trim();
        if (trimmed.Length > MaxContentLength)
        {
            throw new HubException($"Message cannot be longer than {MaxContentLength} characters.");
        }

        Message message = new() {
            SenderId = Context.ConnectionId, // TODO: some other id for auth and better identity
            Content = trimmed
        };

        await _store.AddAsync(message);
        await Clients.All.SendAsync(Contract.ReceiveMessage, message);

        /* the content itself is not logged, only who sent it and how long it was */
        _logger.LogInformation("Message {MessageId} from {ConnectionId} ({Length} characters) stored and broadcast", message.Id, message.SenderId, trimmed.Length);
        return message;
    }

    /// <summary>
    /// Gets the messages created after <paramref name="since"/> in <see cref="IMessageStore"/> instance.
    /// </summary>
    /// <param name="since">the timestamp which determines the threshold.</param>
    /// <returns>A list of messages after "since".</returns>
    public async Task<Message[]> GetMessagesSince(DateTimeOffset since)
    {
        Message[] missed = [.. await _store.GetSinceAsync(since)];

        _logger.LogInformation("Connection {ConnectionId} synced {Count} message(s) since {Since}", Context?.ConnectionId, missed.Length, since);
        return missed;
    }
}
