using System.Net;
using System.Text.Json;
using Networking;

namespace Chat;

/// <summary>
/// Maintains a client's local chatroom copy and submits requests to the server.
/// </summary>
public sealed class ChatClient : IMessageListener, IDisposable
{
    private readonly ICommunicator _communicator;
    private readonly IPEndPoint _serverAddress;
    private readonly object _syncRoot = new();
    private int _version = 0;
    private bool _disposed = false;

    /// <summary>
    /// Creates a client using the shared networking communicator.
    /// </summary>
    /// <param name="username">The username this client asserts in its requests.</param>
    /// <param name="chatroom">The client's local chatroom.</param>
    /// <param name="communicator">The networking communicator.</param>
    /// <param name="serverAddress">The server's network address.</param>
    public ChatClient(string username, Chatroom chatroom, ICommunicator communicator, IPEndPoint serverAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentNullException.ThrowIfNull(chatroom);
        ArgumentNullException.ThrowIfNull(communicator);
        ArgumentNullException.ThrowIfNull(serverAddress);

        Username = username;
        Chatroom = chatroom;
        _communicator = communicator;
        _serverAddress = serverAddress;
        _communicator.AddSubscriber(ChatConstants.ModuleId, this);
    }

    /// <summary>
    /// Raised when the server reports the result of a request.
    /// </summary>
    public event Action<Guid, RoomResult>? RequestCompleted;

    /// <summary>
    /// Raised after a server operation has been applied to the local room.
    /// </summary>
    public event Action<ChatOperation>? OperationApplied;

    /// <summary>
    /// Gets the username this client asserts in its requests.
    /// </summary>
    public string Username { get; }

    /// <summary>
    /// Gets the client's local chatroom copy.
    /// </summary>
    public Chatroom Chatroom { get; }

    /// <summary>
    /// Gets the number of server operations applied to the local copy.
    /// </summary>
    public int Version
    {
        get
        {
            lock (_syncRoot)
            {
                return _version;
            }
        }
    }

    /// <summary>
    /// Requests synchronization with the server.
    /// </summary>
    /// <returns>The request ID.</returns>
    public Guid Sync()
    {
        return SendRequest(new global::Chat.Sync());
    }

    /// <summary>
    /// Requests a reset followed by a complete synchronization.
    /// </summary>
    /// <returns>The request ID.</returns>
    public Guid Reset()
    {
        return SendRequest(new global::Chat.Reset());
    }

    /// <summary>
    /// Sends a new room message or a reply in a thread.
    /// </summary>
    /// <param name="content">The message content.</param>
    /// <param name="mentions">The users mentioned by the message.</param>
    /// <param name="parentMessageId">The thread's root message ID, if this is a reply.</param>
    /// <returns>The request ID.</returns>
    public Guid SendMessage(string content, Mentions mentions, Guid? parentMessageId = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(mentions);
        return SendRequest(new SendMessage(
            DateTimeOffset.UtcNow,
            mentions.Clone(),
            content,
            parentMessageId));
    }

    /// <summary>
    /// Requests an edit to a room or thread message.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="newContent">The replacement content.</param>
    /// <param name="parentMessageId">The thread's root message ID, if applicable.</param>
    /// <returns>The request ID.</returns>
    public Guid EditMessage(Guid messageId, string newContent, Guid? parentMessageId = null)
    {
        ArgumentNullException.ThrowIfNull(newContent);
        return SendRequest(new EditMessage(messageId, newContent, parentMessageId));
    }

    /// <summary>
    /// Requests deletion of a room or thread message.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="parentMessageId">The thread's root message ID, if applicable.</param>
    /// <returns>The request ID.</returns>
    public Guid DeleteMessage(Guid messageId, Guid? parentMessageId = null)
    {
        return SendRequest(new DeleteMessage(messageId, parentMessageId));
    }

    /// <summary>
    /// Toggles a reaction on a room or thread message.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="emoji">The reaction emoji.</param>
    /// <param name="parentMessageId">The thread's root message ID, if applicable.</param>
    /// <returns>The request ID.</returns>
    public Guid ToggleReaction(Guid messageId, string emoji, Guid? parentMessageId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(emoji);
        return SendRequest(new global::Chat.ToggleReaction(messageId, emoji, parentMessageId));
    }

    /// <summary>
    /// Unsubscribes this client from communicator messages.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _communicator.RemoveSubscriber(ChatConstants.ModuleId);
        _disposed = true;
    }

    private Guid SendRequest(ChatClientRequest request)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Guid requestId = Guid.NewGuid();
        ChatClientRequestEnvelope envelope = new(requestId, request, Username);
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(envelope);
        _communicator.SendMessage(_serverAddress, payload, ChatConstants.ModuleId, priority: 1);
        return requestId;
    }

    /// <inheritdoc />
    public void OnMessageReceived(byte[] message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ChatServerMessage? serverMessage = JsonSerializer.Deserialize<ChatServerMessage>(message);
        if (serverMessage is null)
        {
            throw new InvalidDataException("The received chat server message was empty.");
        }

        lock (_syncRoot)
        {
            switch (serverMessage)
            {
                case ChatServerUpdateEnvelope updateEnvelope:
                    switch (updateEnvelope.Update)
                    {
                        case ChatServerReset:
                            Chatroom.ClearMessages();
                            _version = 0;
                            break;

                        case ChatServerOperationBatch batch:
                            if (batch.FromVersion != _version)
                            {
                                SendRequest(new Reset());
                                return;
                            }

                            foreach (ChatOperation operation in batch.Operations)
                            {
                                RoomResult operationResult = ChatProtocol.ApplyOperation(Chatroom, operation);
                                if (operationResult != RoomResult.Ok)
                                {
                                    throw new InvalidDataException(
                                        $"The server sent an operation that could not be applied: {operationResult}.");
                                }

                                OperationApplied?.Invoke(operation);
                                _version++;
                            }

                            if (_version != batch.ToVersion)
                            {
                                SendRequest(new Reset());
                                return;
                            }

                            break;

                        default:
                            throw new InvalidDataException(
                                $"Unsupported chat server update: {updateEnvelope.Update.GetType().Name}.");
                    }

                    Chatroom.SetParticipants(updateEnvelope.Participants);
                    break;

                case ChatServerRequestResult result:
                    RequestCompleted?.Invoke(result.RequestId, result.Result);
                    break;

                default:
                    throw new InvalidDataException($"Unsupported chat server message: {message.GetType().Name}.");
            }
        }
    }
}
