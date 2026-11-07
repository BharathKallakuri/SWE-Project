// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.SignalR;

namespace Cloud.Networking.Server.Models;

/// <summary>
/// Default <see cref="IMessageBroadcaster"/>: store first, then push to everyone connected.
/// Used by both <see cref="MessageHub"/> (client messages) and the dashboard (server messages).
/// </summary>
public sealed class MessageBroadcaster : IMessageBroadcaster
{
    /// <summary>Identifier used for messages originating from the server.</summary>
    public const string ServerSenderId = "server";

    /// <summary>The longest message content (after trimming) that is accepted.</summary>
    public const int MaxContentLength = 2_000;

    private readonly IMessageStore _store;
    private readonly IHubContext<MessageHub> _hub; /* to call hub methods outside of hub we use IHubContext */

    /// <summary>Initializes the broadcaster.</summary>
    /// <param name="store">Where messages are kept for later sync.</param>
    /// <param name="hub">The hub context used to reach connected clients.</param>
    public MessageBroadcaster(IMessageStore store, IHubContext<MessageHub> hub)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(hub);

        _store = store;
        _hub = hub;
    }

    /// <inheritdoc />
    public async Task<Message> BroadcastAsync(string senderId, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(senderId);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        Message message = new() {
            SenderId = senderId,
            Content = content.Trim()
        };

        // Store first: if the push below fails, clients can still get it by syncing.
        // The store raises MessageAdded, which is how the monitor/dashboard find out.
        await _store.AddAsync(message);
        await _hub.Clients.All.SendAsync(Contract.ReceiveMessage, message);
        return message;
    }
}
