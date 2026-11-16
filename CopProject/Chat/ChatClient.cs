// Author: Zeeshan
// Roll no.: 112301040

using System.Diagnostics;
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
        Trace.TraceInformation(
            "[Chat] Client '{0}' subscribed for room '{1}'.",
            Username,
            Chatroom.RoomName);
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
    /// Raised when the local room messages are reset.
    /// </summary>
    public event Action? RoomReset;

    /// <summary>
    /// Raised when the room participant list changes.
    /// </summary>
    public event Action<IReadOnlySet<string>>? ParticipantsChanged;

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
        get {
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
    /// <param name="threadParentMessageId">The thread's root message ID, if this is a reply.</param>
    /// <param name="replyToMessageId">The main-room message this message replies to inline.</param>
    /// <returns>The request ID.</returns>
    public Guid SendMessage(
        string content,
        Mentions mentions,
        Guid? threadParentMessageId = null,
        Guid? replyToMessageId = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(mentions);
        return SendRequest(new SendMessage(
            DateTimeOffset.UtcNow,
            mentions.Clone(),
            content,
            threadParentMessageId,
            replyToMessageId));
    }

    /// <summary>
    /// Requests an edit to a room or thread message.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="newContent">The replacement content.</param>
    /// <param name="threadParentMessageId">The thread's root message ID, if applicable.</param>
    /// <returns>The request ID.</returns>
    public Guid EditMessage(Guid messageId, string newContent, Guid? threadParentMessageId = null)
    {
        ArgumentNullException.ThrowIfNull(newContent);
        return SendRequest(new EditMessage(messageId, newContent, threadParentMessageId));
    }

    /// <summary>
    /// Requests deletion of a room or thread message.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="threadParentMessageId">The thread's root message ID, if applicable.</param>
    /// <returns>The request ID.</returns>
    public Guid DeleteMessage(Guid messageId, Guid? threadParentMessageId = null)
    {
        return SendRequest(new DeleteMessage(messageId, threadParentMessageId));
    }

    /// <summary>
    /// Toggles a reaction on a room or thread message.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="emoji">The reaction emoji.</param>
    /// <param name="threadParentMessageId">The thread's root message ID, if applicable.</param>
    /// <returns>The request ID.</returns>
    public Guid ToggleReaction(Guid messageId, string emoji, Guid? threadParentMessageId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(emoji);
        return SendRequest(new global::Chat.ToggleReaction(messageId, emoji, threadParentMessageId));
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
        Trace.TraceInformation("[Chat] Client '{0}' unsubscribed from chat updates.", Username);
    }

    private Guid SendRequest(ChatClientRequest request)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Guid requestId = Guid.NewGuid();
        ChatClientRequestEnvelope envelope = new(requestId, request, Username);
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(envelope);
        try
        {
            _communicator.SendMessage(_serverAddress, payload, ChatConstants.ModuleId, priority: 1);
        }
        catch (Exception exception)
        {
            Trace.TraceError(
                "[Chat] Client '{0}' failed to send {1} request {2}: {3}",
                Username,
                request.GetType().Name,
                requestId,
                exception);
            throw;
        }

        Trace.TraceInformation(
            "[Chat] Client '{0}' sent {1} request {2}.",
            Username,
            request.GetType().Name,
            requestId);
        return requestId;
    }

    /// <inheritdoc />
    public void OnMessageReceived(byte[] message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ChatServerMessage? serverMessage;
        try
        {
            serverMessage = JsonSerializer.Deserialize<ChatServerMessage>(message);
        }
        catch (JsonException exception)
        {
            Trace.TraceError("[Chat] Client '{0}' could not deserialize a server message: {1}", Username, exception);
            throw;
        }

        if (serverMessage is null)
        {
            Trace.TraceWarning("[Chat] Client '{0}' received an empty server message.", Username);
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
                            Trace.TraceInformation("[Chat] Client '{0}' is resetting its local room.", Username);
                            Chatroom.ClearMessages();
                            _version = 0;
                            RoomReset?.Invoke();
                            break;

                        case ChatServerOperationBatch batch:
                            if (batch.FromVersion != _version)
                            {
                                Trace.TraceWarning(
                                    "[Chat] Client '{0}' expected version {1} but received batch from version {2}; requesting reset.",
                                    Username,
                                    _version,
                                    batch.FromVersion);
                                SendRequest(new Reset());
                                return;
                            }

                            foreach (ChatOperation operation in batch.Operations)
                            {
                                RoomResult operationResult = ChatProtocol.ApplyOperation(Chatroom, operation);
                                if (operationResult != RoomResult.Ok)
                                {
                                    Trace.TraceError(
                                        "[Chat] Client '{0}' could not apply server operation {1}: {2}.",
                                        Username,
                                        operation.GetType().Name,
                                        operationResult);
                                    throw new InvalidDataException(
                                        $"The server sent an operation that could not be applied: {operationResult}.");
                                }

                                OperationApplied?.Invoke(operation);
                                _version++;
                            }

                            if (_version != batch.ToVersion)
                            {
                                Trace.TraceWarning(
                                    "[Chat] Client '{0}' reached version {1} but server batch ended at {2}; requesting reset.",
                                    Username,
                                    _version,
                                    batch.ToVersion);
                                SendRequest(new Reset());
                                return;
                            }

                            Trace.TraceInformation(
                                "[Chat] Client '{0}' applied synchronization batch through version {1}.",
                                Username,
                                _version);
                            break;

                        default:
                            Trace.TraceError(
                                "[Chat] Client '{0}' received unsupported update type '{1}'.",
                                Username,
                                updateEnvelope.Update.GetType().Name);
                            throw new InvalidDataException(
                                $"Unsupported chat server update: {updateEnvelope.Update.GetType().Name}.");
                    }

                    Chatroom.SetParticipants(updateEnvelope.Participants);
                    ParticipantsChanged?.Invoke(Chatroom.Participants);
                    return;

                case ChatServerRequestResult result:
                    if (result.Result != RoomResult.Ok)
                    {
                        Trace.TraceWarning(
                            "[Chat] Server completed request {0} for client '{1}' with result {2}.",
                            result.RequestId,
                            Username,
                            result.Result);
                    }

                    RequestCompleted?.Invoke(result.RequestId, result.Result);
                    break;

                default:
                    Trace.TraceError(
                        "[Chat] Client '{0}' received unsupported server message type '{1}'.",
                        Username,
                        serverMessage.GetType().Name);
                    throw new InvalidDataException($"Unsupported chat server message: {message.GetType().Name}.");
            }
        }
    }
}
